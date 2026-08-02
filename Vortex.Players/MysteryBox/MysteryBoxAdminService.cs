using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.MysteryBox.Admin;
using Vortex.Primitives.Orleans;

namespace Vortex.Players.MysteryBox;

/// <summary>
/// Box-specific admin writes: handing a player a key or a box, and rebuilding the box definition
/// cache. A plain singleton (not a grain), and one that touches no database of its own — keys and
/// furniture are both grain-owned, so every write here goes through the grain that owns the row and
/// pushes the player's tracker. Writing either directly would leave a connected player's toolbar
/// showing stale state, the "DB write not reflected in live state" bug class called out in
/// AGENTS.md.
///
/// The prizes are not here: they are a shared prize pool, edited through
/// <see cref="Vortex.Primitives.Prizes.IPrizePoolAdminService"/>.
/// </summary>
internal sealed class MysteryBoxAdminService(
    IGrainFactory grainFactory,
    ILogger<MysteryBoxAdminService> logger
) : IMysteryBoxAdminService
{
    public async Task<MysteryBoxAdminResult> GrantKeyAsync(
        int playerId,
        string color,
        string actor,
        CancellationToken ct
    )
    {
        string normalizedColor = MysteryBoxColors.Normalize(color);

        if (normalizedColor.Length == 0)
        {
            return MysteryBoxAdminResult.Fail("invalid_color");
        }

        if (playerId <= 0)
        {
            return MysteryBoxAdminResult.Fail("player_required");
        }

        // The grain owns the key rows and pushes the player's tracker; writing the row here would
        // leave a connected player's toolbar showing the old colours.
        bool granted = await grainFactory
            .GetPlayerMysteryBoxGrain(playerId)
            .GrantKeyAsync(normalizedColor, $"staff:{actor}", ct)
            .ConfigureAwait(false);

        return granted
            ? MysteryBoxAdminResult.Ok(playerId)
            : MysteryBoxAdminResult.Fail("grant_failed");
    }

    public async Task<MysteryBoxAdminResult> GrantBoxAsync(
        int playerId,
        int furnitureDefinitionId,
        string color,
        string actor,
        CancellationToken ct
    )
    {
        if (playerId <= 0)
        {
            return MysteryBoxAdminResult.Fail("player_required");
        }

        string normalizedColor = MysteryBoxColors.Normalize(color);

        if (normalizedColor.Length == 0)
        {
            return MysteryBoxAdminResult.Fail("invalid_color");
        }

        if (
            !await grainFactory
                .GetMysteryBoxManagerGrain()
                .IsBoxDefinitionAsync(furnitureDefinitionId, ct)
                .ConfigureAwait(false)
        )
        {
            return MysteryBoxAdminResult.Fail("not_a_mystery_box");
        }

        // The colour is the furniture state, so it has to be baked into the item at creation: there
        // is nowhere else for it to live, and a box created at state 0 is colourless and unpairable
        // forever. The inventory grain owns furniture creation and the extra-data shape, and its add
        // path refreshes the recipient's tracker, so the box shows in their toolbar without a relog.
        await grainFactory
            .GetInventoryGrain(playerId)
            .GrantFurnitureWithLegacyStuffDataAsync(
                furnitureDefinitionId,
                MysteryBoxSprite
                    .ClosedState(normalizedColor)
                    .ToString(CultureInfo.InvariantCulture),
                ct
            )
            .ConfigureAwait(false);

        logger.LogInformation(
            "Staff {Actor} granted a {Color} mystery box (definition {DefinitionId}) to player {PlayerId}.",
            actor,
            normalizedColor,
            furnitureDefinitionId,
            playerId
        );

        return MysteryBoxAdminResult.Ok(playerId);
    }

    public async Task<MysteryBoxAdminResult> ReloadCacheAsync(CancellationToken ct)
    {
        await ReloadAsync(ct).ConfigureAwait(false);

        return MysteryBoxAdminResult.Ok(0);
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        try
        {
            await grainFactory.GetMysteryBoxManagerGrain().ReloadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The DB write already committed -- the live pools are now stale until the next reload or
            // restart. Never swallow this: it is the "DB write not reflected in live state" bug class
            // called out in AGENTS.md.
            logger.LogError(
                ex,
                "Mystery box cache reload failed after an admin write committed -- live box definitions are now stale until the next reload or restart"
            );
            throw;
        }
    }
}
