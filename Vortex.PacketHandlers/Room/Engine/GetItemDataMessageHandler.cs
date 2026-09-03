using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Room.Engine;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

/// <summary>
/// Answers "what does this say?" for the sticky note the client is about to open.
/// </summary>
/// <remarks>
/// To the asker alone: everyone in the room already has the item's data from the room load, and this
/// is the one client that is opening the note. Unanswered, the note opens blank -- the widget will
/// not draw until it has the string.
/// </remarks>
public class GetItemDataMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetItemDataMessage>
{
    public async ValueTask HandleAsync(
        GetItemDataMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.RoomId <= 0)
        {
            return;
        }

        string? data = await grainFactory
            .GetRoomFurni(ctx.RoomId)
            .GetItemDataAsync(ctx.AsActionContext(), message.ItemId, ct)
            .ConfigureAwait(false);

        if (data is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new ItemDataUpdateMessageComposer { ObjectId = message.ItemId, State = data },
                ct
            )
            .ConfigureAwait(false);
    }
}
