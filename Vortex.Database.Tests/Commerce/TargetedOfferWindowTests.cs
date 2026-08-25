using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Vortex.Catalog.Grains;
using Vortex.Database.Commerce;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Primitives.Catalog.Grains;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Events;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Database.Tests.Commerce;

/// <summary>
/// A targeted offer grants its products one unit at a time, and each of those unit grants is its own
/// commit. The whole loop runs inside the wallet's compensated scope, so a failure on the k+1st unit
/// refunds the entire price while the first k units stay in the player's inventory. The purchase
/// counter that enforces the per-player limit is incremented afterwards, outside everything.
/// <para>
/// Characterisation, like the catalog windows: this records what happens today so the fix has
/// something to change. Flipped by PR-C4.
/// </para>
/// </summary>
public sealed class TargetedOfferWindowTests : IDisposable
{
    private const int PLAYER = 21;
    private const int OFFER_ID = 310;
    private const int PRICE = 40;

    private readonly InventoryGrainFixture _inventory = new(PLAYER);
    private readonly List<WalletDebitRequest> _debits = [];
    private readonly List<WalletDebitRequest> _refunds = [];

    public void Dispose() => _inventory.Dispose();

    /// <summary>
    /// WINDOW A5, closed. Three units used to be three commits, so a failure on the third left all
    /// three committed and refunded the whole price — three pieces of furniture for nothing. Every
    /// copy the offer promises now lands in one commit, and that commit is the pivot: the
    /// notification that fails afterwards cannot take the purchase back with it.
    /// </summary>
    [Fact]
    public async Task ANotificationThatFailsMidGrant_KeepsEveryUnitAndTheCharge()
    {
        _inventory.Fails = CommerceFaultStep.FurnitureNotification;
        _inventory.FailFurnitureNotificationAfter = 2;

        PlayerTargetedOfferGrain grain = BuildGrain(limit: 5, perOfferQuantity: 1);

        await grain.PurchaseAsync(OFFER_ID, 3, CancellationToken.None);

        (await _inventory.FurnitureRowsAsync()).Should().Be(3, "one commit, all three copies");
        _refunds.Should().BeEmpty("there is no refund past the pivot");
        (await PurchaseCountAsync()).Should().Be(3, "and the allowance was spent for them");
    }

    /// <summary>
    /// WINDOW A5b, closed. The counter that enforces the per-player limit used to be a bare
    /// increment after the fact, outside everything: a crash between the grant and it left the
    /// player holding the furniture with their whole allowance intact. It is a step of the operation
    /// now, and its receipt commits with it.
    /// </summary>
    [Fact]
    public async Task AFailureAfterTheGrant_KeepsTheGoodsAndSpendsTheAllowance()
    {
        _inventory.Fails = CommerceFaultStep.FurnitureNotification;
        _inventory.FailFurnitureNotificationAfter = 1;

        PlayerTargetedOfferGrain grain = BuildGrain(limit: 5, perOfferQuantity: 2);

        await grain.PurchaseAsync(OFFER_ID, 1, CancellationToken.None);

        (await _inventory.FurnitureRowsAsync()).Should().Be(2, "both copies committed");
        (await PurchaseCountAsync()).Should().Be(1, "and the allowance moved with them");
    }

    [Fact]
    public async Task ACompletePurchase_GrantsEveryUnitAndSpendsTheAllowance()
    {
        PlayerTargetedOfferGrain grain = BuildGrain(limit: 5, perOfferQuantity: 2);

        await grain.PurchaseAsync(OFFER_ID, 3, CancellationToken.None);

        (await _inventory.FurnitureRowsAsync()).Should().Be(6, "two per unit, three units");
        (await PurchaseCountAsync()).Should().Be(3);
        _debits.Should().ContainSingle().Which.Amount.Should().Be(PRICE * 3);
        _refunds.Should().BeEmpty();
    }

