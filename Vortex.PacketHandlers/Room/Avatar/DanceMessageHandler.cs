using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Quests;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.PacketHandlers.Room.Avatar;

public class DanceMessageHandler(IGrainFactory grainFactory) : IMessageHandler<DanceMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        DanceMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        if (
            !await _grainFactory
                .GetRoomGrain(ctx.RoomId)
                .SetAvatarDanceAsync(ctx.AsActionContext(), (AvatarDanceType)message.DanceId, ct)
                .ConfigureAwait(false)
        )
        {
            return;
        }

        // Only an actual dance (not "stop dancing") advances a Dance quest.
        if (message.DanceId != 0)
        {
            await _grainFactory
                .GetPlayerQuestGrain(ctx.PlayerId)
                .ProgressAsync(QuestTypes.Dance, 1, ct)
                .ConfigureAwait(false);
        }
    }
}
