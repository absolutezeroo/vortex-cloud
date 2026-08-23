using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

public class SetMannequinNameMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetMannequinNameMessage>
{
    /// <summary>The widget's own input field is bounded; this is the server-side floor under it.</summary>
    private const int MaxNameLength = 64;

    public async ValueTask HandleAsync(
        SetMannequinNameMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        string name =
            message.Name.Length > MaxNameLength ? message.Name[..MaxNameLength] : message.Name;

        await grainFactory
            .GetRoomFurni(ctx.RoomId)
            .SetMannequinNameAsync(ctx.AsActionContext(), message.ObjectId, name, ct)
            .ConfigureAwait(false);
    }
}
