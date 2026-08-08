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

    /// <summary>
    /// Stores one skill's configuration on a bot. The data is the command's own encoding — a
    /// chatter's phrase list, a wander flag — and is kept verbatim rather than interpreted, so a
    /// skill the server has never heard of still round-trips.
    /// </summary>
    /// <returns>False if the bot is not here or the actor may not configure it.</returns>
    public Task<bool> SetBotSkillAsync(
        ActionContext ctx,
        int botId,
        int commandId,
        string data,
        CancellationToken ct
    );

    /// <summary>What a bot is set to for one skill, or null if it is not in this room.</summary>
    public Task<string?> GetBotSkillAsync(int botId, int commandId, CancellationToken ct);
}
