# Entity index

> **Generated reference index.** This file inventories code symbols from a static scan of the
> repository at commit `e57f0be79a96` and should not be used as the sole source for runtime semantics.
> Regenerate with `/document-vortex update`. Explanatory pages live one directory up.


148 entity types under `Vortex.Database/Entities/**`, grouped by folder. **145 of them are mapped as a `DbSet` on `VortexDbContext`**; the remainder are bases or unmapped types. Table names, keys, indexes and relationships are configured separately &mdash; see [`../07-database/entities-and-relationships.md`](../07-database/entities-and-relationships.md). A table listed here is **not** proof that the DB owns that state at runtime; check [`../07-database/ownership-boundaries.md`](../07-database/ownership-boundaries.md).

| Group | Entities |
|---|---|
| `(root)` | 1 |
| `Achievements` | 5 |
| `Audit` | 3 |
| `Catalog` | 14 |
| `Collectibles` | 11 |
| `Commerce` | 2 |
| `Errors` | 2 |
| `Furniture` | 7 |
| `Groups` | 11 |
| `Help` | 3 |
| `Marketplace` | 2 |
| `Messenger` | 6 |
| `Moderation` | 3 |
| `MysteryBox` | 1 |
| `Navigator` | 4 |
| `Permissions` | 4 |
| `Pets` | 7 |
| `Players` | 27 |
| `Polls` | 5 |
| `Prizes` | 4 |
| `Quests` | 8 |
| `Room` | 11 |
| `Security` | 1 |
| `Server` | 1 |
| `Wired` | 5 |

## (root)

| Entity | File |
|---|---|
| `VortexEntity` | `Vortex.Database/Entities/VortexEntity.cs` |

## Achievements

| Entity | File |
|---|---|
| `AchievementEntity` | `Vortex.Database/Entities/Achievements/AchievementEntity.cs` |
| `AchievementLevelEntity` | `Vortex.Database/Entities/Achievements/AchievementLevelEntity.cs` |
| `AchievementResolutionEntity` | `Vortex.Database/Entities/Achievements/AchievementResolutionEntity.cs` |
| `PlayerAchievementEntity` | `Vortex.Database/Entities/Achievements/PlayerAchievementEntity.cs` |
| `PlayerAchievementResolutionEntity` | `Vortex.Database/Entities/Achievements/PlayerAchievementResolutionEntity.cs` |

## Audit

| Entity | File |
|---|---|
| `AuditEventEntity` | `Vortex.Database/Entities/Audit/AuditEventEntity.cs` |
| `EconomyLedgerEntity` | `Vortex.Database/Entities/Audit/EconomyLedgerEntity.cs` |
| `ItemEventEntity` | `Vortex.Database/Entities/Audit/ItemEventEntity.cs` |

## Catalog

| Entity | File |
|---|---|
| `CatalogClubGiftEntity` | `Vortex.Database/Entities/Catalog/CatalogClubGiftEntity.cs` |
| `CatalogClubOfferEntity` | `Vortex.Database/Entities/Catalog/CatalogClubOfferEntity.cs` |
| `CatalogFrontPageItemEntity` | `Vortex.Database/Entities/Catalog/CatalogFrontPageItemEntity.cs` |
| `CatalogOfferEntity` | `Vortex.Database/Entities/Catalog/CatalogOfferEntity.cs` |
| `CatalogPageEntity` | `Vortex.Database/Entities/Catalog/CatalogPageEntity.cs` |
| `CatalogProductEntity` | `Vortex.Database/Entities/Catalog/CatalogProductEntity.cs` |
| `CurrencyTypeEntity` | `Vortex.Database/Entities/Catalog/CurrencyTypeEntity.cs` |
| `LtdRaffleEntryEntity` | `Vortex.Database/Entities/Catalog/LtdRaffleEntryEntity.cs` |
| `LtdSeriesEntity` | `Vortex.Database/Entities/Catalog/LtdSeriesEntity.cs` |
| `PlayerTargetedOfferEntity` | `Vortex.Database/Entities/Catalog/PlayerTargetedOfferEntity.cs` |
| `TargetedOfferEntity` | `Vortex.Database/Entities/Catalog/TargetedOfferEntity.cs` |
| `TargetedOfferProductEntity` | `Vortex.Database/Entities/Catalog/TargetedOfferProductEntity.cs` |
| `VoucherEntity` | `Vortex.Database/Entities/Catalog/VoucherEntity.cs` |
| `VoucherRedemptionEntity` | `Vortex.Database/Entities/Catalog/VoucherRedemptionEntity.cs` |

