using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Catalog;

namespace Turbo.PacketHandlers.Catalog;

/// <summary>See BuildersClubPlaceRoomItemMessageHandler -- same deferred direct-to-room placement
/// orchestration gap, for wall items.</summary>
public class BuildersClubPlaceWallItemMessageHandler
    : IMessageHandler<BuildersClubPlaceWallItemMessage>
{
    public async ValueTask HandleAsync(
        BuildersClubPlaceWallItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
