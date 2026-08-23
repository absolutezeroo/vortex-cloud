using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Quest;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Quest;

/// <summary>
/// "Open the quest window on this campaign." The grain owns the reply — it pushes the list and the
/// campaign's current quest itself.
/// </summary>
public class StartCampaignMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<StartCampaignMessage>
{
    public async ValueTask HandleAsync(
        StartCampaignMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || string.IsNullOrWhiteSpace(message.CampaignCode))
        {
            return;
        }

        await grainFactory
            .GetPlayerQuestGrain(ctx.PlayerId)
            .StartCampaignAsync(message.CampaignCode, ct)
            .ConfigureAwait(false);
    }
}