## Collectibles

| Entity | File |
|---|---|
| `NftAssetEntity` | `Vortex.Database/Entities/Collectibles/NftAssetEntity.cs` |
| `NftAssetLedgerEntity` | `Vortex.Database/Entities/Collectibles/NftAssetLedgerEntity.cs` |
| `NftAssetLedgerReason` | `Vortex.Database/Entities/Collectibles/NftAssetLedgerEntity.cs` |
| `NftClaimEntity` | `Vortex.Database/Entities/Collectibles/NftClaimEntity.cs` |
| `NftCollectionEntity` | `Vortex.Database/Entities/Collectibles/NftCollectionEntity.cs` |
| `NftCollectionItemEntity` | `Vortex.Database/Entities/Collectibles/NftCollectionItemEntity.cs` |
| `NftMintTokenOfferEntity` | `Vortex.Database/Entities/Collectibles/NftMintTokenOfferEntity.cs` |
| `NftMintableItemTypeEntity` | `Vortex.Database/Entities/Collectibles/NftMintableItemTypeEntity.cs` |
| `NftStoreOfferEntity` | `Vortex.Database/Entities/Collectibles/NftStoreOfferEntity.cs` |
| `PlayerCollectorStatsEntity` | `Vortex.Database/Entities/Collectibles/PlayerCollectorStatsEntity.cs` |
| `PlayerMintTokensEntity` | `Vortex.Database/Entities/Collectibles/PlayerMintTokensEntity.cs` |

## Commerce

| Entity | File |
|---|---|
| `CommerceOperationEntity` | `Vortex.Database/Entities/Commerce/CommerceOperationEntity.cs` |
| `CommerceReceiptEntity` | `Vortex.Database/Entities/Commerce/CommerceReceiptEntity.cs` |

## Errors

| Entity | File |
|---|---|
| `ErrorGroupEntity` | `Vortex.Database/Entities/Errors/ErrorGroupEntity.cs` |
| `ErrorOccurrenceEntity` | `Vortex.Database/Entities/Errors/ErrorOccurrenceEntity.cs` |

## Furniture

| Entity | File |
|---|---|
| `FigureSellableSetEntity` | `Vortex.Database/Entities/Furniture/FigureSellableSetEntity.cs` |
| `FurnitureDefinitionEntity` | `Vortex.Database/Entities/Furniture/FurnitureDefinitionEntity.cs` |
| `FurnitureEntity` | `Vortex.Database/Entities/Furniture/FurnitureEntity.cs` |
| `FurniturePurchasableClothingEntity` | `Vortex.Database/Entities/Furniture/FurniturePurchasableClothingEntity.cs` |
| `FurnitureTeleportLinkEntity` | `Vortex.Database/Entities/Furniture/FurnitureTeleportLinkEntity.cs` |
| `RentableSpaceTermsEntity` | `Vortex.Database/Entities/Furniture/RentableSpaceTermsEntity.cs` |
| `RoomRentableSpaceEntity` | `Vortex.Database/Entities/Furniture/RoomRentableSpaceEntity.cs` |

## Groups

| Entity | File |
|---|---|
| `GroupBadgePartEntity` | `Vortex.Database/Entities/Groups/GroupBadgePartEntity.cs` |
| `GroupBlockedMemberEntity` | `Vortex.Database/Entities/Groups/GroupBlockedMemberEntity.cs` |
| `GroupColorEntity` | `Vortex.Database/Entities/Groups/GroupColorEntity.cs` |
| `GroupEntity` | `Vortex.Database/Entities/Groups/GroupEntity.cs` |
| `GroupFactory` | `Vortex.Database/Entities/Groups/GroupFactory.cs` |
| `GroupForumPostEntity` | `Vortex.Database/Entities/Groups/GroupForumPostEntity.cs` |
| `GroupForumReadMarkerEntity` | `Vortex.Database/Entities/Groups/GroupForumReadMarkerEntity.cs` |
| `GroupForumSettingsEntity` | `Vortex.Database/Entities/Groups/GroupForumSettingsEntity.cs` |
| `GroupForumThreadEntity` | `Vortex.Database/Entities/Groups/GroupForumThreadEntity.cs` |
| `GroupMemberEntity` | `Vortex.Database/Entities/Groups/GroupMemberEntity.cs` |
| `GroupMembershipRequestEntity` | `Vortex.Database/Entities/Groups/GroupMembershipRequestEntity.cs` |

