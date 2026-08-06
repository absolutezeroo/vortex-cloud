using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;
using Vortex.Primitives.Moderation;

namespace Vortex.PacketHandlers.Help;

/// <summary>Reporting a photo poster. The client sends no written reason for this one, so the
/// ticket message is assembled from what it does send: which photo, and where it hangs.</summary>
public class CallForHelpFromPhotoMessageHandler(
    IGrainFactory grainFactory,
    ICfhTicketService tickets
) : IMessageHandler<CallForHelpFromPhotoMessage>
{
    public async ValueTask HandleAsync(
        CallForHelpFromPhotoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        string description = $"Reported photo {message.PhotoId} (furni {message.FurniId}).";

        await CfhReportHelper
            .SubmitAsync(
                grainFactory,
                tickets,
                ctx.PlayerId,
                message.TopicId,
                message.PhotoAuthorId,
                message.RoomId > 0 ? message.RoomId : null,
                description,
                [],
                ct
            )
            .ConfigureAwait(false);
    }
}
