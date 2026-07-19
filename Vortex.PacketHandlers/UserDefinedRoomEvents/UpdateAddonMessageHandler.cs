using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents;

public class UpdateAddonMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UpdateAddonMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        UpdateAddonMessage message,
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
                .GetRoomGrain(ctx.RoomId)
                .ApplyWiredUpdateAsync(ctx.AsActionContext(), message.Id, message, ct)
                .ConfigureAwait(false)
        )
        {
            return;
        }

        _ = ctx.SendComposerAsync(new WiredSaveSuccessEventMessageComposer(), ct)
            .ConfigureAwait(false);
    }
}