## Help

| Entity | File |
|---|---|
| `PlayerQuizEntity` | `Vortex.Database/Entities/Help/PlayerQuizEntity.cs` |
| `QuizEntity` | `Vortex.Database/Entities/Help/QuizEntity.cs` |
| `QuizQuestionEntity` | `Vortex.Database/Entities/Help/QuizQuestionEntity.cs` |

## Marketplace

| Entity | File |
|---|---|
| `MarketplaceOfferEntity` | `Vortex.Database/Entities/Marketplace/MarketplaceOfferEntity.cs` |
| `MarketplaceSettingsEntity` | `Vortex.Database/Entities/Marketplace/MarketplaceSettingsEntity.cs` |

## Messenger

| Entity | File |
|---|---|
| `MessengerBlockedEntity` | `Vortex.Database/Entities/Messenger/MessengerBlockedEntity.cs` |
| `MessengerCategoryEntity` | `Vortex.Database/Entities/Messenger/MessengerCategoryEntity.cs` |
| `MessengerFriendEntity` | `Vortex.Database/Entities/Messenger/MessengerFriendEntity.cs` |
| `MessengerIgnoredEntity` | `Vortex.Database/Entities/Messenger/MessengerIgnoredEntity.cs` |
| `MessengerMessageEntity` | `Vortex.Database/Entities/Messenger/MessengerMessageEntity.cs` |
| `MessengerRequestEntity` | `Vortex.Database/Entities/Messenger/MessengerRequestEntity.cs` |

## Moderation

| Entity | File |
|---|---|
| `CfhCategoryEntity` | `Vortex.Database/Entities/Moderation/CfhCategoryEntity.cs` |
| `CfhTicketEntity` | `Vortex.Database/Entities/Moderation/CfhTicketEntity.cs` |
| `CfhTopicEntity` | `Vortex.Database/Entities/Moderation/CfhTopicEntity.cs` |

## MysteryBox

| Entity | File |
|---|---|
| `PlayerMysteryBoxKeyEntity` | `Vortex.Database/Entities/MysteryBox/PlayerMysteryBoxKeyEntity.cs` |

## Navigator

| Entity | File |
|---|---|
| `NavigatorEventCategoryEntity` | `Vortex.Database/Entities/Navigator/NavigatorEventCategoryEntity.cs` |
| `NavigatorFlatCategoryEntity` | `Vortex.Database/Entities/Navigator/NavigatorFlatCategoryEntity.cs` |
| `NavigatorQuickLinkEntity` | `Vortex.Database/Entities/Navigator/NavigatorQuickLinkEntity.cs` |
| `NavigatorTopLevelContextEntity` | `Vortex.Database/Entities/Navigator/NavigatorTopLevelContextEntity.cs` |

## Permissions

| Entity | File |
|---|---|
| `PlayerAccountRoleEntity` | `Vortex.Database/Entities/Permissions/PlayerAccountRoleEntity.cs` |
| `RoleEntity` | `Vortex.Database/Entities/Permissions/RoleEntity.cs` |
| `RolePermissionEntity` | `Vortex.Database/Entities/Permissions/RolePermissionEntity.cs` |
| `SanctionPresetEntity` | `Vortex.Database/Entities/Permissions/SanctionPresetEntity.cs` |

## Pets

| Entity | File |
|---|---|
| `PetCommandEntity` | `Vortex.Database/Entities/Pets/PetCommandEntity.cs` |
| `PetCommandNameEntity` | `Vortex.Database/Entities/Pets/PetCommandNameEntity.cs` |
| `PetEntity` | `Vortex.Database/Entities/Pets/PetEntity.cs` |
| `PetFoodEntity` | `Vortex.Database/Entities/Pets/PetFoodEntity.cs` |
| `PetLevelEntity` | `Vortex.Database/Entities/Pets/PetLevelEntity.cs` |
| `PetPaletteEntity` | `Vortex.Database/Entities/Pets/PetPaletteEntity.cs` |
| `PetVocalEntity` | `Vortex.Database/Entities/Pets/PetVocalEntity.cs` |

