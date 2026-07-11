using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Navigator;

public class ToggleStaffPickMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService
) : IMessageHandler<ToggleStaffPickMessage>
{
    public async ValueTask HandleAsync(
        ToggleStaffPickMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.RoomId <= 0)
        {
            return;
        }

        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.Has(Capabilities.Navigator.StaffPick))
        {
            return;
        }

        IRoomGrain roomGrain = grainFactory.GetRoomGrain(message.RoomId);

        await roomGrain.SetStaffPickAsync(message.IsStaffPicked, ct).ConfigureAwait(false);
    }
}
