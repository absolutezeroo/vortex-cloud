using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The client telling the room it closed a chest.
/// </summary>
/// <remarks>
/// Opening a chest takes no lock and reserves nothing, so this releases nothing either. What it
/// does do is shut the lid: a chest set to open when someone looks inside wears its open state for
/// as long as a screen is up, and the preview icons it floats hang off that same state.
/// </remarks>
public class CloseWiredChestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<CloseWiredChestMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        CloseWiredChestMessage message,
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
            .CloseWiredChestAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                ct
            )
            .ConfigureAwait(false);
    }
}
