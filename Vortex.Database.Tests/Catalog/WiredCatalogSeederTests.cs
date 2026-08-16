using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vortex.Catalog.Seeding;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Database.Entities.Furniture;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Xunit;

namespace Vortex.Database.Tests.Catalog;

/// <summary>
/// A wired box the server implements but no catalogue page sells cannot be built with, and the two
/// are maintained in completely different places — the server in code, the catalogue in imported
/// data. This seeder is what keeps them from drifting apart, and it has to do so without ever
/// touching what an operator already arranged.
/// </summary>
public sealed class WiredCatalogSeederTests
{
    [Fact]
    public async Task ABoxNoPageSells_IsListedOnItsFamilyPage()
    {
        using VortexDbContext db = NewContext(out IDbContextFactory<VortexDbContext> factory);

        Root(db);
        Definition(db, "wf_slc_users_area");
        await db.SaveChangesAsync();

        await SeedAsync(factory);

        CatalogOfferEntity offer = db
            .CatalogOffers.Include(o => o.Products!)
                .ThenInclude(p => p.FurnitureDefinition)
            .Single();

        offer.LocalizationId.Should().Be("wf_slc_users_area");
        offer.Visible.Should().BeTrue();
        offer.CostCredits.Should().Be(3);

        db.CatalogPages.Single(p => p.Id == offer.CatalogPageEntityId)
            .Localization.Should()
            .Be("selectors");
    }

    [Fact]
    public async Task ABoxAlreadySoldDeeperInTheSection_IsNotListedTwice()
    {
        using VortexDbContext db = NewContext(out IDbContextFactory<VortexDbContext> factory);

        CatalogPageEntity root = Root(db);
        CatalogPageEntity family = Page(db, "triggers", root.Id);
        // The imported catalogue nests its families another level down.
        CatalogPageEntity nested = Page(db, "habbo", family.Id);
        FurnitureDefinitionEntity definition = Definition(db, "wf_trg_click_furni");
        await db.SaveChangesAsync();

        Sell(db, definition, nested, visible: true);
        await db.SaveChangesAsync();

        await SeedAsync(factory);

        db.CatalogOffers.Should().HaveCount(1);
    }

    [Fact]
    public async Task ABoxSoldOnlyOutsideTheSection_IsListedInsideIt()
    {
        using VortexDbContext db = NewContext(out IDbContextFactory<VortexDbContext> factory);

        CatalogPageEntity root = Root(db);
        // The "Unused Wired" drawer an imported catalogue leaves whole families in.
        CatalogPageEntity elsewhere = Page(db, "unused_wired", parentId: null);
        FurnitureDefinitionEntity definition = Definition(db, "wf_var_user");
        await db.SaveChangesAsync();

        Sell(db, definition, elsewhere, visible: true);
        await db.SaveChangesAsync();

        await SeedAsync(factory);

        db.CatalogOffers.Should().HaveCount(2);

        // The operator's own listing is left exactly where they put it.
        db.CatalogOffers.Should().Contain(o => o.CatalogPageEntityId == elsewhere.Id);

        int variables = db
            .CatalogPages.Single(p => p.Localization == "variables" && p.ParentEntityId == root.Id)
            .Id;

        db.CatalogOffers.Should().Contain(o => o.CatalogPageEntityId == variables);
    }

    [Fact]
    public async Task ABoxWhoseOnlyOfferIsHidden_IsListedWhereItCanBeBought()
    {
        using VortexDbContext db = NewContext(out IDbContextFactory<VortexDbContext> factory);

        CatalogPageEntity root = Root(db);
        CatalogPageEntity family = Page(db, "selectors", root.Id);
        FurnitureDefinitionEntity definition = Definition(db, "wf_slc_users_team");
        await db.SaveChangesAsync();

        // A hidden offer is not a listing: nobody can buy it.
        Sell(db, definition, family, visible: false);
        await db.SaveChangesAsync();

        await SeedAsync(factory);

        db.CatalogOffers.Should().HaveCount(2);
        db.CatalogOffers.Count(o => o.Visible).Should().Be(1);
    }

    [Fact]
    public async Task DuplicateDefinitionsOfOneClassname_ProduceOneListing()
    {
        using VortexDbContext db = NewContext(out IDbContextFactory<VortexDbContext> factory);

        Root(db);
        Definition(db, "wf_act_freeze");
        Definition(db, "wf_act_freeze");
        await db.SaveChangesAsync();

        await SeedAsync(factory);

        db.CatalogOffers.Should().HaveCount(1);
    }

