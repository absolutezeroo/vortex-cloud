using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Furniture.Configuration;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Database.Tests.Furniture;

/// <summary>
/// The two definition indexes are published as one object, so a reader sees the set that was there
/// or the set that replaced it — never the new definitions by id alongside the old ones by name.
/// </summary>
/// <remarks>
/// They used to be two fields assigned one after the other. The window is small and only an admin
/// reload opens it, but the symptom — one lookup answering from the previous catalogue while its
/// neighbour answers from the current one — is close to unrecognisable from a bug report, which is
/// the argument for closing it while it costs one object.
/// </remarks>
public sealed class FurnitureDefinitionSetTests : IDisposable
{
    private readonly DbContextOptions<VortexDbContext> _options =
        new DbContextOptionsBuilder<VortexDbContext>()
            .UseInMemoryDatabase($"furni-defs-{Guid.NewGuid():N}")
            .Options;

    public void Dispose()
    {
        using VortexDbContext db = new(_options);
        db.Database.EnsureDeleted();
    }

    [Fact]
    public async Task AReloadPublishesBothIndexesTogether()
    {
        await SeedAsync(("first_chair", 1), ("second_chair", 2));

        FurnitureDefinitionProvider provider = Build();
        await provider.ReloadAsync(CancellationToken.None);

        provider.TryGetDefinition(1)!.Name.Should().Be("first_chair");
        provider.TryGetDefinitionByName("second_chair")!.Id.Should().Be(2);
    }

    /// <summary>
    /// A reload replaces the whole set, so a definition that is gone from the database is gone from
    /// both indexes at once rather than surviving in one of them.
    /// </summary>
    [Fact]
    public async Task ADefinitionRemovedFromTheDatabase_LeavesBothIndexes()
    {
        await SeedAsync(("first_chair", 1), ("second_chair", 2));

        FurnitureDefinitionProvider provider = Build();
        await provider.ReloadAsync(CancellationToken.None);

        await using (VortexDbContext db = new(_options))
        {
            db.FurnitureDefinitions.RemoveRange(
                await db.FurnitureDefinitions.Where(d => d.Id == 2).ToListAsync()
            );
            await db.SaveChangesAsync();
        }

        await provider.ReloadAsync(CancellationToken.None);

        provider.TryGetDefinition(2).Should().BeNull();
        provider.TryGetDefinitionByName("second_chair").Should().BeNull();
    }

    /// <summary>
    /// Classnames are not unique — the shipped catalogue has thousands of duplicates — so the
    /// name index keeps the first and the reload survives, rather than throwing and leaving the
    /// emulator with no definitions at all.
    /// </summary>
    [Fact]
    public async Task DuplicateClassnames_DoNotFailTheReload()
    {
        await SeedAsync(("roomdimmer", 1), ("roomdimmer", 2));

        FurnitureDefinitionProvider provider = Build();

        Func<Task> reload = () => provider.ReloadAsync(CancellationToken.None);

        await reload.Should().NotThrowAsync();

        provider.TryGetDefinitionByName("roomdimmer")!.Id.Should().Be(1);
        provider.TryGetDefinition(2).Should().NotBeNull("both are still addressable by id");
    }

    private FurnitureDefinitionProvider Build() =>
        new(
            Options.Create(new FurnitureConfig()),
            new TestDbContextFactory(_options),
            FakeProxy.Create<IVortexMetrics>(_ => null),
            NullLogger<Vortex.Primitives.Furniture.Providers.IFurnitureDefinitionProvider>.Instance
        );

    private async Task SeedAsync(params (string Name, int Id)[] definitions)
    {
        await using VortexDbContext db = new(_options);

        foreach ((string name, int id) in definitions)
        {
            db.FurnitureDefinitions.Add(
                new FurnitureDefinitionEntity
                {
                    Id = id,
                    Name = name,
                    SpriteId = id,
                    ProductType = ProductType.Floor,
                    FurniCategory = FurnitureCategory.Default,
                    Logic = "default_floor",
                    TotalStates = 1,
                    Width = 1,
                    Length = 1,
                    StackHeight = 1,
                    CanStack = true,
                    CanWalk = false,
                    CanSit = false,
                    CanLay = false,
                    CanRecycle = false,
                    CanTrade = true,
                    CanGroup = false,
                    CanSell = true,
                    UsagePolicy = FurnitureUsageType.Nobody,
                    StuffDataType = StuffDataType.LegacyKey,
                }
            );
        }

        await db.SaveChangesAsync();
    }

    private sealed class TestDbContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
