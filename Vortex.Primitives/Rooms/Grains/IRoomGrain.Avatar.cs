using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomGrain
{
    public Task<bool> CreateAvatarFromPlayerAsync(
        ActionContext ctx,
        PlayerSummarySnapshot snapshot,
        CancellationToken ct
    );
    public Task<bool> RemoveAvatarFromPlayerAsync(
        ActionContext ctx,
        PlayerId playerId,
        CancellationToken ct
    );
    public Task<bool> WalkAvatarToAsync(
        ActionContext ctx,
        int targetX,
        int targetY,
        CancellationToken ct
    );
    public Task<bool> UpdateAvatarWithPlayerAsync(
        PlayerSummarySnapshot snapshot,
        CancellationToken ct
    );
    public Task<bool> SetAvatarDanceAsync(
        ActionContext ctx,
        AvatarDanceType danceType,
        CancellationToken ct
    );
    public Task<bool> SetAvatarExpressionAsync(
        ActionContext ctx,
        AvatarExpressionType expressionType,
        CancellationToken ct
    );
    public Task SendChatFromPlayerAsync(
        PlayerId playerId,
        string text,
        AvatarGestureType gesture,
        int styleId,
        List<(string, string, bool)> links,
        int trackingId,
        PlayerId? targetPlayerId = null
    );

    public Task<ImmutableArray<RoomAvatarSnapshot>> GetAllAvatarSnapshotsAsync(
        CancellationToken ct
    );

    public Task<bool> SetAvatarPostureAsync(ActionContext ctx, CancellationToken ct);

    public Task<bool> SetAvatarSignAsync(ActionContext ctx, int signId, CancellationToken ct);

    public Task<bool> LookToAvatarAsync(
        ActionContext ctx,
        int targetX,
        int targetY,
        CancellationToken ct
    );

    public Task SetAvatarTypingAsync(ActionContext ctx, bool isTyping, CancellationToken ct);

    /// <summary>The acting player (<paramref name="ctx"/>) gives a respect point to
    /// <paramref name="targetPlayerId"/> if present in the room and within the daily budget.</summary>
    public Task RespectPlayerAsync(
        ActionContext ctx,
        int targetPlayerId,
        int dailyLimit,
        CancellationToken ct
    );
}
