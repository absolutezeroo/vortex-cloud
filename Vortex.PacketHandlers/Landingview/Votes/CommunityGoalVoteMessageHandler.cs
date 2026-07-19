using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Landingview.Votes;

namespace Vortex.PacketHandlers.Landingview.Votes;

public class CommunityGoalVoteMessageHandler : IMessageHandler<CommunityGoalVoteMessage>
{
    public async ValueTask HandleAsync(
        CommunityGoalVoteMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
