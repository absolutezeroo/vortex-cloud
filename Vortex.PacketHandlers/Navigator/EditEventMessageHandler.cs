using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.Navigator;

/// <summary>Edits the ad copy of a currently-active room advertisement -- see
/// CancelEventMessageHandler for the "Event" naming note.</summary>
public class EditEventMessageHandler(IRoomAdvertisementService roomAdvertisements)
    : IMessageHandler<EditEventMessage>
{
    public async ValueTask HandleAsync(
        EditEventMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.Id <= 0 || string.IsNullOrWhiteSpace(message.Name))
        {
            return;
        }

        RoomAdvertisementSnapshot? updated = await roomAdvertisements
            .EditAsync(message.Id, ctx.PlayerId, message.Name, message.Description, ct)
            .ConfigureAwait(false);

        if (updated is null)
        {
            return;
        }

        int minutesRemaining = Math.Max(0, (int)(updated.ExpiresAt - DateTime.UtcNow).TotalMinutes);

        await ctx.SendComposerAsync(
                new RoomEventMessageComposer
                {
                    RoomId = updated.RoomId,
                    Name = updated.Name,
                    Description = updated.Description,
                    CategoryId = updated.CategoryId,
                    MinutesRemaining = minutesRemaining,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
