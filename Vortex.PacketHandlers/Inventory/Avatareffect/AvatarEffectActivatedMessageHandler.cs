using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Avatareffect;

namespace Vortex.PacketHandlers.Inventory.Avatareffect;

public class AvatarEffectActivatedMessageHandler : IMessageHandler<AvatarEffectActivatedMessage>
{
    public async ValueTask HandleAsync(
        AvatarEffectActivatedMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
