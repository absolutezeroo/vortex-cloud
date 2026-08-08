using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task<BotSnapshot?> PlaceBotAsync(
        ActionContext ctx,
        int botId,
        int x,
        int y,
        CancellationToken ct
    ) => BotSystem.PlaceBotAsync(ctx, botId, x, y, ct);

    public Task<bool> RemoveBotAsync(ActionContext ctx, int botId, CancellationToken ct) =>
        BotSystem.RemoveBotAsync(ctx, botId, ct);

    public Task<ImmutableArray<RoomAvatarSnapshot>> GetPlacedBotAvatarSnapshotsAsync(
        CancellationToken ct
    ) => BotSystem.GetPlacedBotAvatarSnapshotsAsync(ct);

    public Task<bool> SetBotSkillAsync(
        ActionContext ctx,
        int botId,
        int commandId,
        string data,
        CancellationToken ct
    ) => BotSystem.SetBotSkillAsync(ctx, botId, commandId, data, ct);

    public Task<string?> GetBotSkillAsync(int botId, int commandId, CancellationToken ct) =>
        BotSystem.GetBotSkillAsync(botId, commandId, ct);

    /// <summary>Drives one bot tick from a test; the real one rides the room clock.</summary>
    internal Task ProcessBotsForTestAsync(long nowMs) =>
        BotSystem.ProcessBotsAsync(nowMs, CancellationToken.None);
}
