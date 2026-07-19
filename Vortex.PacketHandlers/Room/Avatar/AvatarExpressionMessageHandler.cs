using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Quests;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Room.Avatar;

public class AvatarExpressionMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<AvatarExpressionMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        AvatarExpressionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.ExpressionId < 0)
        {
            return;
        }

        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(ctx.RoomId);

        AvatarExpressionType expression = (AvatarExpressionType)message.ExpressionId;

        await roomGrain
            .SetAvatarExpressionAsync(ctx.AsActionContext(), expression, ct)
            .ConfigureAwait(false);

        if (expression == AvatarExpressionType.Wave)
        {
            await _grainFactory
                .GetPlayerQuestGrain(ctx.PlayerId)
                .ProgressAsync(QuestTypes.Wave, 1, ct)
                .ConfigureAwait(false);
        }
    }
}
