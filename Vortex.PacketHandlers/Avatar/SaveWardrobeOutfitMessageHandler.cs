using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Avatar;

namespace Vortex.PacketHandlers.Avatar;

public class SaveWardrobeOutfitMessageHandler : IMessageHandler<SaveWardrobeOutfitMessage>
{
    public async ValueTask HandleAsync(
        SaveWardrobeOutfitMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
