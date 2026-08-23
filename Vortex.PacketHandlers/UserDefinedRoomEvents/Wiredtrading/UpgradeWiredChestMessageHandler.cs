using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The chest upgrade dialog's buy button.
/// </summary>
/// <remarks>
/// Registered so the message is read rather than logged as unknown, and refused inside the room —
/// see <c>UpgradeWiredChestAsync</c> for why an upgrade with no price is not granted. The client
/// asks for nothing back, so a refusal leaves the dialog as it was.
/// </remarks>
public class UpgradeWiredChestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UpgradeWiredChestMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        UpgradeWiredChestMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .UpgradeWiredChestAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                message.UpgradeType,
                ct
            )
            .ConfigureAwait(false);
    }
}
