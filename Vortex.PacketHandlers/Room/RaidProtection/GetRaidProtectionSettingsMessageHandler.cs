using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Protocol.Messages.Incoming.Room.RaidProtection;
using Vortex.Protocol.Messages.Outgoing.Room.RaidProtection;

namespace Vortex.PacketHandlers.Room.RaidProtection;

/// <summary>
/// The owner opened the raid-protection panel and wants the room's current settings.
/// </summary>
/// <remarks>
/// Silence is the answer when the room refuses: the official client drops a settings packet for a
/// room it holds no capability in, so there is nothing an error reply could usefully say.
/// </remarks>
public class GetRaidProtectionSettingsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetRaidProtectionSettingsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetRaidProtectionSettingsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        // The client only ever asks about the room it is standing in, and only that room's grain
        // knows whether this player may manage it. Taking the room id from the session rather than
        // from the packet is what stops one player reading another room's configuration.
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.RoomId != ctx.RoomId)
        {
            return;
        }

        IRoomRaidProtection room = _grainFactory.GetRoomRaidProtection(new RoomId(ctx.RoomId));

        RoomRaidProtectionSnapshot? settings = await room.GetRaidProtectionAsync(ctx.PlayerId)
            .ConfigureAwait(false);

        if (settings is null)
        {
            return;
        }

        await _grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(new RaidProtectionSettingsMessageComposer { Settings = settings })
            .ConfigureAwait(false);
    }
}