## Players

| Entity | File |
|---|---|
| `AccountBanEntity` | `Vortex.Database/Entities/Players/AccountBanEntity.cs` |
| `AccountLevelEntity` | `Vortex.Database/Entities/Players/AccountLevelEntity.cs` |
| `BuildersClubTierEntity` | `Vortex.Database/Entities/Players/BuildersClubTierEntity.cs` |
| `NftAvatarEntity` | `Vortex.Database/Entities/Players/NftAvatarEntity.cs` |
| `PlayerAccountEntity` | `Vortex.Database/Entities/Players/PlayerAccountEntity.cs` |
| `PlayerAccountPreferencesEntity` | `Vortex.Database/Entities/Players/PlayerAccountPreferencesEntity.cs` |
| `PlayerBadgeEntity` | `Vortex.Database/Entities/Players/PlayerBadgeEntity.cs` |
| `PlayerChatStyleEntity` | `Vortex.Database/Entities/Players/PlayerChatStyleEntity.cs` |
| `PlayerChatStyleOwnedEntity` | `Vortex.Database/Entities/Players/PlayerChatStyleOwnedEntity.cs` |
| `PlayerClothingEntity` | `Vortex.Database/Entities/Players/PlayerClothingEntity.cs` |
| `PlayerCurrencyEntity` | `Vortex.Database/Entities/Players/PlayerCurrencyEntity.cs` |
| `PlayerEffectEntity` | `Vortex.Database/Entities/Players/PlayerEffectEntity.cs` |
| `PlayerEntity` | `Vortex.Database/Entities/Players/PlayerEntity.cs` |
| `PlayerFavoriteRoomsEntity` | `Vortex.Database/Entities/Players/PlayerFavoriteRoomsEntity.cs` |
| `PlayerKickbackEntity` | `Vortex.Database/Entities/Players/PlayerKickbackEntity.cs` |
| `PlayerModToolPreferencesEntity` | `Vortex.Database/Entities/Players/PlayerModToolPreferencesEntity.cs` |
| `PlayerNavigatorCollapsedCategoryEntity` | `Vortex.Database/Entities/Players/PlayerNavigatorCollapsedCategoryEntity.cs` |
| `PlayerNavigatorPreferencesEntity` | `Vortex.Database/Entities/Players/PlayerNavigatorPreferencesEntity.cs` |
| `PlayerNavigatorSavedSearchEntity` | `Vortex.Database/Entities/Players/PlayerNavigatorSavedSearchEntity.cs` |
| `PlayerNavigatorViewModeEntity` | `Vortex.Database/Entities/Players/PlayerNavigatorViewModeEntity.cs` |
| `PlayerNftAvatarEntity` | `Vortex.Database/Entities/Players/PlayerNftAvatarEntity.cs` |
| `PlayerNftOutfitEntity` | `Vortex.Database/Entities/Players/PlayerNftOutfitEntity.cs` |
| `PlayerSubscriptionEntity` | `Vortex.Database/Entities/Players/PlayerSubscriptionEntity.cs` |
| `PlayerVaultIncomeRewardEntity` | `Vortex.Database/Entities/Players/PlayerVaultIncomeRewardEntity.cs` |
| `PlayerWardrobeOutfitEntity` | `Vortex.Database/Entities/Players/PlayerWardrobeOutfitEntity.cs` |
| `PlayerWiredPreferencesEntity` | `Vortex.Database/Entities/Players/PlayerWiredPreferencesEntity.cs` |
| `PlayerWordFilterEntity` | `Vortex.Database/Entities/Players/PlayerWordFilterEntity.cs` |

## Polls

| Entity | File |
|---|---|
| `PlayerPollAnswerEntity` | `Vortex.Database/Entities/Polls/PlayerPollAnswerEntity.cs` |
| `PlayerPollEntity` | `Vortex.Database/Entities/Polls/PlayerPollEntity.cs` |
| `PollEntity` | `Vortex.Database/Entities/Polls/PollEntity.cs` |
| `PollQuestionChoiceEntity` | `Vortex.Database/Entities/Polls/PollQuestionChoiceEntity.cs` |
| `PollQuestionEntity` | `Vortex.Database/Entities/Polls/PollQuestionEntity.cs` |

