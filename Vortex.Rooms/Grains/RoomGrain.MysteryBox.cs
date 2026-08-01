using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Rooms.Grains;

public sealed partial class RoomGrain
{
    public Task UseMysteryBoxAsync(
        ActionContext ctx,
        RoomObjectId boxObjectId,
        CancellationToken ct
    ) => MysteryBoxSystem.UseMysteryBoxAsync(ctx, boxObjectId, ct);

    public Task CancelMysteryBoxWaitAsync(
        ActionContext ctx,
        PlayerId boxOwnerId,
        CancellationToken ct
    ) => MysteryBoxSystem.CancelMysteryBoxWaitAsync(ctx, boxOwnerId, ct);

    public Task OpenMysteryTrophyAsync(
        ActionContext ctx,
        RoomObjectId objectId,
        string inscription,
        CancellationToken ct
    ) => MysteryBoxSystem.OpenMysteryTrophyAsync(ctx, objectId, inscription, ct);

    /// <summary>Tick-driven sweep. The client's wait dialog has no timer, so a player who wandered
    /// off would otherwise keep the box reserved forever.</summary>
    internal Task ProcessMysteryBoxTimeoutsAsync(long nowMs, CancellationToken ct) =>
        MysteryBoxSystem.ProcessMysteryBoxTimeoutsAsync(nowMs, ct);

    /// <summary>Drops any pending open a leaving player was part of, so the box is not left reserved
    /// by someone who is no longer in the room.</summary>
    internal Task CancelMysteryBoxSessionsForLeavingPlayerAsync(PlayerId playerId) =>
        MysteryBoxSystem.CancelMysteryBoxSessionsForLeavingPlayerAsync(playerId);

    /// <summary>Drops the pending open attached to a box that is being removed from the room.</summary>
    internal Task CancelMysteryBoxSessionForItemAsync(RoomObjectId boxObjectId) =>
        MysteryBoxSystem.CancelMysteryBoxSessionForItemAsync(boxObjectId);
}
