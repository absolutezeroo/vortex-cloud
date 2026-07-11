using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Navigator;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Navigator;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Navigator;

/// <summary>Opens the "create room event" (room-ad purchase) panel: sends the available event
/// categories plus whether the player's current room is eligible to advertise. Real Habbo bundles
/// both responses for this one client request.</summary>
public class GetUserEventCatsMessageHandler(
    IGrainFactory grainFactory,
    INavigatorProvider navigatorProvider
) : IMessageHandler<GetUserEventCatsMessage>
{
    public async ValueTask HandleAsync(
        GetUserEventCatsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ImmutableArray<NavigatorEventCategorySnapshot> categories = await navigatorProvider
            .GetEventCategoriesAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new UserEventCatsMessageComposer { EventCategories = categories },
                ct
            )
            .ConfigureAwait(false);

        bool canCreate = false;
        int errorCode = 1;

        if (ctx.RoomId > 0)
        {
            IRoomGrain roomGrain = grainFactory.GetRoomGrain(ctx.RoomId);
            RoomSnapshot snapshot = await roomGrain.GetSnapshotAsync().ConfigureAwait(false);

            if (snapshot.OwnerId == ctx.PlayerId)
            {
                canCreate = true;
                errorCode = 0;
            }
            else
            {
                errorCode = 2;
            }
        }

        await ctx.SendComposerAsync(
                new CanCreateRoomEventMessageComposer
                {
                    CanCreateEvent = canCreate,
                    ErrorCode = errorCode,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
