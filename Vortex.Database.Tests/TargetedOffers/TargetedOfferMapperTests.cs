using System;
using System.Collections.Immutable;
using FluentAssertions;
using Vortex.Catalog.TargetedOffers;
using Vortex.Primitives.Catalog.Snapshots;
using Xunit;

namespace Vortex.Database.Tests.TargetedOffers;

public class TargetedOfferMapperTests
{
    private static readonly DateTime Now = new(2026, 7, 19, 12, 0, 0);

    private static TargetedOfferDefinitionSnapshot Definition(
        int purchaseLimit = 1,
        DateTime? expiresAt = null
    ) =>
        new()
        {
            Id = 1,
            Identifier = "starter_bundle",
            OfferType = 1,
            Title = "Starter Bundle",
            Description = "desc",
            ImageUrl = "img",
            IconImageUrl = "icon",
            ProductCode = "table",
            PriceInCredits = 25,
            PriceInActivityPoints = 0,
            ActivityPointType = 0,
            PurchaseLimit = purchaseLimit,
            ExpiresAt = expiresAt,
            SortOrder = 0,
            Products =
            [
                new TargetedOfferProductSnapshot
                {
                    ProductCode = "table",
                    FurnitureDefinitionId = 17,
                    Quantity = 1,
                },
                new TargetedOfferProductSnapshot
                {
                    ProductCode = "chair",
                    FurnitureDefinitionId = 18,
                    Quantity = 2,
                },
            ],
        };

    [Theory]
    [InlineData(1, 0, 1)] // limit 1, none bought -> 1 left
    [InlineData(3, 1, 2)] // limit 3, one bought -> 2 left
    [InlineData(1, 1, 0)] // limit reached -> 0 left
    [InlineData(1, 5, 0)] // over the limit -> clamped to 0
    public void RemainingPurchases_ReflectsLimitAndCount(int limit, int bought, int expected)
    {
        TargetedOfferMapper
            .RemainingPurchases(Definition(purchaseLimit: limit), bought)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void RemainingPurchases_ZeroLimit_IsUnlimited()
    {
        TargetedOfferMapper
            .RemainingPurchases(Definition(purchaseLimit: 0), 100)
            .Should()
            .Be(int.MaxValue);
    }

    [Fact]
    public void ExpirationSeconds_CountsDownAndClamps()
    {
        TargetedOfferMapper
            .ExpirationSeconds(Definition(expiresAt: Now.AddHours(1)), Now)
            .Should()
            .Be(3600);
        TargetedOfferMapper
            .ExpirationSeconds(Definition(expiresAt: Now.AddHours(-1)), Now)
            .Should()
            .Be(0);
        TargetedOfferMapper.ExpirationSeconds(Definition(expiresAt: null), Now).Should().Be(0);
    }

    [Theory]
    [InlineData(1, 0, true)] // available
    [InlineData(1, 1, false)] // over limit
    public void CanPurchase_EnforcesLimit(int limit, int bought, bool expected)
    {
        TargetedOfferMapper
            .CanPurchase(Definition(purchaseLimit: limit), bought, Now)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void CanPurchase_ExpiredOffer_IsFalse()
    {
        TargetedOfferMapper
            .CanPurchase(Definition(expiresAt: Now.AddSeconds(-1)), 0, Now)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void ToWire_MapsFieldsRemainingExpiryAndSubProducts()
    {
        TargetedOfferSnapshot wire = TargetedOfferMapper.ToWire(
            Definition(purchaseLimit: 3, expiresAt: Now.AddHours(2)),
            purchaseCount: 1,
            trackingState: 4,
            now: Now
        );

        wire.Id.Should().Be(1);
        wire.Identifier.Should().Be("starter_bundle");
        wire.ProductCode.Should().Be("table");
        wire.PriceInCredits.Should().Be(25);
        wire.TrackingState.Should().Be(4);
        wire.PurchaseLimit.Should().Be(2); // 3 - 1
        wire.ExpirationSeconds.Should().Be(7200);
        wire.SubProductCodes.Should().Equal("table", "chair");
    }
}