## Prizes

| Entity | File |
|---|---|
| `PlayerPrizeClaimEntity` | `Vortex.Database/Entities/Prizes/PlayerPrizeClaimEntity.cs` |
| `PrizePoolBindingEntity` | `Vortex.Database/Entities/Prizes/PrizePoolBindingEntity.cs` |
| `PrizePoolEntity` | `Vortex.Database/Entities/Prizes/PrizePoolEntity.cs` |
| `PrizePoolEntryEntity` | `Vortex.Database/Entities/Prizes/PrizePoolEntryEntity.cs` |

## Quests

| Entity | File |
|---|---|
| `CommunityGoalEntity` | `Vortex.Database/Entities/Quests/CommunityGoalEntity.cs` |
| `CommunityGoalLevelEntity` | `Vortex.Database/Entities/Quests/CommunityGoalLevelEntity.cs` |
| `DailyTaskEntity` | `Vortex.Database/Entities/Quests/DailyTaskEntity.cs` |
| `DailyTaskRewardEntity` | `Vortex.Database/Entities/Quests/DailyTaskRewardEntity.cs` |
| `PlayerCommunityGoalContributionEntity` | `Vortex.Database/Entities/Quests/PlayerCommunityGoalContributionEntity.cs` |
| `PlayerDailyTaskEntity` | `Vortex.Database/Entities/Quests/PlayerDailyTaskEntity.cs` |
| `PlayerQuestEntity` | `Vortex.Database/Entities/Quests/PlayerQuestEntity.cs` |
| `QuestEntity` | `Vortex.Database/Entities/Quests/QuestEntity.cs` |

## Room

| Entity | File |
|---|---|
| `BotEntity` | `Vortex.Database/Entities/Room/BotEntity.cs` |
| `HandItemEntity` | `Vortex.Database/Entities/Room/HandItemEntity.cs` |
| `RoomAdvertisementEntity` | `Vortex.Database/Entities/Room/RoomAdvertisementEntity.cs` |
| `RoomBanEntity` | `Vortex.Database/Entities/Room/RoomBanEntity.cs` |
| `RoomChatlogEntity` | `Vortex.Database/Entities/Room/RoomChatlogEntity.cs` |
| `RoomEntity` | `Vortex.Database/Entities/Room/RoomEntity.cs` |
| `RoomEntryLogEntity` | `Vortex.Database/Entities/Room/RoomEntryLogEntity.cs` |
| `RoomModelEntity` | `Vortex.Database/Entities/Room/RoomModelEntity.cs` |
| `RoomMuteEntity` | `Vortex.Database/Entities/Room/RoomMuteEntity.cs` |
| `RoomRatingEntity` | `Vortex.Database/Entities/Room/RoomRatingEntity.cs` |
| `RoomRightEntity` | `Vortex.Database/Entities/Room/RoomRightEntity.cs` |

## Security

| Entity | File |
|---|---|
| `SecurityTicketEntity` | `Vortex.Database/Entities/Security/SecurityTicketEntity.cs` |

## Server

| Entity | File |
|---|---|
| `ServerConfigEntity` | `Vortex.Database/Entities/Server/ServerConfigEntity.cs` |

## Wired

| Entity | File |
|---|---|
| `RoomWiredLogEntity` | `Vortex.Database/Entities/Wired/RoomWiredLogEntity.cs` |
| `WiredChestEntity` | `Vortex.Database/Entities/Wired/WiredChestEntity.cs` |
| `WiredChestTransactionEntity` | `Vortex.Database/Entities/Wired/WiredChestTransactionEntity.cs` |
| `WiredContractEntity` | `Vortex.Database/Entities/Wired/WiredContractEntity.cs` |
| `WiredPermanentVariableEntity` | `Vortex.Database/Entities/Wired/WiredPermanentVariableEntity.cs` |

## Migrations

133 migration files under `Vortex.Database/Migrations/` (designer files excluded).

- Oldest: `20260205185839_AddedModels.cs`
- Newest: `VortexDbContextModelSnapshot.cs`

See [`../07-database/migrations.md`](../07-database/migrations.md) for the offline authoring recipe.

