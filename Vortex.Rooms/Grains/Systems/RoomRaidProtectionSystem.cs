using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.RaidProtection;
using Vortex.Primitives.Server;
using Vortex.Protocol.Messages.Outgoing.Room.RaidProtection;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// The room's raid detector, and the settings panel behind it.
/// </summary>
/// <remarks>
/// <para>
/// The panel is Habbo's: field for field, value for value, it is what <c>RaidProtectionSettings</c>
/// in the AIR 1.0.31 client sends and reads. <b>The detector is not.</b> No client build carries a
/// threshold, a window or a rule, because the official server never puts one on the wire — so what
/// counts as a raid here is this emulator's design and is recorded as such in the specs. Anyone
/// reading this later: the settings are evidence, the arithmetic below is a decision.
/// </para>
/// <para>
/// It lives on the room grain because it is asked on the entry path, on every arrival, and a raid
/// is exactly when that path cannot afford a second hop. Settings are read once per activation and
/// written through on save, so the hot path touches no database at all.
/// </para>
/// </remarks>
public sealed class RoomRaidProtectionSystem(RoomGrain roomGrain)
{
    /// <summary>Minimum gap between two saves from one room, against a stuck panel.</summary>
    private static readonly TimeSpan SaveCooldown = TimeSpan.FromSeconds(2);

    /// <summary>Low, medium, high — in that order, and read in that order below.</summary>
    private static readonly ImmutableArray<string> ThresholdKeys =
    [
        "room.raid_protection.threshold.low",
        "room.raid_protection.threshold.medium",
        "room.raid_protection.threshold.high",
    ];

    private readonly RoomGrain _roomGrain = roomGrain;

    private RoomRaidProtectionState State => _roomGrain._state.RaidProtection;

    /// <summary>
    /// Whether this player may see and change the room's protection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The owner and the room's rights-holders, which is Sulake's own rule — the announcement is
    /// explicit that the menu "can be accessed by the room owner and Habbos with rights to the
    /// room". It was owner-only here first, on the reasoning that a raid often arrives with rights
    /// already in the wrong hands; the official behaviour outranks that, and a room whose rights
    /// are compromised has worse problems than this switch.
    /// </para>
    /// <para>
    /// The announcement also gives Ambassadors access in official rooms. Not implemented: this
    /// server has no ambassador role (<c>IsAmbassador</c> is hardcoded false everywhere) and no
    /// official-room flag, so there is nothing to resolve it against yet. Staff reach the settings
    /// through the dashboard instead.
    /// </para>
    /// <para>
    /// <see cref="RoomSecurityModule.HasExplicitRights" /> is the same synchronous test the entry
    /// path uses to decide who is exempt from the detector, and deliberately so: whoever can turn
    /// the protection off is exactly whoever it must never lock out.
    /// </para>
    /// </remarks>
    public bool CanManage(PlayerId playerId) =>
        playerId > 0 && _roomGrain.SecurityModule.HasExplicitRights(playerId);

    public async Task<RoomRaidProtectionSnapshot?> GetSettingsAsync(PlayerId viewerId)
    {
        if (!CanManage(viewerId))
        {
            return null;
        }

        await EnsureLoadedAsync(CancellationToken.None).ConfigureAwait(true);

        return BuildSnapshot();
    }

    public async Task<RaidProtectionSaveOutcome> SaveSettingsAsync(
        ActionContext actorCtx,
        RoomRaidProtectionSnapshot draft,
        bool confirmed,
        CancellationToken ct
    )
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(true);

        if (!CanManage(actorCtx.PlayerId) || actorCtx.RoomId != _roomGrain._state.RoomId)
        {
            return Refuse(RaidProtectionSaveResult.NotAllowed);
        }

        if (draft.RoomId != _roomGrain._state.RoomId.Value)
        {
            return Refuse(RaidProtectionSaveResult.RoomNotFound);
        }

        if (
            !RaidProtectionLimits.IsSensitivity(draft.DetectionSensitivity)
            || !RaidProtectionLimits.IsSensitivity(draft.GuardSensitivity)
            || !RaidProtectionLimits.IsAction(draft.ActionType)
            || !RaidProtectionLimits.IsBanDuration(draft.BanDurationSeconds)
            || !RaidProtectionLimits.IsGuardDuration(draft.GuardDurationSeconds)
        )
        {
            return Refuse(RaidProtectionSaveResult.InvalidSettings);
        }

