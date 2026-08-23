using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// The sticky note's contents when its editor closes. Despite the name this is an update, not an
/// add — the note already exists on the wall by the time this arrives.
/// </summary>
public class AddSpamWallPostItMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<AddSpamWallPostItMessage>
{
    private const int MaxTextLength = 684;

    public async ValueTask HandleAsync(
        AddSpamWallPostItMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        // The colour shares one space-separated string with the text, so a colour containing a
        // space would silently eat the first word of the note.
        string colorHex = message.ColorHex.Replace(" ", string.Empty);

        string text =
            message.Text.Length > MaxTextLength ? message.Text[..MaxTextLength] : message.Text;

        await grainFactory
            .GetRoomFurni(ctx.RoomId)
            .SetPostItAsync(ctx.AsActionContext(), message.ObjectId, colorHex, text, ct)
            .ConfigureAwait(false);
    }
}