    [Fact]
    public async Task RunningTwice_AddsNothingTheSecondTime()
    {
        using VortexDbContext db = NewContext(out IDbContextFactory<VortexDbContext> factory);

        Root(db);
        Definition(db, "wf_cnd_has_var");
        await db.SaveChangesAsync();

        await SeedAsync(factory);
        await SeedAsync(factory);

        db.CatalogOffers.Should().HaveCount(1);
        db.CatalogPages.Count(p => p.Localization == "conditions").Should().Be(1);
    }

    [Fact]
    public async Task FurnitureThatIsNotAWiredBox_IsLeftAlone()
    {
        using VortexDbContext db = NewContext(out IDbContextFactory<VortexDbContext> factory);

        Root(db);
        // Decorative wired furni and ordinary furniture are sold as furniture, not as boxes.
        Definition(db, "wf_wire1");
        Definition(db, "chair");
        await db.SaveChangesAsync();

        await SeedAsync(factory);

        db.CatalogOffers.Should().BeEmpty();
    }

    [Fact]
    public async Task WithNoWiredSectionAtAll_NothingIsInvented()
    {
        using VortexDbContext db = NewContext(out IDbContextFactory<VortexDbContext> factory);

        Definition(db, "wf_trg_click_furni");
        await db.SaveChangesAsync();

        await SeedAsync(factory);

        db.CatalogOffers.Should().BeEmpty();
        db.CatalogPages.Should().BeEmpty();
    }

    // ---- harness -------------------------------------------------------------------------------

    private static async Task SeedAsync(IDbContextFactory<VortexDbContext> factory) =>
        await new WiredCatalogSeederService(
            factory,
            NullLogger<WiredCatalogSeederService>.Instance
        ).StartAsync(CancellationToken.None);

    private static VortexDbContext NewContext(out IDbContextFactory<VortexDbContext> factory)
    {
        DbContextOptions<VortexDbContext> options = new DbContextOptionsBuilder<VortexDbContext>()
            .UseInMemoryDatabase($"wired-catalog-{Guid.NewGuid():N}")
            .Options;

        factory = new SharedContextFactory(options);

        return new VortexDbContext(options);
    }

    private static CatalogPageEntity Root(VortexDbContext db) =>
        Page(db, WiredCatalogCategories.RootLocalization, parentId: null);

    private static CatalogPageEntity Page(VortexDbContext db, string localization, int? parentId)
    {
        CatalogPageEntity page = new()
        {
            CatalogType = CatalogType.Normal,
            ParentEntityId = parentId,
            Localization = localization,
            Icon = 0,
            Layout = CatalogPageLayout.Default3x3,
            SortOrder = 0,
            Visible = true,
        };

        db.CatalogPages.Add(page);
        db.SaveChanges();

        return page;
    }

    private static FurnitureDefinitionEntity Definition(VortexDbContext db, string name)
    {
        FurnitureDefinitionEntity definition = new()
        {
            SpriteId = 1,
            Name = name,
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            Logic = name,
            TotalStates = 1,
            Width = 1,
            Length = 1,
            StackHeight = 0,
            CanStack = false,
            CanWalk = false,
            CanSit = false,
            CanLay = false,
            CanRecycle = false,
            CanTrade = true,
            CanGroup = false,
            CanSell = true,
        };

        db.FurnitureDefinitions.Add(definition);

        return definition;
    }

    private static void Sell(
        VortexDbContext db,
        FurnitureDefinitionEntity definition,
        CatalogPageEntity page,
        bool visible
    )
    {
        CatalogOfferEntity offer = new()
        {
            CatalogPageEntityId = page.Id,
            Page = page,
            LocalizationId = definition.Name,
            CostCredits = 3,
            CostCurrency = 0,
            CanGift = true,
            CanBundle = true,
            ClubLevel = 0,
            Visible = visible,
        };

        db.CatalogOffers.Add(offer);
        db.SaveChanges();

        db.CatalogProducts.Add(
            new CatalogProductEntity
            {
                CatalogOfferEntityId = offer.Id,
                Offer = offer,
                ProductType = ProductType.Floor,
                FurnitureDefinitionEntityId = definition.Id,
                Quantity = 1,
                UniqueSize = 0,
                UniqueRemaining = 0,
                BuildersClubEligible = false,
            }
        );
    }

    private sealed class SharedContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