        DateTime nowUtc = DateTime.UtcNow;

        if (nowUtc - State.LastSaveAtUtc < SaveCooldown)
        {
            return Refuse(RaidProtectionSaveResult.TooFast);
        }

        // The one case the client's confirmation prompt actually protects: switching protection on
        // while a raid is being handled changes the rules under the raiders mid-incident. The
        // client asks first and sets the flag; a client that did not ask does not get to skip it.
        if (draft.Enabled && !State.Enabled && State.IncidentActive && !confirmed)
        {
            return Refuse(RaidProtectionSaveResult.ConfirmationRequired);
        }

        State.Enabled = draft.Enabled;
        State.DetectionSensitivity = (RaidDetectionSensitivity)draft.DetectionSensitivity;
        State.ActionType = (RaidProtectionAction)draft.ActionType;
        State.BanDurationSeconds = draft.BanDurationSeconds;
        State.GuardEnabled = draft.GuardEnabled;
        State.GuardDurationSeconds = draft.GuardDurationSeconds;
        State.GuardSensitivity = (RaidDetectionSensitivity)draft.GuardSensitivity;

        // Turning protection off ends whatever it was doing. Leaving a guard period running behind
        // a disabled switch would keep refusing visitors with nothing in the panel explaining why.
        if (!State.Enabled)
        {
            State.IncidentActive = false;
            State.GuardUntilUtc = null;
        }

        State.LastSaveAtUtc = nowUtc;

        if (!await PersistAsync(ct).ConfigureAwait(true))
        {
            return Refuse(RaidProtectionSaveResult.Failed);
        }

