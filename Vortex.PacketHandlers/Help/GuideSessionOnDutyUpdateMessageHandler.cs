using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Help;
using Vortex.Primitives.Help.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Help;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// Puts a guide on or off duty. The reply is what the guide tool draws its whole header from, so it
/// is sent on every change including the one that takes them off duty.
/// </summary>
public class GuideSessionOnDutyUpdateMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GuideSessionOnDutyUpdateMessage>
{
    public async ValueTask HandleAsync(
        GuideSessionOnDutyUpdateMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        GuideDutySnapshot duty = await grainFactory
            .GetGuideDirectoryGrain()
            .SetDutyAsync(
                ctx.PlayerId,
                message.OnDuty,
                message.HandlesGuideRequests,
                message.HandlesHelperRequests,
                message.HandlesGuardianRequests,
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new GuideOnDutyStatusMessageComposer
                {
                    OnDuty = duty.OnDuty,
                    GuidesOnDuty = duty.GuidesOnDuty,
                    HelpersOnDuty = duty.HelpersOnDuty,
                    GuardiansOnDuty = duty.GuardiansOnDuty,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
