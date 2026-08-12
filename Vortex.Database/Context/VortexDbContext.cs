using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Achievements;
using Vortex.Database.Entities.Audit;
using Vortex.Database.Entities.Catalog;
using Vortex.Database.Entities.Collectibles;
using Vortex.Database.Entities.Errors;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Groups;
using Vortex.Database.Entities.Marketplace;
using Vortex.Database.Entities.Messenger;
using Vortex.Database.Entities.Moderation;
using Vortex.Database.Entities.MysteryBox;
using Vortex.Database.Entities.Navigator;
using Vortex.Database.Entities.Permissions;
using Vortex.Database.Entities.Pets;
using Vortex.Database.Entities.Players;
using Vortex.Database.Entities.Polls;
using Vortex.Database.Entities.Prizes;
using Vortex.Database.Entities.Quests;
using Vortex.Database.Entities.Room;
using Vortex.Database.Entities.Security;
using Vortex.Database.Entities.Server;
using Vortex.Database.Entities.Wired;
using Vortex.Primitives.Catalog;

namespace Vortex.Database.Context;

public class VortexDbContext(DbContextOptions<VortexDbContext> options)
    : DbContextBase<VortexDbContext>(options)
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

    public DbSet<TargetedOfferEntity> TargetedOffers { get; init; } = null!;

    public DbSet<TargetedOfferProductEntity> TargetedOfferProducts { get; init; } = null!;

    public DbSet<PlayerTargetedOfferEntity> PlayerTargetedOffers { get; init; } = null!;

    public DbSet<VoucherEntity> Vouchers { get; init; } = null!;

    public DbSet<VoucherRedemptionEntity> VoucherRedemptions { get; init; } = null!;

    public DbSet<FurnitureDefinitionEntity> FurnitureDefinitions { get; init; } = null!;

    public DbSet<FurnitureEntity> Furnitures { get; init; } = null!;

    public DbSet<FurnitureTeleportLinkEntity> FurnitureTeleportLinks { get; init; } = null!;

    public DbSet<PlayerBadgeEntity> PlayerBadges { get; init; } = null!;

    public DbSet<PlayerEffectEntity> PlayerEffects { get; init; } = null!;

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

    public DbSet<MarketplaceSettingsEntity> MarketplaceSettings { get; init; } = null!;

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

    public DbSet<GroupBlockedMemberEntity> GroupBlockedMembers { get; init; } = null!;

    public DbSet<GroupForumReadMarkerEntity> GroupForumReadMarkers { get; init; } = null!;

    public DbSet<GroupForumSettingsEntity> GroupForumSettings { get; init; } = null!;

    public DbSet<GroupForumThreadEntity> GroupForumThreads { get; init; } = null!;

    public DbSet<GroupForumPostEntity> GroupForumPosts { get; init; } = null!;

    public DbSet<RentableSpaceTermsEntity> RentableSpaceTerms { get; init; } = null!;

    public DbSet<RoomRentableSpaceEntity> RoomRentableSpaces { get; init; } = null!;

    public DbSet<PetEntity> Pets { get; init; } = null!;

    public DbSet<BotEntity> Bots { get; init; } = null!;

    public DbSet<PetCommandEntity> PetCommands { get; init; } = null!;

    public DbSet<PetCommandNameEntity> PetCommandNames { get; init; } = null!;

    public DbSet<PetLevelEntity> PetLevels { get; init; } = null!;

    public DbSet<PetFoodEntity> PetFood { get; init; } = null!;

    public DbSet<HandItemEntity> HandItems { get; init; } = null!;

    public DbSet<NftCollectionEntity> NftCollections { get; init; } = null!;

    public DbSet<NftCollectionItemEntity> NftCollectionItems { get; init; } = null!;

    public DbSet<PlayerCollectorStatsEntity> PlayerCollectorStats { get; init; } = null!;

    public DbSet<PetPaletteEntity> PetPalettes { get; init; } = null!;

    public DbSet<PetVocalEntity> PetVocals { get; init; } = null!;

    public DbSet<WiredPermanentVariableEntity> WiredPermanentVariables { get; init; } = null!;

    public DbSet<RoomWiredLogEntity> RoomWiredLogs { get; init; } = null!;

    public DbSet<PlayerWiredPreferencesEntity> PlayerWiredPreferences { get; init; } = null!;

    public DbSet<PlayerAccountPreferencesEntity> PlayerAccountPreferences { get; init; } = null!;

    public DbSet<PlayerModToolPreferencesEntity> PlayerModToolPreferences { get; init; } = null!;

    public DbSet<PlayerWordFilterEntity> PlayerWordFilters { get; init; } = null!;

    public DbSet<PlayerWardrobeOutfitEntity> PlayerWardrobeOutfits { get; init; } = null!;

    public DbSet<ServerConfigEntity> ServerConfig { get; init; } = null!;

    public DbSet<AchievementEntity> Achievements { get; init; } = null!;

    public DbSet<AchievementLevelEntity> AchievementLevels { get; init; } = null!;

    public DbSet<PlayerAchievementEntity> PlayerAchievements { get; init; } = null!;

    public DbSet<QuestEntity> Quests { get; init; } = null!;

    public DbSet<PlayerQuestEntity> PlayerQuests { get; init; } = null!;

    public DbSet<AccountLevelEntity> AccountLevels { get; init; } = null!;

    public DbSet<DailyTaskEntity> DailyTasks { get; init; } = null!;

    public DbSet<DailyTaskRewardEntity> DailyTaskRewards { get; init; } = null!;

    public DbSet<PlayerDailyTaskEntity> PlayerDailyTasks { get; init; } = null!;

    public DbSet<CommunityGoalEntity> CommunityGoals { get; init; } = null!;

    public DbSet<CommunityGoalLevelEntity> CommunityGoalLevels { get; init; } = null!;

    public DbSet<PlayerCommunityGoalContributionEntity> PlayerCommunityGoalContributions { get; init; } =
        null!;

    public DbSet<PollEntity> Polls { get; init; } = null!;

    public DbSet<PollQuestionEntity> PollQuestions { get; init; } = null!;

    public DbSet<PollQuestionChoiceEntity> PollQuestionChoices { get; init; } = null!;

    public DbSet<PlayerPollEntity> PlayerPolls { get; init; } = null!;

    public DbSet<PlayerPollAnswerEntity> PlayerPollAnswers { get; init; } = null!;

    public DbSet<PrizePoolEntity> PrizePools { get; init; } = null!;

    public DbSet<PrizePoolEntryEntity> PrizePoolEntries { get; init; } = null!;

    public DbSet<PrizePoolBindingEntity> PrizePoolBindings { get; init; } = null!;

    public DbSet<PlayerPrizeClaimEntity> PlayerPrizeClaims { get; init; } = null!;

    public DbSet<PlayerMysteryBoxKeyEntity> PlayerMysteryBoxKeys { get; init; } = null!;

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

        // The pair of reference navigations above makes EF's relationship convention mark
        // groups.room_id unique, which the [Index] attribute alone cannot undo. It must not be
        // unique: a dissolved guild keeps its room_id (soft delete) and MySQL has no partial
        // indexes, so the constraint would permanently burn that room as a future guild base.
        // "At most one live guild per room" is enforced on rooms.group_id instead — nullable, so
        // repeated NULLs are allowed and dissolving a guild frees the room.
        mb.Entity<GroupEntity>().HasIndex(g => g.RoomEntityId).IsUnique(false);

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

        // Deleting an achievement definition cascades to its levels, but a player's progress row
        // references the definition non-cascade so definition edits never wipe player progress
        // silently; player progress is instead cleaned up when the player is deleted.
        mb.Entity<AchievementLevelEntity>()
            .HasOne(l => l.AchievementEntity)
            .WithMany(a => a.Levels)
            .HasForeignKey(l => l.AchievementEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<PlayerAchievementEntity>()
            .HasOne(p => p.PlayerEntity)
            .WithMany()
            .HasForeignKey(p => p.PlayerEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<PlayerAchievementEntity>()
            .HasOne(p => p.AchievementEntity)
            .WithMany()
            .HasForeignKey(p => p.AchievementEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Player quest progress: cascade with the player, non-cascade with the definition so a
        // quest definition edit never wipes progress silently.
        mb.Entity<PlayerQuestEntity>()
            .HasOne(p => p.PlayerEntity)
            .WithMany()
            .HasForeignKey(p => p.PlayerEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<PlayerQuestEntity>()
            .HasOne(p => p.QuestEntity)
            .WithMany()
            .HasForeignKey(p => p.QuestEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        // A daily task owns its rewards. Assignments cascade with the player and are Restrict against
        // the definition, so editing the task catalogue never wipes what people already worked on.
        mb.Entity<DailyTaskRewardEntity>()
            .HasOne(r => r.DailyTaskEntity)
            .WithMany()
            .HasForeignKey(r => r.DailyTaskEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<PlayerDailyTaskEntity>()
            .HasOne(a => a.PlayerEntity)
            .WithMany()
            .HasForeignKey(a => a.PlayerEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<PlayerDailyTaskEntity>()
            .HasOne(a => a.DailyTaskEntity)
            .WithMany()
            .HasForeignKey(a => a.DailyTaskEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        // A goal owns its levels; contributions cascade with the player and are Restrict against the
        // goal so retiring a goal never silently destroys who contributed what.
        mb.Entity<CommunityGoalLevelEntity>()
            .HasOne(l => l.CommunityGoalEntity)
            .WithMany()
            .HasForeignKey(l => l.CommunityGoalEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<PlayerCommunityGoalContributionEntity>()
            .HasOne(c => c.PlayerEntity)
            .WithMany()
            .HasForeignKey(c => c.PlayerEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<PlayerCommunityGoalContributionEntity>()
            .HasOne(c => c.CommunityGoalEntity)
            .WithMany()
            .HasForeignKey(c => c.CommunityGoalEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        // A poll owns its questions and a question owns its choices, so editing a survey down to
        // nothing leaves no orphan rows. The NPS follow-up link is self-referencing and must stay
        // Restrict: MySQL rejects a cascading self-FK, and deleting a root question that still has
        // children is a mistake worth surfacing.
        mb.Entity<PollQuestionEntity>()
            .HasOne(q => q.PollEntity)
            .WithMany()
            .HasForeignKey(q => q.PollEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<PollQuestionEntity>()
            .HasOne(q => q.ParentQuestionEntity)
            .WithMany()
            .HasForeignKey(q => q.ParentQuestionEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<PollQuestionChoiceEntity>()
            .HasOne(c => c.QuestionEntity)
            .WithMany()
            .HasForeignKey(c => c.QuestionEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Participation and answers cascade with the player (account deletion takes them), and are
        // Restrict against the poll so retiring a survey never silently destroys its results.
        mb.Entity<PlayerPollEntity>()
            .HasOne(p => p.PlayerEntity)
            .WithMany()
            .HasForeignKey(p => p.PlayerEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<PlayerPollEntity>()
            .HasOne(p => p.PollEntity)
            .WithMany()
            .HasForeignKey(p => p.PollEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<PlayerPollAnswerEntity>()
            .HasOne(a => a.PlayerEntity)
            .WithMany()
            .HasForeignKey(a => a.PlayerEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<PlayerPollAnswerEntity>()
            .HasOne(a => a.PollEntity)
            .WithMany()
            .HasForeignKey(a => a.PollEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<PlayerPollAnswerEntity>()
            .HasOne(a => a.QuestionEntity)
            .WithMany()
            .HasForeignKey(a => a.QuestionEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Targeted offers: deleting an offer cascades to its bundle products; a player's per-offer
        // state references the offer non-cascade and is cleaned up with the player.
        mb.Entity<TargetedOfferProductEntity>()
            .HasOne(p => p.TargetedOfferEntity)
            .WithMany(o => o.Products)
            .HasForeignKey(p => p.TargetedOfferEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<TargetedOfferProductEntity>()
            .HasOne(p => p.FurnitureDefinition)
            .WithMany()
            .HasForeignKey(p => p.FurnitureDefinitionEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        mb.Entity<PlayerTargetedOfferEntity>()
            .HasOne(p => p.PlayerEntity)
            .WithMany()
            .HasForeignKey(p => p.PlayerEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<PlayerTargetedOfferEntity>()
            .HasOne(p => p.TargetedOfferEntity)
            .WithMany()
            .HasForeignKey(p => p.TargetedOfferEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Prize entries carry a plain definition id (no FK) because effect and club prizes
        // legitimately leave it at 0. They do cascade with their pool: an entry outliving the pool
        // it is weighted against can never be drawn again.
        mb.Entity<PrizePoolEntryEntity>()
            .HasOne(e => e.PrizePoolEntity)
            .WithMany()
            .HasForeignKey(e => e.PrizePoolEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        // A binding to a deleted pool would leave the furniture drawing from nothing, so it goes
        // with it. The definition side carries no FK: a hotel may bind an id its furnidata has not
        // shipped yet, which the manager grain simply never matches.
        mb.Entity<PrizePoolBindingEntity>()
            .HasOne(b => b.PrizePoolEntity)
            .WithMany()
            .HasForeignKey(b => b.PrizePoolEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Claims cascade both ways: a deleted player takes their claims with them, and retiring a
        // pool clears the "already taken" marks so re-running the same event is possible.
        mb.Entity<PlayerPrizeClaimEntity>()
            .HasOne(c => c.PlayerEntity)
            .WithMany()
            .HasForeignKey(c => c.PlayerEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<PlayerPrizeClaimEntity>()
            .HasOne(c => c.PrizePoolEntity)
            .WithMany()
            .HasForeignKey(c => c.PrizePoolEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Keys cascade with their player.
        mb.Entity<PlayerMysteryBoxKeyEntity>()
            .HasOne(k => k.PlayerEntity)
            .WithMany()
            .HasForeignKey(k => k.PlayerEntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
