using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Catalog;

namespace Turbo.PacketHandlers.Catalog;

/// <summary>
/// Deliberately deferred: Builders Club catalog purchases place furniture directly into the room at
/// purchase time, skipping the inventory step every other purchase path uses -- there's no existing
/// "create furniture directly in a room" room/furniture orchestration to hook into yet. The
/// subscription tier + furni-count reporting (BuildersClubQueryFurniCountMessageHandler,
/// BuildersClubSubscriptionStatusMessageComposer) are wired; this is a separate, larger piece.
/// </summary>
public class BuildersClubPlaceRoomItemMessageHandler
    : IMessageHandler<BuildersClubPlaceRoomItemMessage>
{
    public async ValueTask HandleAsync(
        BuildersClubPlaceRoomItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
