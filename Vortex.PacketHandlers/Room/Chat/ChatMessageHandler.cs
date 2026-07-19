using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Chat;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Quests;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Chat;

public class ChatMessageHandler(IGrainFactory grainFactory) : IMessageHandler<ChatMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        ChatMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        IRoomGrain roomChatGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

        await roomChatGrain
            .SendChatFromPlayerAsync(
                ctx.PlayerId,
                message.Text,
                0,
                message.StyleId,
                [],
                message.TrackingId
            )
            .ConfigureAwait(false);

        await _grainFactory
            .GetPlayerQuestGrain(ctx.PlayerId)
            .ProgressAsync(QuestTypes.Chat, 1, ct)
            .ConfigureAwait(false);
    }
}
