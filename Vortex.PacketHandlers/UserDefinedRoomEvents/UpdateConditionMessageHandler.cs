using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents;

public class UpdateConditionMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UpdateConditionMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        UpdateConditionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.Id <= 0)
        {
            return;
        }

        if (
            !await _grainFactory
                .GetRoomFurni(ctx.RoomId)
                .ApplyWiredUpdateAsync(ctx.AsActionContext(), message.Id, message.ToRequest(), ct)
                .ConfigureAwait(false)
        )
        {
            return;
        }

        await ctx.SendComposerAsync(new WiredSaveSuccessEventMessageComposer(), ct)
            .ConfigureAwait(false);
    }
}