    /// <summary>
    /// Two purchases of the same offer spend two units of allowance. The receipt is keyed by
    /// operation, not by offer — deduplicating by offer would have meant a player could buy a
    /// limited offer exactly once ever.
    /// </summary>
    [Fact]
    public async Task TwoPurchasesOfTheSameOffer_EachSpendTheirAllowance()
    {
        PlayerTargetedOfferGrain grain = BuildGrain(limit: 5, perOfferQuantity: 1);

        await grain.PurchaseAsync(OFFER_ID, 1, CancellationToken.None);
        await grain.PurchaseAsync(OFFER_ID, 1, CancellationToken.None);

        (await PurchaseCountAsync()).Should().Be(2);
        (await _inventory.FurnitureRowsAsync()).Should().Be(2);
    }

    private async Task<int> PurchaseCountAsync()
    {
        await using VortexDbContext db = new(_inventory.DbOptions);

        PlayerTargetedOfferEntity? row = await db.PlayerTargetedOffers.FirstOrDefaultAsync(r =>
            r.PlayerEntityId == PLAYER && r.TargetedOfferEntityId == OFFER_ID
        );

        return row?.PurchaseCount ?? 0;
    }

    private PlayerTargetedOfferGrain BuildGrain(int limit, int perOfferQuantity)
    {
        TargetedOfferDefinitionSnapshot definition = new()
        {
            Id = OFFER_ID,
            Identifier = "window_deal",
            OfferType = 1,
            Title = "Window",
            Description = string.Empty,
            ImageUrl = string.Empty,
            IconImageUrl = string.Empty,
            ProductCode = "code",
            PriceInCredits = PRICE,
            PriceInActivityPoints = 0,
            ActivityPointType = 0,
            PurchaseLimit = limit,
            ExpiresAt = DateTime.Now.AddDays(1),
            SortOrder = 0,
            Products =
            [
                new TargetedOfferProductSnapshot
                {
                    ProductCode = "code",
                    FurnitureDefinitionId = InventoryGrainFixture.FLOOR_DEFINITION_ID,
                    Quantity = perOfferQuantity,
                },
            ],
        };

        PlayerTargetedOfferGrain grain = new(
            BuildGrainFactory(definition),
            _inventory.DbContextFactory,
            FakeProxy.Create<IEventPublisher>(_ => Task.CompletedTask),
            new CommerceJournal(
                _inventory.DbContextFactory,
                FakeProxy.Create<IVortexMetrics>(_ => null),
                NullLogger<CommerceJournal>.Instance
            ),
            NullLogger<PlayerTargetedOfferGrain>.Instance
        );

        GrainContexts.Install(grain, "playertargetedoffer", PLAYER);

        return grain;
    }

    private IGrainFactory BuildGrainFactory(TargetedOfferDefinitionSnapshot definition)
    {
        IPlayerWalletGrain wallet = FakeProxy.Create<IPlayerWalletGrain>(call =>
        {
            switch (call.Method.Name)
            {
                case nameof(IPlayerWalletGrain.TryDebitAsync):
                    _debits.AddRange((List<WalletDebitRequest>)call.Args![0]!);

                    return Task.FromResult(WalletDebitResult.Success());

                case nameof(IPlayerWalletGrain.CreditBackAsync):
                    _refunds.AddRange((List<WalletDebitRequest>)call.Args![0]!);

                    return Task.CompletedTask;

                default:
                    return null;
            }
        });

        ITargetedOfferManagerGrain manager = FakeProxy.Create<ITargetedOfferManagerGrain>(call =>
            call.Method.Name == nameof(ITargetedOfferManagerGrain.GetDefinitionsAsync)
                ? Task.FromResult(ImmutableArray.Create(definition))
                : null
        );

        return FakeProxy.Create<IGrainFactory>(call =>
        {
            if (!call.Method.IsGenericMethod)
            {
                return null;
            }

            Type grainType = call.Method.GetGenericArguments()[0];

            return grainType switch
            {
                Type t when t == typeof(IPlayerWalletGrain) => wallet,
                Type t when t == typeof(IInventoryGrain) => _inventory.Grain,
                Type t when t == typeof(ITargetedOfferManagerGrain) => manager,
                _ => null,
            };
        });
    }
}
