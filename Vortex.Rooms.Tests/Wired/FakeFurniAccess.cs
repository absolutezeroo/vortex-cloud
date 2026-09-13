using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The room as a wired box reaches it: only the members the wired tests exercise answer, and each
/// records what it was asked. Hand-written rather than proxied because several members answer
/// through <c>out</c> parameters.
/// </summary>
internal sealed class FakeFurniAccess : IRoomFurniAccess
{
    public Dictionary<int, HashSet<PlayerId>> Members { get; } = [];

    public Dictionary<PlayerId, HashSet<string>> WornBadges { get; } = [];

    public List<int> RostersRequested { get; } = [];

    public List<PlayerId> BadgesRequested { get; } = [];

    public List<(
        int CallerTileIdx,
        int[] TargetFurniIds,
        int[] InheritedPlayerIds
    )> StacksCalled { get; } = [];

    public Task EnsureGuildRosterAsync(int groupId, CancellationToken ct)
    {
        RostersRequested.Add(groupId);

        return Task.CompletedTask;
    }

    public bool IsGuildMember(int groupId, PlayerId player) =>
        Members.TryGetValue(groupId, out HashSet<PlayerId>? members) && members.Contains(player);

    public Task EnsureWornBadgesAsync(PlayerId player, CancellationToken ct)
    {
        BadgesRequested.Add(player);

        return Task.CompletedTask;
    }

    public bool IsWearingBadge(PlayerId player, string badgeCode) =>
        WornBadges.TryGetValue(player, out HashSet<string>? worn) && worn.Contains(badgeCode);

    public Task<int> ExecuteWiredStacksAtAsync(
        int callerTileIdx,
        IReadOnlyCollection<int> targetFurniIds,
        IWiredSelectionSet inheritedSelection,
        CancellationToken ct
    )
    {
        StacksCalled.Add(
            (callerTileIdx, [.. targetFurniIds], [.. inheritedSelection.SelectedPlayerIds])
        );

        return Task.FromResult(targetFurniIds.Count);
    }

    /// <summary>Which destinations the room accepts. Everything, unless a test narrows it — a box
    /// that moves furni has to be exercised against a refusal as well as against a clear floor.
    /// </summary>
    public Func<int, int, bool> PlacementAllowed { get; set; } = (_, _) => true;

    public Task<bool> ValidateFloorItemPlacementAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Rotation rot
    ) => Task.FromResult(PlacementAllowed(x, y));

    /// <summary>Variable boxes the test put in the room, keyed by id. Empty unless a test fills it —
    /// a box that resolves a configured variable id prunes it against this, so an id nothing answers
    /// for is dropped exactly as it would be in a live room.</summary>
    public Dictionary<WiredVariableId, IWiredVariable> Variables { get; } = [];

    public IWiredVariable? GetVariableById(WiredVariableId id) =>
        Variables.TryGetValue(id, out IWiredVariable? variable) ? variable : null;

    public void ScheduleFlashRevert(RoomObjectId objectId) { }

    public void ResetTimers() { }

    public WiredVariableHash AllVariablesHash => default;

    public bool TryGetVariableStore(WiredVariableKey key, out IWiredKeyValueStore? store)
    {
        store = null;

        return false;
    }

    public Task<bool> KickUserFromWiredAsync(PlayerId targetPlayerId, CancellationToken ct) =>
        Task.FromResult(false);

    public List<(PlayerId Target, int DurationSeconds)> Muted { get; } = [];

    public Task<bool> MuteUserFromWiredAsync(
        PlayerId targetPlayerId,
        int durationSeconds,
        CancellationToken ct
    )
    {
        Muted.Add((targetPlayerId, durationSeconds));

        return Task.FromResult(true);
    }
}
