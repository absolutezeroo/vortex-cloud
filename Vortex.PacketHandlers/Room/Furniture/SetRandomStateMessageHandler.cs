using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// The room engine's "randomise this item" entry point, which is what a dice is rolled through when
/// the click lands on the object rather than on the widget.
/// <para>
/// The packet carries a state, and it is deliberately ignored: letting a client choose the face a
/// dice lands on would make every dice game in the hotel decorative. The server rolls, exactly as
/// ThrowDiceMessageHandler does, and routing through <see cref="IRoomService.UseItemInRoomAsync"/>
/// keeps the same rights check as every other furni interaction.
/// </para>
/// </summary>
public class SetRandomStateMessageHandler(IRoomService roomService)
    : IMessageHandler<SetRandomStateMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        SetRandomStateMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .UseItemInRoomAsync(
                ctx.AsActionContext(),
                message.ObjectId,
                ct,
                FurnitureDiceAction.Roll
            )
            .ConfigureAwait(false);
    }
}
