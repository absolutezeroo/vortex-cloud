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
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Primitives.Catalog.Grains;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Events;
using Vortex.Primitives.Inventory.Grains;
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
    /// WINDOW A5 — three units bought, the third fails, the first two stay granted and the whole
    /// price comes back. Two pieces of furniture for nothing.
    /// </summary>
    [Fact]
    public async Task AUnitThatFailsMidLoop_LeavesTheEarlierUnitsGrantedAndRefundsEverything()
    {
        _inventory.Fails = CommerceFaultStep.FurnitureNotification;
        _inventory.FailFurnitureNotificationAfter = 2;

        PlayerTargetedOfferGrain grain = BuildGrain(limit: 5, perOfferQuantity: 1);

        Func<Task> act = () => grain.PurchaseAsync(OFFER_ID, 3, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        (await _inventory.FurnitureRowsAsync())
            .Should()
            .Be(
                3,
                "each unit grant commits its row before anyone is told about it, so all three are "
                    + "durable by the time the third notification throws"
            );

        _refunds
            .Should()
            .ContainSingle()
            .Which.Amount.Should()
            .Be(PRICE * 3, "the compensated scope refunds the whole purchase, not the failed unit");
    }

    /// <summary>
    /// WINDOW A5b — the counter that enforces the per-player limit is written after the grant
    /// succeeds and outside the operation. A crash between the two loses the increment: the player
    /// keeps the furniture and keeps their whole allowance.
    /// </summary>
    /// <remarks>
    /// The crash is modelled by the grant throwing on the very last notification — the goods are
    /// committed, the counter never runs. Flipped by PR-C4, where the counter becomes a journalled
    /// step of the operation rather than an afterthought.
    /// </remarks>
    [Fact]
    public async Task AFailureAfterTheLastUnit_KeepsTheGoodsAndSpendsNoAllowance()
    {
        _inventory.Fails = CommerceFaultStep.FurnitureNotification;
        _inventory.FailFurnitureNotificationAfter = 1;

        PlayerTargetedOfferGrain grain = BuildGrain(limit: 5, perOfferQuantity: 2);

        Func<Task> act = () => grain.PurchaseAsync(OFFER_ID, 1, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        (await _inventory.FurnitureRowsAsync()).Should().Be(2, "both copies committed");
        (await PurchaseCountAsync()).Should().Be(0, "IncrementPurchaseCountAsync never ran");
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
