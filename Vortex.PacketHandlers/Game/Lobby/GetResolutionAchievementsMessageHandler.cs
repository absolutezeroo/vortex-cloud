using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Lobby;

namespace Vortex.PacketHandlers.Game.Lobby;

public class GetResolutionAchievementsMessageHandler
    : IMessageHandler<GetResolutionAchievementsMessage>
{
    public async ValueTask HandleAsync(
        GetResolutionAchievementsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
