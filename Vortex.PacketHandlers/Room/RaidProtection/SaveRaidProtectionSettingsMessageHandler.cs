using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.RaidProtection;
using Vortex.Protocol.Messages.Incoming.Room.RaidProtection;
using Vortex.Protocol.Messages.Outgoing.Room.RaidProtection;

namespace Vortex.PacketHandlers.Room.RaidProtection;

/// <summary>
/// A draft from the raid-protection panel. The room validates it, keeps it or refuses it, and
/// answers with a result code and its own state either way.
/// </summary>
public class SaveRaidProtectionSettingsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SaveRaidProtectionSettingsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SaveRaidProtectionSettingsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.RoomId != ctx.RoomId)
        {
            return;
        }

        IRoomRaidProtection room = _grainFactory.GetRoomRaidProtection(new RoomId(ctx.RoomId));

        // IncidentActive and LastRaidAt are the room's to report, never the client's to set: the
        // draft carries whatever keeps the record whole and the room overwrites both.
        RoomRaidProtectionSnapshot draft = new()
        {
            RoomId = message.RoomId,
            Enabled = message.Enabled,
            DetectionSensitivity = message.DetectionSensitivity,
            ActionType = message.ActionType,
            BanDurationSeconds = message.BanDurationSeconds,
            GuardEnabled = message.GuardEnabled,
            GuardDurationSeconds = message.GuardDurationSeconds,
            GuardSensitivity = message.GuardSensitivity,
            IncidentActive = false,
            LastRaidAtEpochSeconds = 0,
        };

        RaidProtectionSaveOutcome outcome = await room.SaveRaidProtectionAsync(
                ctx.AsActionContext(),
                draft,
                message.Confirmed,
                ct
            )
            .ConfigureAwait(false);

        await _grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new RaidProtectionSettingsResultMessageComposer
                {
                    Result = outcome.Result,
                    Settings = outcome.Settings,
                }
            )
            .ConfigureAwait(false);
    }
}
