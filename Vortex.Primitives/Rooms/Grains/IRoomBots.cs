using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>Bots standing in the room: placement and removal.</summary>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomBots")]
public interface IRoomBots : IGrainWithIntegerKey
{
    /// <summary>
    /// Drops a bot from the owner's inventory onto a tile. Null when the bot does not exist, is not
    /// the actor's, is already somewhere, or the tile will not take it.
    /// </summary>
    public Task<BotSnapshot?> PlaceBotAsync(
        ActionContext ctx,
        int botId,
        int x,
        int y,
        CancellationToken ct
    );

    /// <summary>Picks a bot back up into its owner's inventory. False if it was not here.</summary>
    public Task<bool> RemoveBotAsync(ActionContext ctx, int botId, CancellationToken ct);

    public Task<ImmutableArray<RoomAvatarSnapshot>> GetPlacedBotAvatarSnapshotsAsync(
        CancellationToken ct
    );
}