        return new RaidProtectionSaveOutcome
        {
            Result = RaidProtectionSaveResult.Ok,
            Settings = BuildSnapshot(),
        };
    }

    /// <summary>
    /// Judges one arrival and counts it. See <see cref="IRoomRaidProtection.EvaluateEntryAsync" />
    /// for why it must be called exactly once per entry.
    /// </summary>
    public async Task<RaidEntryDecision> EvaluateEntryAsync(PlayerId playerId, CancellationToken ct)
    {
        bool canManage = CanManage(playerId);

        await EnsureLoadedAsync(ct).ConfigureAwait(true);

        if (!State.Enabled || playerId <= 0)
        {
            return RaidEntryDecision.Allowed(canManage);
        }

        // The owner and the people they trusted are never counted and never turned away. If a raid
        // could shut the owner out, the only switch that stops it would be behind the raid.
        if (_roomGrain.SecurityModule.HasExplicitRights(playerId))
        {
            return RaidEntryDecision.Allowed(canManage);
        }

        DateTime nowUtc = DateTime.UtcNow;

        State.Arrivals[playerId] = nowUtc;

        int arrivals = State.CountRecentArrivals(nowUtc);
        int threshold = State.CurrentThreshold(nowUtc);

        if (arrivals <= threshold)
        {
            return RaidEntryDecision.Allowed(canManage);
        }

        // Staff are checked only here, not on every arrival: this is a permission lookup, and the
        // entry path during a raid is the last place to spend one on people who are not being
        // sanctioned.
        if (
            await _roomGrain
                .SecurityModule.HasCapabilityAsync(playerId, Capabilities.Room.ModerateAny)
                .ConfigureAwait(true)
        )
        {
            return RaidEntryDecision.Allowed(canManage);
        }

        await OnIncidentAsync(arrivals, threshold, nowUtc, ct).ConfigureAwait(true);

        if (State.ActionType == RaidProtectionAction.Kick)
        {
            return new RaidEntryDecision
            {
                Verdict = RaidEntryVerdict.Kick,
                BanDurationSeconds = 0,
                CanManage = canManage,
            };
        }

        await _roomGrain
            ._moderationStore.BanAsync(
                _roomGrain._state.RoomId.Value,
                playerId,
                nowUtc.AddSeconds(State.BanDurationSeconds),
                ct
            )
            .ConfigureAwait(true);

        return new RaidEntryDecision
        {
            Verdict = RaidEntryVerdict.Ban,
            BanDurationSeconds = State.BanDurationSeconds,
            CanManage = canManage,
        };
    }

    /// <summary>
    /// Closes an incident once the room is quiet again, and expires the guard period behind it.
    /// Driven by the room clock, so nothing here runs on the entry path.
    /// </summary>
    public async Task TickAsync(DateTime nowUtc, CancellationToken ct)
    {
        // This runs twenty times a second in every loaded room. There is nothing to expire unless
        // an incident or a guard period is actually running, and in a healthy hotel that is none of
        // them — so the cheap test comes first and the window sweep only happens when it can matter.
        if (
            !State.Loaded
            || !State.Enabled
            || (!State.IncidentActive && State.GuardUntilUtc is null)
        )
        {
            return;
        }

        bool changed = false;

        if (
            State.IncidentActive
            && State.CountRecentArrivals(nowUtc) <= State.CurrentThreshold(nowUtc)
        )
        {
            State.IncidentActive = false;
            changed = true;

            if (State.GuardEnabled)
            {
                State.GuardUntilUtc = nowUtc.AddSeconds(State.GuardDurationSeconds);
            }

            await ReportIncidentAsync(active: false).ConfigureAwait(true);
        }

        if (State.GuardUntilUtc is DateTime until && until <= nowUtc)
        {
            State.GuardUntilUtc = null;
            changed = true;
        }

        if (changed)
        {
            await PushToOwnerAsync(ct).ConfigureAwait(true);
        }
    }

    private async Task OnIncidentAsync(
        int arrivals,
        int threshold,
        DateTime nowUtc,
        CancellationToken ct
    )
    {
        bool opening = !State.IncidentActive;

        State.IncidentActive = true;
        State.LastRaidAtUtc = nowUtc;

        if (!opening)
        {
            return;
        }

        _roomGrain._logger.LogWarning(
            "Room {RoomId} is being raided: {Arrivals} arrivals in {WindowSeconds}s against a "
                + "threshold of {Threshold}. Applying {Action}.",
            _roomGrain._state.RoomId,
            arrivals,
            (int)RoomRaidProtectionState.Window.TotalSeconds,
            threshold,
            State.ActionType
        );

        await _roomGrain
            ._events.PublishAsync(
                new RoomRaidDetectedEvent(
                    _roomGrain._state.RoomId.Value,
                    arrivals,
                    threshold,
                    State.GuardEnabled
                ),
                ct
            )
            .ConfigureAwait(true);

        // Only the first arrival of an incident writes: the timestamp is what survives a restart,
        // and rewriting it for every raider would put a database round trip on each one. The same
        // reasoning covers the directory push — twice per incident, not twice per raider.
        await PersistAsync(ct).ConfigureAwait(true);
        await ReportIncidentAsync(active: true).ConfigureAwait(true);
        await PushToOwnerAsync(ct).ConfigureAwait(true);
    }

    /// <summary>
    /// Redraws the owner's panel if they have it open. The client updates a visible window from an
    /// unsolicited settings packet and ignores it otherwise, so this needs no "is it open" state.
    /// </summary>
    private async Task PushToOwnerAsync(CancellationToken ct)
    {
        PlayerId ownerId = _roomGrain._state.RoomSnapshot.OwnerId;

        if (ownerId <= 0 || !_roomGrain._state.AvatarsByPlayerId.ContainsKey(ownerId))
        {
            return;
        }

        await _roomGrain
            ._grainFactory.GetPlayerPresenceGrain(ownerId)
            .SendComposerAsync(
                new RaidProtectionSettingsMessageComposer { Settings = BuildSnapshot() }
            )
            .ConfigureAwait(true);
    }

    private RaidProtectionSaveOutcome Refuse(RaidProtectionSaveResult result) =>
        new() { Result = result, Settings = BuildSnapshot() };

    private RoomRaidProtectionSnapshot BuildSnapshot() =>
        new()
        {
            RoomId = _roomGrain._state.RoomId.Value,
            Enabled = State.Enabled,
            DetectionSensitivity = (int)State.DetectionSensitivity,
            ActionType = (int)State.ActionType,
            BanDurationSeconds = State.BanDurationSeconds,
            GuardEnabled = State.GuardEnabled,
            GuardDurationSeconds = State.GuardDurationSeconds,
            GuardSensitivity = (int)State.GuardSensitivity,
            IncidentActive = State.IncidentActive,
            LastRaidAtEpochSeconds = State.LastRaidAtUtc is DateTime at
                ? (int)new DateTimeOffset(at, TimeSpan.Zero).ToUnixTimeSeconds()
                : 0,
        };

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (State.Loaded)
        {
            return;
        }

        // Marked loaded before the read, not after: a failed read leaves the room on defaults
        // (protection off) rather than retrying the same broken query on every single arrival.
        State.Loaded = true;

        await LoadThresholdsAsync().ConfigureAwait(true);

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        RoomRaidProtectionEntity? row = await dbCtx
            .RoomRaidProtection.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoomEntityId == _roomGrain._state.RoomId.Value, ct)
            .ConfigureAwait(true);

        if (row is null)
        {
            return;
        }

        State.Enabled = row.Enabled;
        State.DetectionSensitivity = (RaidDetectionSensitivity)row.DetectionSensitivity;
        State.ActionType = (RaidProtectionAction)row.ActionType;
        State.BanDurationSeconds = row.BanDurationSeconds;
        State.GuardEnabled = row.GuardEnabled;
        State.GuardDurationSeconds = row.GuardDurationSeconds;
        State.GuardSensitivity = (RaidDetectionSensitivity)row.GuardSensitivity;
        State.LastRaidAtUtc = row.LastRaidAt;
    }

    /// <summary>
    /// Tells the room directory that this room has started or finished handling a raid, so the
    /// dashboard's live room list shows it without polling every room in the hotel.
    /// </summary>
    /// <remarks>
    /// Best effort, and deliberately: an operator's view going stale is not a reason to abandon a
    /// room mid-raid, and this is called from the entry path.
    /// </remarks>
    private async Task ReportIncidentAsync(bool active)
    {
        try
        {
            await _roomGrain
                ._grainFactory.GetRoomDirectoryGrain()
                .SetRaidIncidentAsync(_roomGrain._state.RoomId, active)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Room {RoomId} could not report its raid incident to the directory.",
                _roomGrain._state.RoomId
            );
        }
    }

    /// <summary>
    /// Pulls the hotel's three thresholds, in one round trip to the config singleton, once per room
    /// activation. A failure here is not worth failing an entry over: the state keeps the fallbacks
    /// it was constructed with and the room runs on those.
    /// </summary>
    private async Task LoadThresholdsAsync()
    {
        try
        {
            ImmutableDictionary<string, string> values = await _roomGrain
                ._grainFactory.GetServerConfigGrain()
                .GetManyAsync(ThresholdKeys)
                .ConfigureAwait(true);

            State.LowThreshold = ServerConfigValues.GetInt(values, ThresholdKeys[0], 12);
            State.MediumThreshold = ServerConfigValues.GetInt(values, ThresholdKeys[1], 8);
            State.HighThreshold = ServerConfigValues.GetInt(values, ThresholdKeys[2], 5);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Room {RoomId} could not read the raid-protection thresholds; using the built-in "
                    + "defaults for this activation.",
                _roomGrain._state.RoomId
            );
        }
    }

    private async Task<bool> PersistAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            RoomRaidProtectionEntity? row = await dbCtx
                .RoomRaidProtection.FirstOrDefaultAsync(
                    r => r.RoomEntityId == _roomGrain._state.RoomId.Value,
                    ct
                )
                .ConfigureAwait(true);

            if (row is null)
            {
                row = new RoomRaidProtectionEntity
                {
                    RoomEntityId = _roomGrain._state.RoomId.Value,
                    Enabled = State.Enabled,
                    DetectionSensitivity = (int)State.DetectionSensitivity,
                    ActionType = (int)State.ActionType,
                    BanDurationSeconds = State.BanDurationSeconds,
                    GuardEnabled = State.GuardEnabled,
                    GuardDurationSeconds = State.GuardDurationSeconds,
                    GuardSensitivity = (int)State.GuardSensitivity,
                    LastRaidAt = State.LastRaidAtUtc,
                };

                dbCtx.RoomRaidProtection.Add(row);
            }
            else
            {
                row.Enabled = State.Enabled;
                row.DetectionSensitivity = (int)State.DetectionSensitivity;
                row.ActionType = (int)State.ActionType;
                row.BanDurationSeconds = State.BanDurationSeconds;
                row.GuardEnabled = State.GuardEnabled;
                row.GuardDurationSeconds = State.GuardDurationSeconds;
                row.GuardSensitivity = (int)State.GuardSensitivity;
                row.LastRaidAt = State.LastRaidAtUtc;
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            return true;
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogError(
                ex,
                "Failed to persist raid protection settings for room {RoomId}.",
                _roomGrain._state.RoomId
            );

            return false;
        }
    }
}
