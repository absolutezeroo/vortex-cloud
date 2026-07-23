using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Inventory.Snapshots;

namespace Vortex.Primitives.Players.Grains;

/// <summary>Owns a player's avatar-effect inventory (the <c>player_effects</c> table): the list sent at
/// login, granting from the catalog, activating a fresh effect (starting its countdown), selecting the
/// worn effect, and timed expiry. Mirrors <see cref="IPlayerBadgeGrain"/>'s DB-gateway shape.</summary>
public interface IPlayerEffectGrain : IGrainWithIntegerKey
{
    /// <summary>The grouped inventory list for the <c>AvatarEffects</c> composer (expired timed effects
    /// are purged first, so the result is always current).</summary>
    public Task<ImmutableArray<AvatarEffectSnapshot>> GetEffectsAsync(CancellationToken ct);

    /// <summary>Grants a new (inactive) effect instance — from a catalog purchase, wired reward, etc. —
    /// and pushes <c>AvatarEffectAdded</c> to the owner. <paramref name="durationSeconds"/> 0 = permanent.</summary>
    public Task AddEffectAsync(
        int effectId,
        int subType,
        int durationSeconds,
        CancellationToken ct
    );

    /// <summary>Starts the countdown on an inactive instance of <paramref name="effectId"/> and pushes
    /// <c>AvatarEffectActivated</c>. No-op if none is owned/inactive.</summary>
    public Task ActivateEffectAsync(int effectId, CancellationToken ct);

    /// <summary>Marks an activated/permanent instance of <paramref name="effectId"/> as worn (0 = remove),
    /// pushes the <c>AvatarEffectSelected</c> ack, and returns the effect id to apply in the room (0 if the
    /// selection was cleared or the effect is not wearable).</summary>
    public Task<int> SelectEffectAsync(int effectId, CancellationToken ct);

    /// <summary>The currently worn effect id (0 if none) — used to re-apply the effect when the player
    /// enters a room or reconnects.</summary>
    public Task<int> GetSelectedEffectAsync(CancellationToken ct);
}
