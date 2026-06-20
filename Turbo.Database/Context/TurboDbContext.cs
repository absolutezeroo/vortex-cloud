using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Audit;
using Turbo.Database.Entities.Catalog;
using Turbo.Database.Entities.Errors;
using Turbo.Database.Entities.Furniture;
using Turbo.Database.Entities.Groups;
using Turbo.Database.Entities.Marketplace;
using Turbo.Database.Entities.Messenger;
using Turbo.Database.Entities.Navigator;
using Turbo.Database.Entities.Permissions;
using Turbo.Database.Entities.Players;
using Turbo.Database.Entities.Room;
using Turbo.Database.Entities.Security;
using Turbo.Database.Entities.Tracking;
using Turbo.Primitives.Catalog;

namespace Turbo.Database.Context;

public class TurboDbContext(DbContextOptions<TurboDbContext> options)
    : DbContextBase<TurboDbContext>(options)
{
    public DbSet<AuditEventEntity> AuditEvents { get; init; }

    public DbSet<EconomyLedgerEntity> EconomyLedger { get; init; }

    public DbSet<ItemEventEntity> ItemEvents { get; init; }

    public DbSet<CatalogClubOfferEntity> CatalogClubOffers { get; init; }

    public DbSet<CatalogClubGiftEntity> CatalogClubGifts { get; init; }

    public DbSet<CatalogFrontPageItemEntity> CatalogFrontPageItems { get; init; }

    public DbSet<CatalogOfferEntity> CatalogOffers { get; init; }

    public DbSet<CurrencyTypeEntity> CurrencyTypes { get; init; }

    public DbSet<CatalogPageEntity> CatalogPages { get; init; }

    public DbSet<CatalogProductEntity> CatalogProducts { get; init; }
    public DbSet<FurnitureDefinitionEntity> FurnitureDefinitions { get; init; }

    public DbSet<FurnitureEntity> Furnitures { get; init; }

    public DbSet<FurnitureTeleportLinkEntity> FurnitureTeleportLinks { get; init; }

    public DbSet<PlayerBadgeEntity> PlayerBadges { get; init; }

    public DbSet<PlayerCurrencyEntity> PlayerCurrencies { get; init; }
    public DbSet<PlayerEntity> Players { get; init; }

    public DbSet<RoomBanEntity> RoomBans { get; init; }

    public DbSet<RoomEntity> Rooms { get; init; }

    public DbSet<RoomModelEntity> RoomModels { get; init; }

    public DbSet<RoomMuteEntity> RoomMutes { get; init; }

    public DbSet<RoomRightEntity> RoomRights { get; init; }

    public DbSet<RoomEntryLogEntity> RoomEntryLogs { get; init; }

    public DbSet<RoomChatlogEntity> Chatlogs { get; init; }
    public DbSet<SecurityTicketEntity> SecurityTickets { get; init; }

    public DbSet<PlayerAccountEntity> PlayerAccounts { get; init; }

    public DbSet<RoleEntity> Roles { get; init; }

    public DbSet<RolePermissionEntity> RolePermissions { get; init; }

    public DbSet<PlayerAccountRoleEntity> PlayerAccountRoles { get; init; }

    public DbSet<NavigatorTopLevelContextEntity> NavigatorTopLevelContexts { get; init; }

    public DbSet<NavigatorFlatCategoryEntity> NavigatorFlatCategories { get; init; }

    public DbSet<NavigatorEventCategoryEntity> NavigatorEventCategories { get; init; }

    public DbSet<NavigatorQuickLinkEntity> NavigatorQuickLinks { get; init; }

    public DbSet<PlayerChatStyleEntity> PlayerChatStyles { get; init; }
    public DbSet<PlayerChatStyleOwnedEntity> PlayerOwnedChatStyles { get; init; }

    public DbSet<PerformanceLogEntity> PerformanceLogs { get; init; }

    public DbSet<PlayerFavoriteRoomsEntity> PlayerFavouriteRooms { get; init; }

    public DbSet<PlayerNavigatorPreferencesEntity> PlayerNavigatorPreferences { get; init; }

    public DbSet<PlayerNavigatorSavedSearchEntity> PlayerNavigatorSavedSearches { get; init; }

    public DbSet<PlayerNavigatorCollapsedCategoryEntity> PlayerNavigatorCollapsedCategories { get; init; }

    public DbSet<PlayerNavigatorViewModeEntity> PlayerNavigatorViewModes { get; init; }

    public DbSet<PlayerSubscriptionEntity> PlayerSubscriptions { get; init; }

    public DbSet<PlayerKickbackEntity> PlayerKickbacks { get; init; }

    public DbSet<ErrorGroupEntity> ErrorGroups { get; init; }

    public DbSet<ErrorOccurrenceEntity> ErrorOccurrences { get; init; }

    public DbSet<PlayerVaultIncomeRewardEntity> PlayerVaultIncomeRewards { get; init; }

    public DbSet<MarketplaceOfferEntity> MarketplaceOffers { get; init; }

    public DbSet<MessengerCategoryEntity> MessengerCategories { get; init; }

    public DbSet<MessengerFriendEntity> MessengerFriends { get; init; }

    public DbSet<MessengerRequestEntity> MessengerRequests { get; init; }

    public DbSet<MessengerBlockedEntity> MessengerBlocked { get; init; }

    public DbSet<MessengerIgnoredEntity> MessengerIgnored { get; init; }

    public DbSet<MessengerMessageEntity> MessengerMessages { get; init; }

    public DbSet<LtdSeriesEntity> LtdSeries { get; init; }

    public DbSet<LtdRaffleEntryEntity> LtdRaffleEntries { get; init; }

    public DbSet<GroupEntity> Groups { get; init; }

    public DbSet<GroupMemberEntity> GroupMembers { get; init; }

    public DbSet<GroupMembershipRequestEntity> GroupMembershipRequests { get; init; }

    public DbSet<GroupForumSettingsEntity> GroupForumSettings { get; init; }

    public DbSet<GroupForumThreadEntity> GroupForumThreads { get; init; }

    public DbSet<GroupForumPostEntity> GroupForumPosts { get; init; }

    public DbSet<RentableSpaceTermsEntity> RentableSpaceTerms { get; init; }

    public DbSet<RoomRentableSpaceEntity> RoomRentableSpaces { get; init; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<CatalogPageEntity>()
            .Property(e => e.Layout)
            .HasColumnType("varchar(50)")
            .HasConversion(
                v => v.ToLayoutString(),
                v => CatalogPageLayoutExtensions.FromLayoutString(v)
            );

        // Circular link groups.room_id <-> rooms.group_id (DATA-MODEL §2.7). Both FKs are
        // configured non-cascade so MySQL never builds a cascade cycle and so deleting one side
        // never silently deletes the other — dissolving a group detaches rooms.group_id then
        // soft-deletes. Modelled as two independent one-directional relationships (each nav owns
        // its own FK; they are NOT inverses of one another).
        mb.Entity<GroupEntity>()
            .HasOne(g => g.RoomEntity)
            .WithMany()
            .HasForeignKey(g => g.RoomEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<RoomEntity>()
            .HasOne(r => r.GroupEntity)
            .WithMany()
            .HasForeignKey(r => r.GroupEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing FK on furniture.rentable_space_furniture_id (DATA-MODEL §3.3).
        // Non-cascade: deleting a space furni must not cascade-delete the items placed in it.
        mb.Entity<FurnitureEntity>()
            .HasOne(f => f.RentableSpaceFurnitureEntity)
            .WithMany()
            .HasForeignKey(f => f.RentableSpaceFurnitureEntityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
