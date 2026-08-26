# Grain index

> **Generated reference index.** This file inventories code symbols from a static scan of the
> repository at commit `e57f0be79a96` and should not be used as the sole source for runtime semantics.
> Regenerate with `/document-vortex update`. Explanatory pages live one directory up.


Grain interfaces found by static scan, including partial-file parts. `Key` is read from the `IGrainWith*Key` the interface extends. `Persistence` distinguishes an Orleans `[PersistentState]` store from a grain that merely *names* a `DbContext` type &mdash; the latter is a lexical hit, not proof of what the grain persists. Responsibilities and **state ownership** are documented in [`../03-orleans/grain-map.md`](../03-orleans/grain-map.md); a grain named after an entity does not necessarily own it.

| Interface | Declared in | Implementation | Key | Persistence |
|---|---|---|---|---|
| `IAchievementManagerGrain` | `Vortex.Primitives/Players/Grains/IAchievementManagerGrain.cs` | `Vortex.Progression/Grains/AchievementManagerGrain.cs` | string | names a `DbContext` |
| `ICatalogPurchaseGrain` | `Vortex.Primitives/Catalog/Grains/ICatalogPurchaseGrain.RoomAd.cs` | `Vortex.Catalog/Grains/CatalogPurchaseGrain.cs` | long | &mdash; |
| `ICommunityGoalGrain` | `Vortex.Primitives/Quests/Grains/ICommunityGoalGrain.cs` | `Vortex.Progression/Grains/CommunityGoalGrain.cs` | string | names a `DbContext` |
| `IGroupDirectoryGrain` | `Vortex.Primitives/Groups/Grains/IGroupDirectoryGrain.cs` | `Vortex.Social/Grains/GroupDirectoryGrain.cs` | string | names a `DbContext` |
| `IGroupForumGrain` | `Vortex.Primitives/Groups/Grains/IGroupForumGrain.cs` | `Vortex.Social/Grains/GroupForumGrain.cs` | long | names a `DbContext` |
| `IGroupGrain` | `Vortex.Primitives/Groups/Grains/IGroupGrain.cs` | `Vortex.Social/Grains/GroupGrain.cs` | long | names a `DbContext` |
| `IGuideDirectoryGrain` | `Vortex.Primitives/Help/Grains/IGuideDirectoryGrain.cs` | `Vortex.Social/Grains/GuideDirectoryGrain.cs` | string | &mdash; |
| `IInventoryGrain` | `Vortex.Primitives/Inventory/Grains/IInventoryGrain.Trading.cs` | `Vortex.Inventory/Grains/InventoryGrain.cs` | long | names a `DbContext` |
| `ILtdRaffleGrain` | `Vortex.Primitives/Catalog/Grains/ILtdRaffleGrain.cs` | `Vortex.Catalog/Grains/LtdRaffleGrain.cs` | long | names a `DbContext` |
| `IMarketplacePurchaseGrain` | `Vortex.Primitives/Marketplace/Grains/IMarketplacePurchaseGrain.cs` | `Vortex.Marketplace/Grains/MarketplacePurchaseGrain.cs` | long | names a `DbContext` |
| `IMarketplaceSearchGrain` | `Vortex.Primitives/Marketplace/Grains/IMarketplaceSearchGrain.cs` | `Vortex.Marketplace/Grains/MarketplaceSearchGrain.cs` | string | names a `DbContext` |
| `IMessengerGrain` | `Vortex.Primitives/FriendList/Grains/IMessengerGrain.cs` | `Vortex.Social/Grains/MessengerGrain.cs` | long | names a `DbContext` |
| `IModerationQueueGrain` | `Vortex.Primitives/Moderation/Grains/IModerationQueueGrain.cs` | `Vortex.Rooms/Grains/ModerationQueueGrain.cs` | string | &mdash; |
| `IMysteryBoxManagerGrain` | `Vortex.Primitives/MysteryBox/Grains/IMysteryBoxManagerGrain.cs` | `Vortex.Players/Grains/MysteryBoxManagerGrain.cs` | string | names a `DbContext` |
| `INftCollectionsGrain` | `Vortex.Primitives/Collectibles/Grains/INftCollectionsGrain.cs` | `Vortex.Collectibles/Grains/NftCollectionsGrain.cs` | string | names a `DbContext` |
| `INftMintingGrain` | `Vortex.Primitives/Collectibles/Grains/INftMintingGrain.cs` | `Vortex.Collectibles/Grains/NftMintingGrain.cs` | string | names a `DbContext` |
| `INftStoreGrain` | `Vortex.Primitives/Collectibles/Grains/INftStoreGrain.cs` | `Vortex.Collectibles/Grains/NftStoreGrain.cs` | string | names a `DbContext` |
| `IPlayerAchievementGrain` | `Vortex.Primitives/Players/Grains/IPlayerAchievementGrain.cs` | `Vortex.Progression/Grains/PlayerAchievementGrain.cs` | long | names a `DbContext` |
| `IPlayerAchievementResolutionGrain` | `Vortex.Primitives/Players/Grains/IPlayerAchievementResolutionGrain.cs` | `Vortex.Progression/Grains/PlayerAchievementResolutionGrain.cs` | long | names a `DbContext` |
| `IPlayerBadgeGrain` | `Vortex.Primitives/Players/Grains/IPlayerBadgeGrain.cs` | `Vortex.Progression/Grains/PlayerBadgeGrain.cs` | long | names a `DbContext` |
| `IPlayerClothingGrain` | `Vortex.Primitives/Players/Grains/IPlayerClothingGrain.cs` | `Vortex.Players/Grains/PlayerClothingGrain.cs` | long | names a `DbContext` |
| `IPlayerDailyTaskGrain` | `Vortex.Primitives/Quests/Grains/IPlayerDailyTaskGrain.cs` | `Vortex.Progression/Grains/PlayerDailyTaskGrain.cs` | long | names a `DbContext` |
| `IPlayerDirectoryGrain` | `Vortex.Primitives/Players/Grains/IPlayerDirectoryGrain.cs` | `Vortex.Players/Grains/PlayerDirectoryGrain.cs` | string | names a `DbContext` |
| `IPlayerEffectGrain` | `Vortex.Primitives/Players/Grains/IPlayerEffectGrain.cs` | `Vortex.Players/Grains/PlayerEffectGrain.cs` | long | names a `DbContext` |
| `IPlayerGrain` | `Vortex.Primitives/Players/Grains/IPlayerGrain.cs` | `Vortex.Players/Grains/PlayerGrain.cs` | long | names a `DbContext` |
| `IPlayerMintGrain` | `Vortex.Primitives/Collectibles/Grains/IPlayerMintGrain.cs` | `Vortex.Collectibles/Grains/PlayerMintGrain.cs` | long | names a `DbContext` |
| `IPlayerMysteryBoxGrain` | `Vortex.Primitives/MysteryBox/Grains/IPlayerMysteryBoxGrain.cs` | `Vortex.Players/Grains/PlayerMysteryBoxGrain.cs` | long | names a `DbContext` |
| `IPlayerNavigatorGrain` | `Vortex.Primitives/Players/Grains/IPlayerNavigatorGrain.cs` | `Vortex.Players/Grains/PlayerNavigatorGrain.cs` | long | names a `DbContext` |
| `IPlayerNftClaimsGrain` | `Vortex.Primitives/Collectibles/Grains/IPlayerNftClaimsGrain.cs` | `Vortex.Collectibles/Grains/PlayerNftClaimsGrain.cs` | long | names a `DbContext` |
| `IPlayerNftWardrobeGrain` | `Vortex.Primitives/Players/Grains/IPlayerNftWardrobeGrain.cs` | `Vortex.Collectibles/Grains/PlayerNftWardrobeGrain.cs` | long | names a `DbContext` |
| `IPlayerPollGrain` | `Vortex.Primitives/Polls/Grains/IPlayerPollGrain.cs` | `Vortex.Progression/Grains/PlayerPollGrain.cs` | long | names a `DbContext` |
| `IPlayerPresenceGrain` | `Vortex.Primitives/Players/Grains/IPlayerPresenceGrain.Wallet.cs` | `Vortex.Players/Grains/PlayerPresenceGrain.cs` | long | &mdash; |
| `IPlayerPrizeGrain` | `Vortex.Primitives/Prizes/Grains/IPlayerPrizeGrain.cs` | `Vortex.Progression/Grains/PlayerPrizeGrain.cs` | long | names a `DbContext` |
| `IPlayerQuestGrain` | `Vortex.Primitives/Quests/Grains/IPlayerQuestGrain.cs` | `Vortex.Progression/Grains/PlayerQuestGrain.cs` | long | names a `DbContext` |
| `IPlayerQuizGrain` | `Vortex.Primitives/Players/Grains/IPlayerQuizGrain.cs` | `Vortex.Social/Grains/PlayerQuizGrain.cs` | long | names a `DbContext` |
| `IPlayerTargetedOfferGrain` | `Vortex.Primitives/Catalog/Grains/IPlayerTargetedOfferGrain.cs` | `Vortex.Catalog/Grains/PlayerTargetedOfferGrain.cs` | long | names a `DbContext` |
| `IPlayerVaultGrain` | `Vortex.Primitives/Players/Grains/IPlayerVaultGrain.cs` | `Vortex.Collectibles/Grains/PlayerVaultGrain.cs` | long | names a `DbContext` |
| `IPlayerWalletGrain` | `Vortex.Primitives/Players/Grains/IPlayerWalletGrain.cs` | `Vortex.Players/Grains/PlayerWalletGrain.cs` | long | names a `DbContext` |
| `IPollManagerGrain` | `Vortex.Primitives/Polls/Grains/IPollManagerGrain.cs` | `Vortex.Progression/Grains/PollManagerGrain.cs` | string | names a `DbContext` |
| `IPrizePoolManagerGrain` | `Vortex.Primitives/Prizes/Grains/IPrizePoolManagerGrain.cs` | `Vortex.Progression/Grains/PrizePoolManagerGrain.cs` | string | names a `DbContext` |
| `IQuestManagerGrain` | `Vortex.Primitives/Quests/Grains/IQuestManagerGrain.cs` | `Vortex.Progression/Grains/QuestManagerGrain.cs` | string | names a `DbContext` |
| `IRentableSpaceGrain` | `Vortex.Primitives/Rooms/Grains/IRentableSpaceGrain.cs` | `Vortex.Players/Grains/RentableSpaceGrain.cs` | long | names a `DbContext` |
| `IRoomDirectoryGrain` | `Vortex.Primitives/Rooms/Grains/IRoomDirectoryGrain.cs` | `Vortex.Rooms/Grains/RoomDirectoryGrain.cs` | string | &mdash; |
| `IRoomGrain` | `Vortex.Primitives/Rooms/Grains/IRoomGrain.cs` | `Vortex.Rooms/Grains/RoomGrain.cs` | long (via `IRoomCore`) | names a `DbContext` |
| `IRoomPersistenceGrain` | `Vortex.Primitives/Rooms/Grains/IRoomPersistenceGrain.cs` | `Vortex.Rooms/Grains/RoomPersistenceGrain.cs` | long | names a `DbContext` |
| `IServerConfigGrain` | `Vortex.Primitives/Server/Grains/IServerConfigGrain.cs` | `Vortex.Players/Grains/ServerConfigGrain.cs` | string | names a `DbContext` |
| `ITargetedOfferManagerGrain` | `Vortex.Primitives/Catalog/Grains/ITargetedOfferManagerGrain.cs` | `Vortex.Catalog/Grains/TargetedOfferManagerGrain.cs` | string | names a `DbContext` |
| `IVoucherGrain` | `Vortex.Primitives/Catalog/Grains/IVoucherGrain.cs` | `Vortex.Catalog/Grains/VoucherGrain.cs` | string | names a `DbContext` |
