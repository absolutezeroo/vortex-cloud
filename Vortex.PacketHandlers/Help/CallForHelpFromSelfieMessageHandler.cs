using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Configuration;
using Vortex.Protocol.Messages.Incoming.Help;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Server.Grains;

namespace Vortex.PacketHandlers.Help;

/// <summary>
/// Reporting a selfie. The client offers a single reason for these rather than the topic tree, so
/// the topic is a server-side setting instead of something the packet carries.
/// </summary>
public class CallForHelpFromSelfieMessageHandler(
    IGrainFactory grainFactory,
    ICfhTicketService tickets
) : IMessageHandler<CallForHelpFromSelfieMessage>
{
    public async ValueTask HandleAsync(
        CallForHelpFromSelfieMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        int topicId = await grainFactory
            .GetServerConfigGrain()
            .GetIntAsync(
                ModerationConfig.SelfieReportTopicKey,
                ModerationConfig.SelfieReportTopicDefault
            )
            .ConfigureAwait(false);

        string description = string.IsNullOrWhiteSpace(message.Message)
            ? $"Reported selfie {message.Url} (furni {message.FurniId})."
            : $"{message.Message}\n\nReported selfie {message.Url} (furni {message.FurniId}).";

        await CfhReportHelper
            .SubmitAsync(
                grainFactory,
                tickets,
                ctx.PlayerId,
                topicId,
                message.PhotoAuthorId,
                message.RoomId > 0 ? message.RoomId : null,
                description,
                [],
                ct
            )
            .ConfigureAwait(false);
    }
}
