using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Audit;
using Turbo.Database.Entities.Catalog;
using Turbo.Database.Entities.Errors;
using Turbo.Database.Entities.Furniture;
using Turbo.Database.Entities.Groups;
using Turbo.Database.Entities.Marketplace;
using Turbo.Database.Entities.Messenger;
using Turbo.Database.Entities.Moderation;
using Turbo.Database.Entities.Navigator;
using Turbo.Database.Entities.Permissions;
using Turbo.Database.Entities.Pets;
using Turbo.Database.Entities.Players;
using Turbo.Database.Entities.Room;
using Turbo.Database.Entities.Security;
using Turbo.Database.Entities.Wired;
using Turbo.Primitives.Catalog;

namespace Turbo.Database.Context;

public class TurboDbContext(DbContextOptions<TurboDbContext> options)
    : DbContextBase<TurboDbContext>(options)
{
    public DbSet<AuditEventEntity> AuditEvents { get; init; } = null!;

    public DbSet<EconomyLedgerEntity> EconomyLedger { get; init; } = null!;

    public DbSet<ItemEventEntity> ItemEvents { get; init; } = null!;

    public DbSet<CatalogClubOfferEntity> CatalogClubOffers { get; init; } = null!;

    public DbSet<CatalogClubGiftEntity> CatalogClubGifts { get; init; } = null!;

    public DbSet<CatalogFrontPageItemEntity> CatalogFrontPageItems { get; init; } = null!;

    public DbSet<CatalogOfferEntity> CatalogOffers { get; init; } = null!;

    public DbSet<CurrencyTypeEntity> CurrencyTypes { get; init; } = null!;

    public DbSet<CatalogPageEntity> CatalogPages { get; init; } = null!;

    public DbSet<CatalogProductEntity> CatalogProducts { get; init; } = null!;

    public DbSet<VoucherEntity> Vouchers { get; init; } = null!;

    public DbSet<VoucherRedemptionEntity> VoucherRedemptions { get; init; } = null!;
    public DbSet<FurnitureDefinitionEntity> FurnitureDefinitions { get; init; } = null!;

    public DbSet<FurnitureEntity> Furnitures { get; init; } = null!;

    public DbSet<FurnitureTeleportLinkEntity> FurnitureTeleportLinks { get; init; } = null!;

    public DbSet<PlayerBadgeEntity> PlayerBadges { get; init; } = null!;

    public DbSet<PlayerCurrencyEntity> PlayerCurrencies { get; init; } = null!;
    public DbSet<PlayerEntity> Players { get; init; } = null!;

    public DbSet<RoomBanEntity> RoomBans { get; init; } = null!;

    public DbSet<RoomEntity> Rooms { get; init; } = null!;

    public DbSet<RoomModelEntity> RoomModels { get; init; } = null!;

    public DbSet<RoomMuteEntity> RoomMutes { get; init; } = null!;

    public DbSet<RoomRightEntity> RoomRights { get; init; } = null!;

    public DbSet<RoomEntryLogEntity> RoomEntryLogs { get; init; } = null!;

    public DbSet<RoomAdvertisementEntity> RoomAdvertisements { get; init; } = null!;

    public DbSet<RoomRatingEntity> RoomRatings { get; init; } = null!;

    public DbSet<RoomChatlogEntity> Chatlogs { get; init; } = null!;
    public DbSet<SecurityTicketEntity> SecurityTickets { get; init; } = null!;

    public DbSet<PlayerAccountEntity> PlayerAccounts { get; init; } = null!;

    public DbSet<AccountBanEntity> AccountBans { get; init; } = null!;

    public DbSet<RoleEntity> Roles { get; init; } = null!;

    public DbSet<RolePermissionEntity> RolePermissions { get; init; } = null!;

    public DbSet<PlayerAccountRoleEntity> PlayerAccountRoles { get; init; } = null!;

    public DbSet<SanctionPresetEntity> SanctionPresets { get; init; } = null!;

    public DbSet<CfhCategoryEntity> CfhCategories { get; init; } = null!;

    public DbSet<CfhTopicEntity> CfhTopics { get; init; } = null!;

    public DbSet<CfhTicketEntity> CfhTickets { get; init; } = null!;

    public DbSet<NavigatorTopLevelContextEntity> NavigatorTopLevelContexts { get; init; } = null!;

    public DbSet<NavigatorFlatCategoryEntity> NavigatorFlatCategories { get; init; } = null!;

    public DbSet<NavigatorEventCategoryEntity> NavigatorEventCategories { get; init; } = null!;

    public DbSet<NavigatorQuickLinkEntity> NavigatorQuickLinks { get; init; } = null!;

    public DbSet<PlayerChatStyleEntity> PlayerChatStyles { get; init; } = null!;
    public DbSet<PlayerChatStyleOwnedEntity> PlayerOwnedChatStyles { get; init; } = null!;

    public DbSet<PlayerFavoriteRoomsEntity> PlayerFavouriteRooms { get; init; } = null!;

    public DbSet<PlayerNavigatorPreferencesEntity> PlayerNavigatorPreferences { get; init; } =
        null!;

    public DbSet<PlayerNavigatorSavedSearchEntity> PlayerNavigatorSavedSearches { get; init; } =
        null!;

    public DbSet<PlayerNavigatorCollapsedCategoryEntity> PlayerNavigatorCollapsedCategories { get; init; } =
        null!;

    public DbSet<PlayerNavigatorViewModeEntity> PlayerNavigatorViewModes { get; init; } = null!;

    public DbSet<PlayerSubscriptionEntity> PlayerSubscriptions { get; init; } = null!;

    public DbSet<BuildersClubTierEntity> BuildersClubTiers { get; init; } = null!;

    public DbSet<PlayerKickbackEntity> PlayerKickbacks { get; init; } = null!;

    public DbSet<ErrorGroupEntity> ErrorGroups { get; init; } = null!;

    public DbSet<ErrorOccurrenceEntity> ErrorOccurrences { get; init; } = null!;

    public DbSet<PlayerVaultIncomeRewardEntity> PlayerVaultIncomeRewards { get; init; } = null!;

    public DbSet<MarketplaceOfferEntity> MarketplaceOffers { get; init; } = null!;

    public DbSet<MessengerCategoryEntity> MessengerCategories { get; init; } = null!;

    public DbSet<MessengerFriendEntity> MessengerFriends { get; init; } = null!;

    public DbSet<MessengerRequestEntity> MessengerRequests { get; init; } = null!;

    public DbSet<MessengerBlockedEntity> MessengerBlocked { get; init; } = null!;

    public DbSet<MessengerIgnoredEntity> MessengerIgnored { get; init; } = null!;

    public DbSet<MessengerMessageEntity> MessengerMessages { get; init; } = null!;

    public DbSet<LtdSeriesEntity> LtdSeries { get; init; } = null!;

    public DbSet<LtdRaffleEntryEntity> LtdRaffleEntries { get; init; } = null!;

    public DbSet<GroupEntity> Groups { get; init; } = null!;

    public DbSet<GroupBadgePartEntity> GroupBadgeParts { get; init; } = null!;

    public DbSet<GroupColorEntity> GroupColors { get; init; } = null!;

    public DbSet<GroupMemberEntity> GroupMembers { get; init; } = null!;

    public DbSet<GroupMembershipRequestEntity> GroupMembershipRequests { get; init; } = null!;

    public DbSet<GroupForumSettingsEntity> GroupForumSettings { get; init; } = null!;

    public DbSet<GroupForumThreadEntity> GroupForumThreads { get; init; } = null!;

    public DbSet<GroupForumPostEntity> GroupForumPosts { get; init; } = null!;

    public DbSet<RentableSpaceTermsEntity> RentableSpaceTerms { get; init; } = null!;

    public DbSet<RoomRentableSpaceEntity> RoomRentableSpaces { get; init; } = null!;

    public DbSet<PetEntity> Pets { get; init; } = null!;

    public DbSet<PetCommandEntity> PetCommands { get; init; } = null!;

    public DbSet<PetLevelEntity> PetLevels { get; init; } = null!;

    public DbSet<PetFoodEntity> PetFood { get; init; } = null!;

    public DbSet<PetPaletteEntity> PetPalettes { get; init; } = null!;

    public DbSet<WiredPermanentVariableEntity> WiredPermanentVariables { get; init; } = null!;

    public DbSet<RoomWiredLogEntity> RoomWiredLogs { get; init; } = null!;

    public DbSet<PlayerWiredPreferencesEntity> PlayerWiredPreferences { get; init; } = null!;

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

        // cfh_tickets has three FKs to players (reporter, reported, picker) — non-cascade on all of
        // them so MySQL never has to pick a cascade path, and so a ticket record (audit-adjacent)
        // outlives whichever side it references.
        mb.Entity<CfhTicketEntity>()
            .HasOne(t => t.ReporterPlayerEntity)
            .WithMany()
            .HasForeignKey(t => t.ReporterPlayerEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<CfhTicketEntity>()
            .HasOne(t => t.ReportedPlayerEntity)
            .WithMany()
            .HasForeignKey(t => t.ReportedPlayerEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<CfhTicketEntity>()
            .HasOne(t => t.PickerPlayerEntity)
            .WithMany()
            .HasForeignKey(t => t.PickerPlayerEntityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
