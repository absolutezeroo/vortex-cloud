using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Vortex.Dashboard.API.Admin.Hotel;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Primitives.Content;
using Vortex.Primitives.Players.Providers;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Dashboard.Tests;

/// <summary>
/// Taking an item back off a player: the counterpart of the grant, and the guards that keep it from
/// deleting a row another part of the runtime owns.
/// </summary>
/// <remarks>
/// The guards are the point. A placed item belongs to its room's live state, and an item id with no
/// owner check would let a typo take a stranger's furniture -- neither is something the compiler or
/// the endpoint can catch, and both delete real property when they go wrong.
/// </remarks>
public sealed class ContentAdminItemRevokeTests
{
    [Fact]
    public async Task An_item_in_the_owners_hand_is_taken_and_the_inventory_is_reloaded()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedAsync(options, itemId: 500, ownerId: 7, roomId: null);

        bool reloaded = false;
        ContentAdminResult result = await Service(options, () => reloaded = true)
            .RevokeFurnitureAsync(7, 500, CancellationToken.None);

        result.Success.Should().BeTrue();
        await using VortexDbContext db = new(options);
        (await db.Furnitures.CountAsync()).Should().Be(0);

        // The grain caches the furniture list from activation, so the row going is only half of it:
        // without this the player keeps seeing what they owned a moment ago.
        reloaded
            .Should()
            .BeTrue("the inventory grain holds a cached list the row change does not reach");
    }

    [Fact]
    public async Task An_item_standing_in_a_room_is_refused()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedAsync(options, itemId: 501, ownerId: 7, roomId: 42);

        bool reloaded = false;
        ContentAdminResult result = await Service(options, () => reloaded = true)
            .RevokeFurnitureAsync(7, 501, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("item_is_placed");

        await using VortexDbContext db = new(options);
        (await db.Furnitures.CountAsync()).Should().Be(1, "the room still owns it");
        reloaded.Should().BeFalse("nothing changed, so nothing to reload");
    }

    [Fact]
    public async Task An_item_belonging_to_someone_else_is_refused()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedAsync(options, itemId: 502, ownerId: 7, roomId: null);

        // The id exists and is not placed: only the owner check stands between a typo and someone
        // else's furniture.
        ContentAdminResult result = await Service(options, () => { })
            .RevokeFurnitureAsync(8, 502, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("item_not_owned");

        await using VortexDbContext db = new(options);
        (await db.Furnitures.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task An_item_that_does_not_exist_is_refused()
    {
        ContentAdminResult result = await Service(NewOptions(), () => { })
            .RevokeFurnitureAsync(7, 9999, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("item_not_found");
    }

    // ── Harness ──────────────────────────────────────────────────────────────

    private static DbContextOptions<VortexDbContext> NewOptions() =>
        new DbContextOptionsBuilder<VortexDbContext>()
            .UseInMemoryDatabase($"item-revoke-{Guid.NewGuid():N}")
            .Options;

    private static async Task SeedAsync(
        DbContextOptions<VortexDbContext> options,
        int itemId,
        int ownerId,
        int? roomId
    )
    {
        await using VortexDbContext db = new(options);

        db.Furnitures.Add(
            new FurnitureEntity
            {
                Id = itemId,
                PlayerEntityId = ownerId,
                FurnitureDefinitionEntityId = 1,
                RoomEntityId = roomId,
            }
        );

        await db.SaveChangesAsync();
    }

    private static ContentAdminService Service(
        DbContextOptions<VortexDbContext> options,
        Action onReload
    )
    {
        IInventoryGrain inventory = FakeProxy.Create<IInventoryGrain>(call =>
        {
            if (call.Method.Name == nameof(IInventoryGrain.ReloadFurnitureAsync))
            {
                onReload();
            }

            return Task.CompletedTask;
        });

        return new ContentAdminService(
            new TestContextFactory(options),
            FakeProxy.Create<IGrainFactory>(_ => inventory),
            FakeProxy.Create<ICurrencyTypeProvider>(_ => null!),
            NullLogger<ContentAdminService>.Instance
        );
    }

    private sealed class TestContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
