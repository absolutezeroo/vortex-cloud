using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.MysteryBox.Admin;

namespace Vortex.Primitives.MysteryBox;

/// <summary>
/// CRUD for <c>mystery_box_prizes</c> plus the staff box/key grants, used by the dashboard's mystery
/// box surface. Every write reloads the <see cref="Grains.IMysteryBoxManagerGrain"/> cache so the
/// live pools never drift from the database — see the implementation.
/// </summary>
public interface IMysteryBoxAdminService
{
    Task<MysteryBoxAdminResult> CreatePrizeAsync(MysteryBoxPrizeSpec spec, CancellationToken ct);
    Task<MysteryBoxAdminResult> UpdatePrizeAsync(
        int prizeId,
        MysteryBoxPrizeSpec spec,
        CancellationToken ct
    );
    Task<MysteryBoxAdminResult> DeletePrizeAsync(int prizeId, CancellationToken ct);

    /// <summary>Hands a key to a player. Keys are not furniture, so this is the only staff route
    /// into a player's key inventory.</summary>
    Task<MysteryBoxAdminResult> GrantKeyAsync(
        int playerId,
        string color,
        string actor,
        CancellationToken ct
    );

    /// <summary>
    /// Hands a player a mystery box in <paramref name="color"/>. The generic "grant item" operation
    /// can create the same furniture, but only at the colourless state 0 — a box that can never be
    /// paired with a key. This bakes the colour into the item's state and refuses a definition that
    /// does not run the mystery box logic.
    /// </summary>
    Task<MysteryBoxAdminResult> GrantBoxAsync(
        int playerId,
        int furnitureDefinitionId,
        string color,
        string actor,
        CancellationToken ct
    );

    /// <summary>Rebuilds the live cache from the database without an emulator restart.</summary>
    Task<MysteryBoxAdminResult> ReloadCacheAsync(CancellationToken ct);
}
