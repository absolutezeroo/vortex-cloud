using System;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Furniture.Enums;
using Xunit;

namespace Vortex.Database.Tests.Commerce;

/// <summary>
/// What a catalog grant leaves behind when one of its later steps fails. The grant is not one
/// commit: furniture and badges land together, pets in a commit of their own, bots in another, and
/// effects in a different grain entirely. The wallet's shared purchase primitive refunds the whole
/// price if any of them throws — an invariant written for a grant that was atomic, and kept as the
/// grant stopped being atomic one product family at a time.
/// <para>
/// These are characterisation tests: they record what the code does today, window by window, so the
/// fix has something to change rather than something to argue with. Each one names the PR that
/// flips its assertion.
/// </para>
/// </summary>
public sealed class CatalogGrantWindowTests
{
    /// <summary>An offer with a chair, a badge, a pet and an effect — one product per commit
    /// boundary, which is what makes the windows visible.</summary>
    private static CatalogProductSnapshot[] MultiFamilyOffer() =>
        [
            CatalogOffers.Product(1, ProductType.Floor),
            CatalogOffers.Product(
                2,
                ProductType.Badge,
                extraParam: CommerceFaultHarness.BADGE_CODE
            ),
            CatalogOffers.Product(3, ProductType.Pet, extraParam: "1"),
            CatalogOffers.Product(
                4,
                ProductType.Effect,
                extraParam: $"{CommerceFaultHarness.EFFECT_ID}:0:0"
            ),
        ];

    [Fact]
    public async Task ACompleteGrant_DeliversEveryFamilyAndChargesOnce()
    {
        using CommerceFaultHarness harness = new(MultiFamilyOffer());

        await harness.BuyAsync(extraParam: "Rex");

        (await harness.FurnitureRowsAsync()).Should().Be(1);
        (await harness.BadgeRowsAsync()).Should().Be(1);
        (await harness.PetRowsAsync()).Should().Be(1);
        harness.EffectsGranted.Should().ContainSingle();
        harness.Debits.Should().ContainSingle();
        harness.Refunds.Should().BeEmpty();
    }

    /// <summary>
    /// WINDOW A1 — the largest one. The effect grant is the last step of the whole purchase, and its
    /// own comment in <c>InventoryGrain.Furni.cs</c> says a throw there makes the purchase
    /// auto-refund. By then the furniture, the badge and the pet are committed and will stay
    /// committed: the refund undoes the payment and nothing undoes the delivery.
    /// </summary>
    /// <remarks>
    /// Flipped by PR-C4: the furniture, badge and pet rows join one commit, and the effect becomes a
    /// journalled step that is retried rather than a reason to refund a delivered purchase.
    /// </remarks>
    [Fact]
    public async Task AnEffectThatFailsLast_KeepsTheGoodsAndRefundsThePrice()
    {
        using CommerceFaultHarness harness = new(MultiFamilyOffer())
        {
            Fails = CommerceFaultStep.EffectGrant,
        };

        Func<Task> act = () => harness.BuyAsync(extraParam: "Rex");

        await act.Should().ThrowAsync<InvalidOperationException>();

        // Committed and staying committed.
        (await harness.FurnitureRowsAsync())
            .Should()
            .Be(1);
        (await harness.BadgeRowsAsync()).Should().Be(1);
        (await harness.PetRowsAsync()).Should().Be(1);

        // And paid back in full.
        harness
            .Refunds.Should()
            .ContainSingle("the shared purchase primitive refunds the whole price on any throw");

        harness
            .EffectsGranted.Should()
            .BeEmpty("the step that failed is the one that delivered nothing");
    }

    /// <summary>
    /// WINDOW A1b — the same shape one step earlier. The pet row is committed by
    /// <c>CreatePetAsync</c>; its presence notification is a separate call that can fail on its own,
    /// and does so after the furniture, the badge and the pet are all durable.
    /// </summary>
    /// <remarks>Flipped by PR-C4 for the same reason.</remarks>
    [Fact]
    public async Task APetNotificationThatFails_KeepsThePetAndRefundsThePrice()
    {
        using CommerceFaultHarness harness = new(MultiFamilyOffer())
        {
            Fails = CommerceFaultStep.PetNotification,
        };

        Func<Task> act = () => harness.BuyAsync(extraParam: "Rex");

        await act.Should().ThrowAsync<InvalidOperationException>();

        (await harness.FurnitureRowsAsync()).Should().Be(1);
        (await harness.BadgeRowsAsync()).Should().Be(1);
        (await harness.PetRowsAsync())
            .Should()
            .Be(1, "CreatePetAsync commits the row before anyone is told about it");
        harness.Refunds.Should().ContainSingle();
    }

    /// <summary>
    /// The bot row commits in <c>CreateBotAsync</c> and the composer that opens the inventory is
    /// sent afterwards — a third commit boundary in the same grant, and a third way to end up with
    /// goods and a refund.
    /// </summary>
    /// <remarks>Flipped by PR-C4.</remarks>
    [Fact]
    public async Task ABotNotificationThatFails_KeepsTheBotAndRefundsThePrice()
    {
        using CommerceFaultHarness harness = new([
            CatalogOffers.Product(1, ProductType.Floor),
            CatalogOffers.Product(
                2,
                ProductType.Robot,
                extraParam: "name:Robbie;figure:hd-180-1;gender:m"
            ),
        ])
        {
            Fails = CommerceFaultStep.BotNotification,
        };

        Func<Task> act = () => harness.BuyAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();

        (await harness.FurnitureRowsAsync()).Should().Be(1);
        (await harness.BotRowsAsync()).Should().Be(1);
        harness.Refunds.Should().ContainSingle();
    }

    /// <summary>
    /// A bundle is one offer holding several products, and any product may carry more than one copy.
    /// The copies granted are the purchase quantity times the product's own count — the arithmetic
    /// that collapsed every bundle to one of each item when it was missed once already, and the one
    /// the single-commit consolidation must not disturb.
    /// </summary>
    [Fact]
    public async Task ABundleGrantsQuantityTimesProductCount()
    {
        using CommerceFaultHarness harness = new([
            CatalogOffers.Product(1, ProductType.Floor, quantity: 3),
        ]);

        await harness.BuyAsync(quantity: 2);

        (await harness.FurnitureRowsAsync()).Should().Be(6);
    }

    /// <summary>
    /// A badge the player already owns is skipped rather than duplicated. The guard runs inside the
    /// same commit as the furniture, so the consolidation has to carry it across unchanged.
    /// </summary>
    [Fact]
    public async Task ABadgeAlreadyOwned_IsNotGrantedTwice()
    {
        using CommerceFaultHarness harness = new([
            CatalogOffers.Product(
                1,
                ProductType.Badge,
                extraParam: CommerceFaultHarness.BADGE_CODE
            ),
        ]);

        await harness.BuyAsync();
        await harness.BuyAsync();

        (await harness.BadgeRowsAsync()).Should().Be(1);
    }
}
