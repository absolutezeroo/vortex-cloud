# Packet handler index

> **Generated reference index.** This file inventories code symbols from a static scan of the
> repository at commit `e57f0be79a96` and should not be used as the sole source for runtime semantics.
> Regenerate with `/document-vortex update`. Explanatory pages live one directory up.


539 handler types under `Vortex.PacketHandlers/**`, grouped by domain folder. The filter is a declared `IMessageHandler<T>` implementation, not a `*Handler` filename &mdash; registration is a reflection scan for that closed generic and nothing else. Handlers are orchestration only: they validate input, call grains, and map snapshots to composers. See [`../02-network-protocol/packet-pipeline.md`](../02-network-protocol/packet-pipeline.md).

| Domain | Handlers |
|---|---|
| `Advertisement` | 2 |
| `Avatar` | 4 |
| `Camera` | 5 |
| `Campaign` | 2 |
| `Catalog` | 33 |
| `Collectibles` | 19 |
| `Competition` | 9 |
| `Crafting` | 5 |
| `FriendFurni` | 1 |
| `FriendList` | 15 |
| `Game` | 28 |
| `Gifts` | 5 |
| `GroupForums` | 12 |
| `Handshake` | 9 |
| `Help` | 27 |
| `Hotlooks` | 1 |
| `Inventory` | 27 |
| `Landingview` | 2 |
| `Marketplace` | 10 |
| `Moderator` | 21 |
| `MysteryBox` | 1 |
| `Navigator` | 36 |
| `NewNavigator` | 7 |
| `Nft` | 5 |
| `Notifications` | 2 |
| `Nux` | 3 |
| `Poll` | 3 |
| `Preferences` | 10 |
| `Quest` | 16 |
| `Register` | 1 |
| `Room` | 102 |
| `RoomDirectory` | 1 |
| `RoomSettings` | 8 |
| `Sound` | 9 |
| `Talent` | 3 |
| `Tracking` | 5 |
| `UserClassification` | 2 |
| `UserDefinedRoomEvents` | 41 |
| `Users` | 43 |
| `Vault` | 4 |

## Advertisement

| Handler | File |
|---|---|
| `GetInterstitialMessageHandler` | `Vortex.PacketHandlers/Advertisement/GetInterstitialMessageHandler.cs` |
| `InterstitialShownMessageHandler` | `Vortex.PacketHandlers/Advertisement/InterstitialShownMessageHandler.cs` |

## Avatar

| Handler | File |
|---|---|
| `ChangeUserNameMessageHandler` | `Vortex.PacketHandlers/Avatar/ChangeUserNameMessageHandler.cs` |
| `CheckUserNameMessageHandler` | `Vortex.PacketHandlers/Avatar/CheckUserNameMessageHandler.cs` |
| `GetWardrobeMessageHandler` | `Vortex.PacketHandlers/Avatar/GetWardrobeMessageHandler.cs` |
| `SaveWardrobeOutfitMessageHandler` | `Vortex.PacketHandlers/Avatar/SaveWardrobeOutfitMessageHandler.cs` |

## Camera

| Handler | File |
|---|---|
| `PhotoCompetitionMessageHandler` | `Vortex.PacketHandlers/Camera/PhotoCompetitionMessageHandler.cs` |
| `PublishPhotoMessageHandler` | `Vortex.PacketHandlers/Camera/PublishPhotoMessageHandler.cs` |
| `PurchasePhotoMessageHandler` | `Vortex.PacketHandlers/Camera/PurchasePhotoMessageHandler.cs` |
| `RenderRoomMessageHandler` | `Vortex.PacketHandlers/Camera/RenderRoomMessageHandler.cs` |
| `RequestCameraConfigurationMessageHandler` | `Vortex.PacketHandlers/Camera/RequestCameraConfigurationMessageHandler.cs` |

## Campaign

| Handler | File |
|---|---|
| `OpenCampaignCalendarDoorAsStaffMessageHandler` | `Vortex.PacketHandlers/Campaign/OpenCampaignCalendarDoorAsStaffMessageHandler.cs` |
| `OpenCampaignCalendarDoorMessageHandler` | `Vortex.PacketHandlers/Campaign/OpenCampaignCalendarDoorMessageHandler.cs` |

## Catalog

| Handler | File |
|---|---|
| `BuildersClubPlaceRoomItemMessageHandler` | `Vortex.PacketHandlers/Catalog/BuildersClubPlaceRoomItemMessageHandler.cs` |
| `BuildersClubPlaceWallItemMessageHandler` | `Vortex.PacketHandlers/Catalog/BuildersClubPlaceWallItemMessageHandler.cs` |
| `BuildersClubQueryFurniCountMessageHandler` | `Vortex.PacketHandlers/Catalog/BuildersClubQueryFurniCountMessageHandler.cs` |
| `ChargeFireworkMessageHandler` | `Vortex.PacketHandlers/Catalog/ChargeFireworkMessageHandler.cs` |
| `GetBonusRareInfoMessageHandler` | `Vortex.PacketHandlers/Catalog/GetBonusRareInfoMessageHandler.cs` |
| `GetBundleDiscountRulesetMessageHandler` | `Vortex.PacketHandlers/Catalog/GetBundleDiscountRulesetMessageHandler.cs` |
| `GetCatalogIndexMessageHandler` | `Vortex.PacketHandlers/Catalog/GetCatalogIndexMessageHandler.cs` |
| `GetCatalogPageMessageHandler` | `Vortex.PacketHandlers/Catalog/GetCatalogPageMessageHandler.cs` |
| `GetCatalogPageWithEarliestExpiryMessageHandler` | `Vortex.PacketHandlers/Catalog/GetCatalogPageWithEarliestExpiryMessageHandler.cs` |
| `GetClubGiftInfoMessageHandler` | `Vortex.PacketHandlers/Catalog/GetClubGiftInfoMessageHandler.cs` |
| `GetClubOffersMessageHandler` | `Vortex.PacketHandlers/Catalog/GetClubOffersMessageHandler.cs` |
| `GetGiftWrappingConfigurationMessageHandler` | `Vortex.PacketHandlers/Catalog/GetGiftWrappingConfigurationMessageHandler.cs` |
| `GetHabboClubExtendOfferMessageHandler` | `Vortex.PacketHandlers/Catalog/GetHabboClubExtendOfferMessageHandler.cs` |
| `GetIsOfferGiftableMessageHandler` | `Vortex.PacketHandlers/Catalog/GetIsOfferGiftableMessageHandler.cs` |
| `GetLimitedOfferAppearingNextMessageHandler` | `Vortex.PacketHandlers/Catalog/GetLimitedOfferAppearingNextMessageHandler.cs` |
| `GetNextTargetedOfferMessageHandler` | `Vortex.PacketHandlers/Catalog/GetNextTargetedOfferMessageHandler.cs` |
| `GetProductOfferMessageHandler` | `Vortex.PacketHandlers/Catalog/GetProductOfferMessageHandler.cs` |
| `GetRoomAdPurchaseInfoMessageHandler` | `Vortex.PacketHandlers/Catalog/GetRoomAdPurchaseInfoMessageHandler.cs` |
| `GetSeasonalCalendarDailyOfferMessageHandler` | `Vortex.PacketHandlers/Catalog/GetSeasonalCalendarDailyOfferMessageHandler.cs` |
| `GetSellablePetPalettesMessageHandler` | `Vortex.PacketHandlers/Catalog/GetSellablePetPalettesMessageHandler.cs` |
| `GetTargetedOfferMessageHandler` | `Vortex.PacketHandlers/Catalog/GetTargetedOfferMessageHandler.cs` |
| `MarkCatalogNewAdditionsPageOpenedMessageHandler` | `Vortex.PacketHandlers/Catalog/MarkCatalogNewAdditionsPageOpenedMessageHandler.cs` |
| `PurchaseBasicMembershipExtensionMessageHandler` | `Vortex.PacketHandlers/Catalog/PurchaseBasicMembershipExtensionMessageHandler.cs` |
| `PurchaseFromCatalogAsGiftMessageHandler` | `Vortex.PacketHandlers/Catalog/PurchaseFromCatalogAsGiftMessageHandler.cs` |
| `PurchaseFromCatalogMessageHandler` | `Vortex.PacketHandlers/Catalog/PurchaseFromCatalogMessageHandler.cs` |
| `PurchaseRoomAdMessageMessageHandler` | `Vortex.PacketHandlers/Catalog/PurchaseRoomAdMessageMessageHandler.cs` |
| `PurchaseTargetedOfferMessageHandler` | `Vortex.PacketHandlers/Catalog/PurchaseTargetedOfferMessageHandler.cs` |
| `PurchaseVipMembershipExtensionMessageHandler` | `Vortex.PacketHandlers/Catalog/PurchaseVipMembershipExtensionMessageHandler.cs` |
| `RedeemVoucherMessageHandler` | `Vortex.PacketHandlers/Catalog/RedeemVoucherMessageHandler.cs` |
| `RoomAdPurchaseInitiatedMessageHandler` | `Vortex.PacketHandlers/Catalog/RoomAdPurchaseInitiatedMessageHandler.cs` |
| `SelectClubGiftMessageHandler` | `Vortex.PacketHandlers/Catalog/SelectClubGiftMessageHandler.cs` |
| `SetTargetedOfferStateMessageHandler` | `Vortex.PacketHandlers/Catalog/SetTargetedOfferStateMessageHandler.cs` |
| `ShopTargetedOfferViewedMessageHandler` | `Vortex.PacketHandlers/Catalog/ShopTargetedOfferViewedMessageHandler.cs` |

## Collectibles

| Handler | File |
|---|---|
| `AddNftToTradeMessageHandler` | `Vortex.PacketHandlers/Collectibles/AddNftToTradeMessageHandler.cs` |
| `ClaimNftClaimsMessageHandler` | `Vortex.PacketHandlers/Collectibles/ClaimNftClaimsMessageHandler.cs` |
| `GetCollectibleMintTokensMessageHandler` | `Vortex.PacketHandlers/Collectibles/GetCollectibleMintTokensMessageHandler.cs` |
| `GetCollectibleMintableItemTypesMessageHandler` | `Vortex.PacketHandlers/Collectibles/GetCollectibleMintableItemTypesMessageHandler.cs` |
| `GetCollectibleMintingEnabledMessageHandler` | `Vortex.PacketHandlers/Collectibles/GetCollectibleMintingEnabledMessageHandler.cs` |
| `GetCollectibleWalletAddressesMessageHandler` | `Vortex.PacketHandlers/Collectibles/GetCollectibleWalletAddressesMessageHandler.cs` |
| `GetCollectorScoreMessageHandler` | `Vortex.PacketHandlers/Collectibles/GetCollectorScoreMessageHandler.cs` |
| `GetMintTokenOffersMessageHandler` | `Vortex.PacketHandlers/Collectibles/GetMintTokenOffersMessageHandler.cs` |
| `GetNftAssetInventoryMessageHandler` | `Vortex.PacketHandlers/Collectibles/GetNftAssetInventoryMessageHandler.cs` |
| `GetNftClaimsMessageHandler` | `Vortex.PacketHandlers/Collectibles/GetNftClaimsMessageHandler.cs` |
| `GetNftCollectionsMessageHandler` | `Vortex.PacketHandlers/Collectibles/GetNftCollectionsMessageHandler.cs` |
| `GetNftStoreOffersMessageHandler` | `Vortex.PacketHandlers/Collectibles/GetNftStoreOffersMessageHandler.cs` |
| `GetNftTransferFeeMessageHandler` | `Vortex.PacketHandlers/Collectibles/GetNftTransferFeeMessageHandler.cs` |
| `MintItemMessageHandler` | `Vortex.PacketHandlers/Collectibles/MintItemMessageHandler.cs` |
| `NftCollectiblesClaimBonusItemMessageHandler` | `Vortex.PacketHandlers/Collectibles/NftCollectiblesClaimBonusItemMessageHandler.cs` |
| `NftCollectiblesClaimRewardItemMessageHandler` | `Vortex.PacketHandlers/Collectibles/NftCollectiblesClaimRewardItemMessageHandler.cs` |
| `NftStorePurchaseMessageHandler` | `Vortex.PacketHandlers/Collectibles/NftStorePurchaseMessageHandler.cs` |
| `PurchaseMintTokenMessageHandler` | `Vortex.PacketHandlers/Collectibles/PurchaseMintTokenMessageHandler.cs` |
| `TransferNftAssetsMessageHandler` | `Vortex.PacketHandlers/Collectibles/TransferNftAssetsMessageHandler.cs` |

## Competition

| Handler | File |
|---|---|
| `ForwardToACompetitionRoomMessageHandler` | `Vortex.PacketHandlers/Competition/ForwardToACompetitionRoomMessageHandler.cs` |
| `ForwardToASubmittableRoomMessageHandler` | `Vortex.PacketHandlers/Competition/ForwardToASubmittableRoomMessageHandler.cs` |
| `ForwardToRandomCompetitionRoomMessageHandler` | `Vortex.PacketHandlers/Competition/ForwardToRandomCompetitionRoomMessageHandler.cs` |
| `GetCurrentTimingCodeMessageHandler` | `Vortex.PacketHandlers/Competition/GetCurrentTimingCodeMessageHandler.cs` |
| `GetIsUserPartOfCompetitionMessageHandler` | `Vortex.PacketHandlers/Competition/GetIsUserPartOfCompetitionMessageHandler.cs` |
| `GetSecondsUntilMessageHandler` | `Vortex.PacketHandlers/Competition/GetSecondsUntilMessageHandler.cs` |
| `RoomCompetitionInitMessageHandler` | `Vortex.PacketHandlers/Competition/RoomCompetitionInitMessageHandler.cs` |
| `SubmitRoomToCompetitionMessageHandler` | `Vortex.PacketHandlers/Competition/SubmitRoomToCompetitionMessageHandler.cs` |
| `VoteForRoomMessageHandler` | `Vortex.PacketHandlers/Competition/VoteForRoomMessageHandler.cs` |

## Crafting

| Handler | File |
|---|---|
| `CraftMessageHandler` | `Vortex.PacketHandlers/Crafting/CraftMessageHandler.cs` |
| `CraftSecretMessageHandler` | `Vortex.PacketHandlers/Crafting/CraftSecretMessageHandler.cs` |
| `GetCraftableProductsMessageHandler` | `Vortex.PacketHandlers/Crafting/GetCraftableProductsMessageHandler.cs` |
| `GetCraftingRecipeMessageHandler` | `Vortex.PacketHandlers/Crafting/GetCraftingRecipeMessageHandler.cs` |
| `GetCraftingRecipesAvailableMessageHandler` | `Vortex.PacketHandlers/Crafting/GetCraftingRecipesAvailableMessageHandler.cs` |

## FriendFurni

| Handler | File |
|---|---|
| `FriendFurniConfirmLockMessageHandler` | `Vortex.PacketHandlers/FriendFurni/FriendFurniConfirmLockMessageHandler.cs` |

## FriendList

| Handler | File |
|---|---|
| `AcceptFriendMessageHandler` | `Vortex.PacketHandlers/FriendList/AcceptFriendMessageHandler.cs` |
| `DeclineFriendMessageHandler` | `Vortex.PacketHandlers/FriendList/DeclineFriendMessageHandler.cs` |
| `FindNewFriendsMessageHandler` | `Vortex.PacketHandlers/FriendList/FindNewFriendsMessageHandler.cs` |
| `FollowFriendMessageHandler` | `Vortex.PacketHandlers/FriendList/FollowFriendMessageHandler.cs` |
| `FriendListUpdateMessageHandler` | `Vortex.PacketHandlers/FriendList/FriendListUpdateMessageHandler.cs` |
| `GetFriendRequestsMessageHandler` | `Vortex.PacketHandlers/FriendList/GetFriendRequestsMessageHandler.cs` |
| `GetMessengerHistoryMessageHandler` | `Vortex.PacketHandlers/FriendList/GetMessengerHistoryMessageHandler.cs` |
| `HabboSearchMessageHandler` | `Vortex.PacketHandlers/FriendList/HabboSearchMessageHandler.cs` |
| `MessengerInitMessageHandler` | `Vortex.PacketHandlers/FriendList/MessengerInitMessageHandler.cs` |
| `RemoveFriendMessageHandler` | `Vortex.PacketHandlers/FriendList/RemoveFriendMessageHandler.cs` |
| `RequestFriendMessageHandler` | `Vortex.PacketHandlers/FriendList/RequestFriendMessageHandler.cs` |
| `SendMsgMessageHandler` | `Vortex.PacketHandlers/FriendList/SendMsgMessageHandler.cs` |
| `SendRoomInviteMessageHandler` | `Vortex.PacketHandlers/FriendList/SendRoomInviteMessageHandler.cs` |
| `SetRelationshipStatusMessageHandler` | `Vortex.PacketHandlers/FriendList/SetRelationshipStatusMessageHandler.cs` |
| `VisitUserMessageHandler` | `Vortex.PacketHandlers/FriendList/VisitUserMessageHandler.cs` |

## Game

| Handler | File |
|---|---|
| `Game2CheckGameDirectoryStatusMessageHandler` | `Vortex.PacketHandlers/Game/Directory/Game2CheckGameDirectoryStatusMessageHandler.cs` |
| `Game2ExitGameMessageHandler` | `Vortex.PacketHandlers/Game/Arena/Game2ExitGameMessageHandler.cs` |
| `Game2GameChatMessageHandler` | `Vortex.PacketHandlers/Game/Arena/Game2GameChatMessageHandler.cs` |
| `Game2GetAccountGameStatusMessageHandler` | `Vortex.PacketHandlers/Game/Directory/Game2GetAccountGameStatusMessageHandler.cs` |
| `Game2GetFriendsLeaderboardMessageHandler` | `Vortex.PacketHandlers/Game/Score/Game2GetFriendsLeaderboardMessageHandler.cs` |
| `Game2GetTotalGroupLeaderboardMessageHandler` | `Vortex.PacketHandlers/Game/Score/Game2GetTotalGroupLeaderboardMessageHandler.cs` |
| `Game2GetTotalLeaderboardMessageHandler` | `Vortex.PacketHandlers/Game/Score/Game2GetTotalLeaderboardMessageHandler.cs` |
| `Game2GetWeeklyFriendsLeaderboardMessageHandler` | `Vortex.PacketHandlers/Game/Score/Game2GetWeeklyFriendsLeaderboardMessageHandler.cs` |
| `Game2GetWeeklyGroupLeaderboardMessageHandler` | `Vortex.PacketHandlers/Game/Score/Game2GetWeeklyGroupLeaderboardMessageHandler.cs` |
| `Game2GetWeeklyLeaderboardMessageHandler` | `Vortex.PacketHandlers/Game/Score/Game2GetWeeklyLeaderboardMessageHandler.cs` |
| `Game2LeaveGameMessageHandler` | `Vortex.PacketHandlers/Game/Directory/Game2LeaveGameMessageHandler.cs` |
| `Game2LoadStageReadyMessageHandler` | `Vortex.PacketHandlers/Game/Arena/Game2LoadStageReadyMessageHandler.cs` |
| `Game2MakeSnowballMessageHandler` | `Vortex.PacketHandlers/Game/Ingame/Game2MakeSnowballMessageHandler.cs` |
| `Game2PlayAgainMessageHandler` | `Vortex.PacketHandlers/Game/Arena/Game2PlayAgainMessageHandler.cs` |
| `Game2QuickJoinGameMessageHandler` | `Vortex.PacketHandlers/Game/Directory/Game2QuickJoinGameMessageHandler.cs` |
| `Game2RequestFullStatusUpdateMessageHandler` | `Vortex.PacketHandlers/Game/Ingame/Game2RequestFullStatusUpdateMessageHandler.cs` |
| `Game2SetUserMoveTargetMessageHandler` | `Vortex.PacketHandlers/Game/Ingame/Game2SetUserMoveTargetMessageHandler.cs` |
| `Game2StartSnowWarMessageHandler` | `Vortex.PacketHandlers/Game/Directory/Game2StartSnowWarMessageHandler.cs` |
| `Game2ThrowSnowballAtHumanMessageHandler` | `Vortex.PacketHandlers/Game/Ingame/Game2ThrowSnowballAtHumanMessageHandler.cs` |
| `Game2ThrowSnowballAtPositionMessageHandler` | `Vortex.PacketHandlers/Game/Ingame/Game2ThrowSnowballAtPositionMessageHandler.cs` |
| `GetFriendsWeeklyCompetitiveLeaderboardMessageHandler` | `Vortex.PacketHandlers/Game/Score/GetFriendsWeeklyCompetitiveLeaderboardMessageHandler.cs` |
| `GetResolutionAchievementsMessageHandler` | `Vortex.PacketHandlers/Game/Lobby/GetResolutionAchievementsMessageHandler.cs` |
| `GetUserGameAchievementsMessageHandler` | `Vortex.PacketHandlers/Game/Lobby/GetUserGameAchievementsMessageHandler.cs` |
| `GetWeeklyCompetitiveLeaderboardMessageHandler` | `Vortex.PacketHandlers/Game/Score/GetWeeklyCompetitiveLeaderboardMessageHandler.cs` |
| `GetWeeklyGameRewardMessageHandler` | `Vortex.PacketHandlers/Game/Score/GetWeeklyGameRewardMessageHandler.cs` |
| `GetWeeklyGameRewardWinnersMessageHandler` | `Vortex.PacketHandlers/Game/Score/GetWeeklyGameRewardWinnersMessageHandler.cs` |
| `ResetResolutionAchievementMessageHandler` | `Vortex.PacketHandlers/Game/Lobby/ResetResolutionAchievementMessageHandler.cs` |
| `class_165MessageHandler` | `Vortex.PacketHandlers/Game/Lobby/class_165MessageHandler.cs` |

## Gifts

| Handler | File |
|---|---|
| `ResetPhoneNumberStateMessageHandler` | `Vortex.PacketHandlers/Gifts/ResetPhoneNumberStateMessageHandler.cs` |
| `SetPhoneNumberVerificationStatusMessageHandler` | `Vortex.PacketHandlers/Gifts/SetPhoneNumberVerificationStatusMessageHandler.cs` |
| `TryPhoneNumberMessageHandler` | `Vortex.PacketHandlers/Gifts/TryPhoneNumberMessageHandler.cs` |
| `VerifyCodeMessageHandler` | `Vortex.PacketHandlers/Gifts/VerifyCodeMessageHandler.cs` |
| `class_200MessageHandler` | `Vortex.PacketHandlers/Gifts/class_200MessageHandler.cs` |

## GroupForums

| Handler | File |
|---|---|
| `GetForumStatsMessageHandler` | `Vortex.PacketHandlers/GroupForums/GetForumStatsMessageHandler.cs` |
| `GetForumsListMessageHandler` | `Vortex.PacketHandlers/GroupForums/GetForumsListMessageHandler.cs` |
| `GetMessagesMessageHandler` | `Vortex.PacketHandlers/GroupForums/GetMessagesMessageHandler.cs` |
| `GetThreadMessageHandler` | `Vortex.PacketHandlers/GroupForums/GetThreadMessageHandler.cs` |
| `GetThreadsMessageHandler` | `Vortex.PacketHandlers/GroupForums/GetThreadsMessageHandler.cs` |
| `GetUnreadForumsCountMessageHandler` | `Vortex.PacketHandlers/GroupForums/GetUnreadForumsCountMessageHandler.cs` |
| `ModerateMessageMessageHandler` | `Vortex.PacketHandlers/GroupForums/ModerateMessageMessageHandler.cs` |
| `ModerateThreadMessageHandler` | `Vortex.PacketHandlers/GroupForums/ModerateThreadMessageHandler.cs` |
| `PostMessageMessageHandler` | `Vortex.PacketHandlers/GroupForums/PostMessageMessageHandler.cs` |
| `UpdateForumReadMarkerMessageHandler` | `Vortex.PacketHandlers/GroupForums/UpdateForumReadMarkerMessageHandler.cs` |
| `UpdateForumSettingsMessageHandler` | `Vortex.PacketHandlers/GroupForums/UpdateForumSettingsMessageHandler.cs` |
| `UpdateThreadMessageHandler` | `Vortex.PacketHandlers/GroupForums/UpdateThreadMessageHandler.cs` |

## Handshake

| Handler | File |
|---|---|
| `ClientHelloMessageHandler` | `Vortex.PacketHandlers/Handshake/ClientHelloMessageHandler.cs` |
| `CompleteDiffieHandshakeMessageHandler` | `Vortex.PacketHandlers/Handshake/CompleteDiffieHandshakeMessageHandler.cs` |
| `DisconnectMessageHandler` | `Vortex.PacketHandlers/Handshake/DisconnectMessageHandler.cs` |
| `InfoRetrieveMessageHandler` | `Vortex.PacketHandlers/Handshake/InfoRetrieveMessageHandler.cs` |
| `InitDiffieHandshakeMessageHandler` | `Vortex.PacketHandlers/Handshake/InitDiffieHandshakeMessageHandler.cs` |
| `PongMessageHandler` | `Vortex.PacketHandlers/Handshake/PongMessageHandler.cs` |
| `SSOTicketMessageHandler` | `Vortex.PacketHandlers/Handshake/SSOTicketMessageHandler.cs` |
| `UniqueIdMessageHandler` | `Vortex.PacketHandlers/Handshake/UniqueIdMessageHandler.cs` |
| `VersionCheckMessageHandler` | `Vortex.PacketHandlers/Handshake/VersionCheckMessageHandler.cs` |

## Help

| Handler | File |
|---|---|
| `CallForHelpFromForumMessageMessageHandler` | `Vortex.PacketHandlers/Help/CallForHelpFromForumMessageMessageHandler.cs` |
| `CallForHelpFromForumThreadMessageHandler` | `Vortex.PacketHandlers/Help/CallForHelpFromForumThreadMessageHandler.cs` |
| `CallForHelpFromIMMessageHandler` | `Vortex.PacketHandlers/Help/CallForHelpFromIMMessageHandler.cs` |
| `CallForHelpFromPhotoMessageHandler` | `Vortex.PacketHandlers/Help/CallForHelpFromPhotoMessageHandler.cs` |
| `CallForHelpFromSelfieMessageHandler` | `Vortex.PacketHandlers/Help/CallForHelpFromSelfieMessageHandler.cs` |
| `CallForHelpMessageHandler` | `Vortex.PacketHandlers/Help/CallForHelpMessageHandler.cs` |
| `ChatReviewGuideDecidesOnOfferMessageHandler` | `Vortex.PacketHandlers/Help/ChatReviewGuideDecidesOnOfferMessageHandler.cs` |
| `ChatReviewGuideDetachedMessageHandler` | `Vortex.PacketHandlers/Help/ChatReviewGuideDetachedMessageHandler.cs` |
| `ChatReviewGuideVoteMessageHandler` | `Vortex.PacketHandlers/Help/ChatReviewGuideVoteMessageHandler.cs` |
| `ChatReviewSessionCreateMessageHandler` | `Vortex.PacketHandlers/Help/ChatReviewSessionCreateMessageHandler.cs` |
| `DeletePendingCallsForHelpMessageHandler` | `Vortex.PacketHandlers/Help/DeletePendingCallsForHelpMessageHandler.cs` |
| `GetCfhStatusMessageHandler` | `Vortex.PacketHandlers/Help/GetCfhStatusMessageHandler.cs` |
| `GetGuideReportingStatusMessageHandler` | `Vortex.PacketHandlers/Help/GetGuideReportingStatusMessageHandler.cs` |
| `GetMyCfhReportStatusMessageHandler` | `Vortex.PacketHandlers/Help/GetMyCfhReportStatusMessageHandler.cs` |
| `GetPendingCallsForHelpMessageHandler` | `Vortex.PacketHandlers/Help/GetPendingCallsForHelpMessageHandler.cs` |
| `GetQuizQuestionsMessageHandler` | `Vortex.PacketHandlers/Help/GetQuizQuestionsMessageHandler.cs` |
| `GuideSessionCreateMessageHandler` | `Vortex.PacketHandlers/Help/GuideSessionCreateMessageHandler.cs` |
| `GuideSessionFeedbackMessageHandler` | `Vortex.PacketHandlers/Help/GuideSessionFeedbackMessageHandler.cs` |
| `GuideSessionGetRequesterRoomMessageHandler` | `Vortex.PacketHandlers/Help/GuideSessionGetRequesterRoomMessageHandler.cs` |
| `GuideSessionGuideDecidesMessageHandler` | `Vortex.PacketHandlers/Help/GuideSessionGuideDecidesMessageHandler.cs` |
| `GuideSessionInviteRequesterMessageHandler` | `Vortex.PacketHandlers/Help/GuideSessionInviteRequesterMessageHandler.cs` |
| `GuideSessionIsTypingMessageHandler` | `Vortex.PacketHandlers/Help/GuideSessionIsTypingMessageHandler.cs` |
| `GuideSessionMessageMessageHandler` | `Vortex.PacketHandlers/Help/GuideSessionMessageMessageHandler.cs` |
| `GuideSessionOnDutyUpdateMessageHandler` | `Vortex.PacketHandlers/Help/GuideSessionOnDutyUpdateMessageHandler.cs` |
| `GuideSessionRequesterCancelsMessageHandler` | `Vortex.PacketHandlers/Help/GuideSessionRequesterCancelsMessageHandler.cs` |
| `GuideSessionResolvedMessageHandler` | `Vortex.PacketHandlers/Help/GuideSessionResolvedMessageHandler.cs` |
| `PostQuizAnswersMessageHandler` | `Vortex.PacketHandlers/Help/PostQuizAnswersMessageHandler.cs` |

## Hotlooks

| Handler | File |
|---|---|
| `GetHotLooksMessageHandler` | `Vortex.PacketHandlers/Hotlooks/GetHotLooksMessageHandler.cs` |

## Inventory

| Handler | File |
|---|---|
| `AcceptTradingMessageHandler` | `Vortex.PacketHandlers/Inventory/Trading/AcceptTradingMessageHandler.cs` |
| `AddItemToTradeMessageHandler` | `Vortex.PacketHandlers/Inventory/Trading/AddItemToTradeMessageHandler.cs` |
| `AddItemsToTradeMessageHandler` | `Vortex.PacketHandlers/Inventory/Trading/AddItemsToTradeMessageHandler.cs` |
| `AvatarEffectActivatedMessageHandler` | `Vortex.PacketHandlers/Inventory/Avatareffect/AvatarEffectActivatedMessageHandler.cs` |
| `AvatarEffectSelectedMessageHandler` | `Vortex.PacketHandlers/Inventory/Avatareffect/AvatarEffectSelectedMessageHandler.cs` |
| `CancelPetBreedingMessageHandler` | `Vortex.PacketHandlers/Inventory/Pets/CancelPetBreedingMessageHandler.cs` |
| `CloseTradingMessageHandler` | `Vortex.PacketHandlers/Inventory/Trading/CloseTradingMessageHandler.cs` |
| `ConfirmAcceptTradingMessageHandler` | `Vortex.PacketHandlers/Inventory/Trading/ConfirmAcceptTradingMessageHandler.cs` |
| `ConfirmDeclineTradingMessageHandler` | `Vortex.PacketHandlers/Inventory/Trading/ConfirmDeclineTradingMessageHandler.cs` |
| `ConfirmPetBreedingMessageHandler` | `Vortex.PacketHandlers/Inventory/Pets/ConfirmPetBreedingMessageHandler.cs` |
| `GetAchievementsMessageHandler` | `Vortex.PacketHandlers/Inventory/Achievements/GetAchievementsMessageHandler.cs` |
| `GetBadgePointLimitsMessageHandler` | `Vortex.PacketHandlers/Inventory/Badges/GetBadgePointLimitsMessageHandler.cs` |
| `GetBadgesMessageHandler` | `Vortex.PacketHandlers/Inventory/Badges/GetBadgesMessageHandler.cs` |
| `GetBotInventoryMessageHandler` | `Vortex.PacketHandlers/Inventory/Bots/GetBotInventoryMessageHandler.cs` |
| `GetCreditsInfoMessageHandler` | `Vortex.PacketHandlers/Inventory/Purse/GetCreditsInfoMessageHandler.cs` |
| `GetIsBadgeRequestFulfilledMessageHandler` | `Vortex.PacketHandlers/Inventory/Badges/GetIsBadgeRequestFulfilledMessageHandler.cs` |
| `GetPetInventoryMessageHandler` | `Vortex.PacketHandlers/Inventory/Pets/GetPetInventoryMessageHandler.cs` |
| `OpenTradingMessageHandler` | `Vortex.PacketHandlers/Inventory/Trading/OpenTradingMessageHandler.cs` |
| `RedeemPurchasableClothingMessageHandler` | `Vortex.PacketHandlers/Inventory/Clothing/RedeemPurchasableClothingMessageHandler.cs` |
| `RemoveItemFromTradeMessageHandler` | `Vortex.PacketHandlers/Inventory/Trading/RemoveItemFromTradeMessageHandler.cs` |
| `RequestABadgeMessageHandler` | `Vortex.PacketHandlers/Inventory/Badges/RequestABadgeMessageHandler.cs` |
| `RequestFurniInventoryMessageHandler` | `Vortex.PacketHandlers/Inventory/Furni/RequestFurniInventoryMessageHandler.cs` |
| `RequestFurniInventoryWhenNotInRoomMessageHandler` | `Vortex.PacketHandlers/Inventory/Furni/RequestFurniInventoryWhenNotInRoomMessageHandler.cs` |
| `RequestRoomPropertySetMessageHandler` | `Vortex.PacketHandlers/Inventory/Furni/RequestRoomPropertySetMessageHandler.cs` |
| `SetActivatedBadgesMessageHandler` | `Vortex.PacketHandlers/Inventory/Badges/SetActivatedBadgesMessageHandler.cs` |
| `SilverFeeMessageHandler` | `Vortex.PacketHandlers/Inventory/Trading/SilverFeeMessageHandler.cs` |
| `UnacceptTradingMessageHandler` | `Vortex.PacketHandlers/Inventory/Trading/UnacceptTradingMessageHandler.cs` |

## Landingview

| Handler | File |
|---|---|
| `CommunityGoalVoteMessageHandler` | `Vortex.PacketHandlers/Landingview/Votes/CommunityGoalVoteMessageHandler.cs` |
| `GetPromoArticlesMessageHandler` | `Vortex.PacketHandlers/Landingview/GetPromoArticlesMessageHandler.cs` |

## Marketplace

| Handler | File |
|---|---|
| `BuyMarketplaceOfferMessageHandler` | `Vortex.PacketHandlers/Marketplace/BuyMarketplaceOfferMessageHandler.cs` |
| `BuyMarketplaceTokensMessageHandler` | `Vortex.PacketHandlers/Marketplace/BuyMarketplaceTokensMessageHandler.cs` |
| `CancelMarketplaceOfferMessageHandler` | `Vortex.PacketHandlers/Marketplace/CancelMarketplaceOfferMessageHandler.cs` |
| `GetMarketplaceCanMakeOfferMessageHandler` | `Vortex.PacketHandlers/Marketplace/GetMarketplaceCanMakeOfferMessageHandler.cs` |
| `GetMarketplaceConfigurationMessageHandler` | `Vortex.PacketHandlers/Marketplace/GetMarketplaceConfigurationMessageHandler.cs` |
| `GetMarketplaceItemStatsMessageHandler` | `Vortex.PacketHandlers/Marketplace/GetMarketplaceItemStatsMessageHandler.cs` |
| `GetMarketplaceOffersMessageHandler` | `Vortex.PacketHandlers/Marketplace/GetMarketplaceOffersMessageHandler.cs` |
| `GetMarketplaceOwnOffersMessageHandler` | `Vortex.PacketHandlers/Marketplace/GetMarketplaceOwnOffersMessageHandler.cs` |
| `MakeOfferMessageHandler` | `Vortex.PacketHandlers/Marketplace/MakeOfferMessageHandler.cs` |
| `RedeemMarketplaceOfferCreditsMessageHandler` | `Vortex.PacketHandlers/Marketplace/RedeemMarketplaceOfferCreditsMessageHandler.cs` |

## Moderator

| Handler | File |
|---|---|
| `CloseIssueDefaultActionMessageHandler` | `Vortex.PacketHandlers/Moderator/CloseIssueDefaultActionMessageHandler.cs` |
| `CloseIssuesMessageHandler` | `Vortex.PacketHandlers/Moderator/CloseIssuesMessageHandler.cs` |
| `DefaultSanctionMessageHandler` | `Vortex.PacketHandlers/Moderator/DefaultSanctionMessageHandler.cs` |
| `GetCfhChatlogMessageHandler` | `Vortex.PacketHandlers/Moderator/GetCfhChatlogMessageHandler.cs` |
| `GetModeratorRoomInfoMessageHandler` | `Vortex.PacketHandlers/Moderator/GetModeratorRoomInfoMessageHandler.cs` |
| `GetModeratorUserInfoMessageHandler` | `Vortex.PacketHandlers/Moderator/GetModeratorUserInfoMessageHandler.cs` |
| `GetRoomChatlogMessageHandler` | `Vortex.PacketHandlers/Moderator/GetRoomChatlogMessageHandler.cs` |
| `GetRoomVisitsMessageHandler` | `Vortex.PacketHandlers/Moderator/GetRoomVisitsMessageHandler.cs` |
| `GetUserChatlogMessageHandler` | `Vortex.PacketHandlers/Moderator/GetUserChatlogMessageHandler.cs` |
| `ModAlertMessageHandler` | `Vortex.PacketHandlers/Moderator/ModAlertMessageHandler.cs` |
| `ModBanMessageHandler` | `Vortex.PacketHandlers/Moderator/ModBanMessageHandler.cs` |
| `ModKickMessageHandler` | `Vortex.PacketHandlers/Moderator/ModKickMessageHandler.cs` |
| `ModMessageMessageHandler` | `Vortex.PacketHandlers/Moderator/ModMessageMessageHandler.cs` |
| `ModMuteMessageHandler` | `Vortex.PacketHandlers/Moderator/ModMuteMessageHandler.cs` |
| `ModToolPreferencesMessageHandler` | `Vortex.PacketHandlers/Moderator/ModToolPreferencesMessageHandler.cs` |
| `ModToolRoomAlertMessageHandler` | `Vortex.PacketHandlers/Moderator/ModToolRoomAlertMessageHandler.cs` |
| `ModToolSanctionMessageHandler` | `Vortex.PacketHandlers/Moderator/ModToolSanctionMessageHandler.cs` |
| `ModTradingLockMessageHandler` | `Vortex.PacketHandlers/Moderator/ModTradingLockMessageHandler.cs` |
| `ModerateRoomMessageHandler` | `Vortex.PacketHandlers/Moderator/ModerateRoomMessageHandler.cs` |
| `PickIssuesMessageHandler` | `Vortex.PacketHandlers/Moderator/PickIssuesMessageHandler.cs` |
| `ReleaseIssuesMessageHandler` | `Vortex.PacketHandlers/Moderator/ReleaseIssuesMessageHandler.cs` |

## MysteryBox

| Handler | File |
|---|---|
| `MysteryBoxWaitingCanceledMessageHandler` | `Vortex.PacketHandlers/MysteryBox/MysteryBoxWaitingCanceledMessageHandler.cs` |

## Navigator

| Handler | File |
|---|---|
| `AddFavouriteRoomMessageHandler` | `Vortex.PacketHandlers/Navigator/AddFavouriteRoomMessageHandler.cs` |
| `CanCreateRoomMessageHandler` | `Vortex.PacketHandlers/Navigator/CanCreateRoomMessageHandler.cs` |
| `CancelEventMessageHandler` | `Vortex.PacketHandlers/Navigator/CancelEventMessageHandler.cs` |
| `CompetitionRoomsSearchMessageHandler` | `Vortex.PacketHandlers/Navigator/CompetitionRoomsSearchMessageHandler.cs` |
| `ConvertGlobalRoomIdMessageHandler` | `Vortex.PacketHandlers/Navigator/ConvertGlobalRoomIdMessageHandler.cs` |
| `CreateFlatMessageHandler` | `Vortex.PacketHandlers/Navigator/CreateFlatMessageHandler.cs` |
| `DeleteFavouriteRoomMessageHandler` | `Vortex.PacketHandlers/Navigator/DeleteFavouriteRoomMessageHandler.cs` |
| `EditEventMessageHandler` | `Vortex.PacketHandlers/Navigator/EditEventMessageHandler.cs` |
| `ForwardToARandomPromotedRoomMessageHandler` | `Vortex.PacketHandlers/Navigator/ForwardToARandomPromotedRoomMessageHandler.cs` |
| `ForwardToSomeRoomMessageHandler` | `Vortex.PacketHandlers/Navigator/ForwardToSomeRoomMessageHandler.cs` |
| `GetGuestRoomMessageHandler` | `Vortex.PacketHandlers/Navigator/GetGuestRoomMessageHandler.cs` |
| `GetOfficialRoomsMessageHandler` | `Vortex.PacketHandlers/Navigator/GetOfficialRoomsMessageHandler.cs` |
| `GetPopularRoomTagsMessageHandler` | `Vortex.PacketHandlers/Navigator/GetPopularRoomTagsMessageHandler.cs` |
| `GetUserEventCatsMessageHandler` | `Vortex.PacketHandlers/Navigator/GetUserEventCatsMessageHandler.cs` |
| `GetUserFlatCatsMessageHandler` | `Vortex.PacketHandlers/Navigator/GetUserFlatCatsMessageHandler.cs` |
| `GuildBaseSearchMessageHandler` | `Vortex.PacketHandlers/Navigator/GuildBaseSearchMessageHandler.cs` |
| `MyFavouriteRoomsSearchMessageHandler` | `Vortex.PacketHandlers/Navigator/MyFavouriteRoomsSearchMessageHandler.cs` |
| `MyFrequentRoomHistorySearchMessageHandler` | `Vortex.PacketHandlers/Navigator/MyFrequentRoomHistorySearchMessageHandler.cs` |
| `MyFriendsRoomsSearchMessageHandler` | `Vortex.PacketHandlers/Navigator/MyFriendsRoomsSearchMessageHandler.cs` |
| `MyGuildBasesSearchMessageHandler` | `Vortex.PacketHandlers/Navigator/MyGuildBasesSearchMessageHandler.cs` |
| `MyRecommendedRoomsMessageHandler` | `Vortex.PacketHandlers/Navigator/MyRecommendedRoomsMessageHandler.cs` |
| `MyRoomHistorySearchMessageHandler` | `Vortex.PacketHandlers/Navigator/MyRoomHistorySearchMessageHandler.cs` |
| `MyRoomRightsSearchMessageHandler` | `Vortex.PacketHandlers/Navigator/MyRoomRightsSearchMessageHandler.cs` |
| `MyRoomsSearchMessageHandler` | `Vortex.PacketHandlers/Navigator/MyRoomsSearchMessageHandler.cs` |
| `PopularRoomsSearchMessageHandler` | `Vortex.PacketHandlers/Navigator/PopularRoomsSearchMessageHandler.cs` |
| `RateFlatMessageHandler` | `Vortex.PacketHandlers/Navigator/RateFlatMessageHandler.cs` |
| `RemoveOwnRoomRightsRoomMessageHandler` | `Vortex.PacketHandlers/Navigator/RemoveOwnRoomRightsRoomMessageHandler.cs` |
| `RoomAdEventTabAdClickedMessageHandler` | `Vortex.PacketHandlers/Navigator/RoomAdEventTabAdClickedMessageHandler.cs` |
| `RoomAdEventTabViewedMessageHandler` | `Vortex.PacketHandlers/Navigator/RoomAdEventTabViewedMessageHandler.cs` |
| `RoomAdSearchMessageHandler` | `Vortex.PacketHandlers/Navigator/RoomAdSearchMessageHandler.cs` |
| `RoomTextSearchMessageHandler` | `Vortex.PacketHandlers/Navigator/RoomTextSearchMessageHandler.cs` |
| `RoomsWhereMyFriendsAreSearchMessageHandler` | `Vortex.PacketHandlers/Navigator/RoomsWhereMyFriendsAreSearchMessageHandler.cs` |
| `RoomsWithHighestScoreSearchMessageHandler` | `Vortex.PacketHandlers/Navigator/RoomsWithHighestScoreSearchMessageHandler.cs` |
| `SetRoomSessionTagsMessageHandler` | `Vortex.PacketHandlers/Navigator/SetRoomSessionTagsMessageHandler.cs` |
| `ToggleStaffPickMessageHandler` | `Vortex.PacketHandlers/Navigator/ToggleStaffPickMessageHandler.cs` |
| `UpdateHomeRoomMessageHandler` | `Vortex.PacketHandlers/Navigator/UpdateHomeRoomMessageHandler.cs` |

## NewNavigator

| Handler | File |
|---|---|
| `NavigatorAddCollapsedCategoryMessageHandler` | `Vortex.PacketHandlers/NewNavigator/NavigatorAddCollapsedCategoryMessageHandler.cs` |
| `NavigatorAddSavedSearchMessageHandler` | `Vortex.PacketHandlers/NewNavigator/NavigatorAddSavedSearchMessageHandler.cs` |
| `NavigatorDeleteSavedSearchMessageHandler` | `Vortex.PacketHandlers/NewNavigator/NavigatorDeleteSavedSearchMessageHandler.cs` |
| `NavigatorRemoveCollapsedCategoryMessageHandler` | `Vortex.PacketHandlers/NewNavigator/NavigatorRemoveCollapsedCategoryMessageHandler.cs` |
| `NavigatorSetSearchCodeViewModeMessageHandler` | `Vortex.PacketHandlers/NewNavigator/NavigatorSetSearchCodeViewModeMessageHandler.cs` |
| `NewNavigatorInitMessageHandler` | `Vortex.PacketHandlers/NewNavigator/NewNavigatorInitMessageHandler.cs` |
| `NewNavigatorSearchMessageHandler` | `Vortex.PacketHandlers/NewNavigator/NewNavigatorSearchMessageHandler.cs` |

## Nft

| Handler | File |
|---|---|
| `GetNftCreditsMessageHandler` | `Vortex.PacketHandlers/Nft/GetNftCreditsMessageHandler.cs` |
| `GetSelectedNftWardrobeOutfitMessageHandler` | `Vortex.PacketHandlers/Nft/GetSelectedNftWardrobeOutfitMessageHandler.cs` |
| `GetSilverMessageHandler` | `Vortex.PacketHandlers/Nft/GetSilverMessageHandler.cs` |
| `GetUserNftWardrobeMessageHandler` | `Vortex.PacketHandlers/Nft/GetUserNftWardrobeMessageHandler.cs` |
| `SaveUserNftWardrobeMessageHandler` | `Vortex.PacketHandlers/Nft/SaveUserNftWardrobeMessageHandler.cs` |

## Notifications

| Handler | File |
|---|---|
| `ResetUnseenItemIdsMessageHandler` | `Vortex.PacketHandlers/Notifications/ResetUnseenItemIdsMessageHandler.cs` |
| `ResetUnseenItemsMessageHandler` | `Vortex.PacketHandlers/Notifications/ResetUnseenItemsMessageHandler.cs` |

## Nux

| Handler | File |
|---|---|
| `NewUserExperienceGetGiftsMessageHandler` | `Vortex.PacketHandlers/Nux/NewUserExperienceGetGiftsMessageHandler.cs` |
| `NewUserExperienceScriptProceedMessageHandler` | `Vortex.PacketHandlers/Nux/NewUserExperienceScriptProceedMessageHandler.cs` |
| `SelectInitialRoomMessageHandler` | `Vortex.PacketHandlers/Nux/SelectInitialRoomMessageHandler.cs` |

## Poll

| Handler | File |
|---|---|
| `PollAnswerMessageHandler` | `Vortex.PacketHandlers/Poll/PollAnswerMessageHandler.cs` |
| `PollRejectMessageHandler` | `Vortex.PacketHandlers/Poll/PollRejectMessageHandler.cs` |
| `PollStartMessageHandler` | `Vortex.PacketHandlers/Poll/PollStartMessageHandler.cs` |

## Preferences

| Handler | File |
|---|---|
| `AddToCustomFilterMessageHandler` | `Vortex.PacketHandlers/Preferences/AddToCustomFilterMessageHandler.cs` |
| `GetCustomFilterMessageHandler` | `Vortex.PacketHandlers/Preferences/GetCustomFilterMessageHandler.cs` |
| `RemoveFromCustomFilterMessageHandler` | `Vortex.PacketHandlers/Preferences/RemoveFromCustomFilterMessageHandler.cs` |
| `SetChatPreferencesMessageHandler` | `Vortex.PacketHandlers/Preferences/SetChatPreferencesMessageHandler.cs` |
| `SetChatStylePreferenceMessageHandler` | `Vortex.PacketHandlers/Preferences/SetChatStylePreferenceMessageHandler.cs` |
| `SetIgnoreRoomInvitesMessageHandler` | `Vortex.PacketHandlers/Preferences/SetIgnoreRoomInvitesMessageHandler.cs` |
| `SetNewNavigatorWindowPreferencesMessageHandler` | `Vortex.PacketHandlers/Preferences/SetNewNavigatorWindowPreferencesMessageHandler.cs` |
| `SetRoomCameraPreferencesMessageHandler` | `Vortex.PacketHandlers/Preferences/SetRoomCameraPreferencesMessageHandler.cs` |
| `SetSoundSettingsMessageHandler` | `Vortex.PacketHandlers/Preferences/SetSoundSettingsMessageHandler.cs` |
| `SetUIFlagsMessageHandler` | `Vortex.PacketHandlers/Preferences/SetUIFlagsMessageHandler.cs` |

## Quest

| Handler | File |
|---|---|
| `AcceptQuestMessageHandler` | `Vortex.PacketHandlers/Quest/AcceptQuestMessageHandler.cs` |
| `ActivateQuestMessageHandler` | `Vortex.PacketHandlers/Quest/ActivateQuestMessageHandler.cs` |
| `CancelQuestMessageHandler` | `Vortex.PacketHandlers/Quest/CancelQuestMessageHandler.cs` |
| `ClaimDailyTaskMessageHandler` | `Vortex.PacketHandlers/Quest/ClaimDailyTaskMessageHandler.cs` |
| `FriendRequestQuestCompleteMessageHandler` | `Vortex.PacketHandlers/Quest/FriendRequestQuestCompleteMessageHandler.cs` |
| `GetCommunityGoalHallOfFameMessageHandler` | `Vortex.PacketHandlers/Quest/GetCommunityGoalHallOfFameMessageHandler.cs` |
| `GetCommunityGoalProgressMessageHandler` | `Vortex.PacketHandlers/Quest/GetCommunityGoalProgressMessageHandler.cs` |
| `GetConcurrentUsersGoalProgressMessageHandler` | `Vortex.PacketHandlers/Quest/GetConcurrentUsersGoalProgressMessageHandler.cs` |
| `GetConcurrentUsersRewardMessageHandler` | `Vortex.PacketHandlers/Quest/GetConcurrentUsersRewardMessageHandler.cs` |
| `GetDailyQuestMessageHandler` | `Vortex.PacketHandlers/Quest/GetDailyQuestMessageHandler.cs` |
| `GetDailyTasksMessageHandler` | `Vortex.PacketHandlers/Quest/GetDailyTasksMessageHandler.cs` |
| `GetQuestsMessageHandler` | `Vortex.PacketHandlers/Quest/GetQuestsMessageHandler.cs` |
| `GetSeasonalQuestsOnlyMessageHandler` | `Vortex.PacketHandlers/Quest/GetSeasonalQuestsOnlyMessageHandler.cs` |
| `OpenQuestTrackerMessageHandler` | `Vortex.PacketHandlers/Quest/OpenQuestTrackerMessageHandler.cs` |
| `RejectQuestMessageHandler` | `Vortex.PacketHandlers/Quest/RejectQuestMessageHandler.cs` |
| `StartCampaignMessageHandler` | `Vortex.PacketHandlers/Quest/StartCampaignMessageHandler.cs` |

## Register

| Handler | File |
|---|---|
| `UpdateFigureDataMessageHandler` | `Vortex.PacketHandlers/Register/UpdateFigureDataMessageHandler.cs` |

## Room

| Handler | File |
|---|---|
| `AddSpamWallPostItMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/AddSpamWallPostItMessageHandler.cs` |
| `AmbassadorAlertMessageHandler` | `Vortex.PacketHandlers/Room/Action/AmbassadorAlertMessageHandler.cs` |
| `AssignRightsMessageHandler` | `Vortex.PacketHandlers/Room/Action/AssignRightsMessageHandler.cs` |
| `AvatarExpressionMessageHandler` | `Vortex.PacketHandlers/Room/Avatar/AvatarExpressionMessageHandler.cs` |
| `BanUserWithDurationMessageHandler` | `Vortex.PacketHandlers/Room/Action/BanUserWithDurationMessageHandler.cs` |
| `BreedPetsMessageHandler` | `Vortex.PacketHandlers/Room/Pets/BreedPetsMessageHandler.cs` |
| `CancelTypingMessageHandler` | `Vortex.PacketHandlers/Room/Chat/CancelTypingMessageHandler.cs` |
| `ChangeMottoMessageHandler` | `Vortex.PacketHandlers/Room/Avatar/ChangeMottoMessageHandler.cs` |
| `ChangePostureMessageHandler` | `Vortex.PacketHandlers/Room/Avatar/ChangePostureMessageHandler.cs` |
| `ChangeQueueMessageHandler` | `Vortex.PacketHandlers/Room/Session/ChangeQueueMessageHandler.cs` |
| `ChatMessageHandler` | `Vortex.PacketHandlers/Room/Chat/ChatMessageHandler.cs` |
| `ClickCharacterMessageHandler` | `Vortex.PacketHandlers/Room/Engine/ClickCharacterMessageHandler.cs` |
| `ClickFurniMessageHandler` | `Vortex.PacketHandlers/Room/Engine/ClickFurniMessageHandler.cs` |
| `CommandBotMessageHandler` | `Vortex.PacketHandlers/Room/Bots/CommandBotMessageHandler.cs` |
| `CompostPlantMessageHandler` | `Vortex.PacketHandlers/Room/Pets/CompostPlantMessageHandler.cs` |
| `ConfigureRentableSpaceMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/ConfigureRentableSpaceMessageHandler.cs` |
| `ControlYoutubeDisplayPlaybackMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/ControlYoutubeDisplayPlaybackMessageHandler.cs` |
| `CreditFurniRedeemMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/CreditFurniRedeemMessageHandler.cs` |
| `CustomizePetWithFurniMessageHandler` | `Vortex.PacketHandlers/Room/Pets/CustomizePetWithFurniMessageHandler.cs` |
| `DanceMessageHandler` | `Vortex.PacketHandlers/Room/Avatar/DanceMessageHandler.cs` |
| `DiceOffMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/DiceOffMessageHandler.cs` |
| `DropCarryItemMessageHandler` | `Vortex.PacketHandlers/Room/Avatar/DropCarryItemMessageHandler.cs` |
| `EnterOneWayDoorMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/EnterOneWayDoorMessageHandler.cs` |
| `ExtendRentOrBuyoutFurniMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/ExtendRentOrBuyoutFurniMessageHandler.cs` |
| `ExtendRentOrBuyoutStripItemMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/ExtendRentOrBuyoutStripItemMessageHandler.cs` |
| `GetBotCommandConfigurationDataMessageHandler` | `Vortex.PacketHandlers/Room/Bots/GetBotCommandConfigurationDataMessageHandler.cs` |
| `GetFurnitureAliasesMessageHandler` | `Vortex.PacketHandlers/Room/Engine/GetFurnitureAliasesMessageHandler.cs` |
| `GetGuildFurniContextMenuInfoMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/GetGuildFurniContextMenuInfoMessageHandler.cs` |
| `GetItemDataMessageHandler` | `Vortex.PacketHandlers/Room/Engine/GetItemDataMessageHandler.cs` |
| `GetOccupiedTilesMessageHandler` | `Vortex.PacketHandlers/Room/Layout/GetOccupiedTilesMessageHandler.cs` |
| `GetPetCommandsMessageHandler` | `Vortex.PacketHandlers/Room/Engine/GetPetCommandsMessageHandler.cs` |
| `GetPetInfoMessageHandler` | `Vortex.PacketHandlers/Room/Pets/GetPetInfoMessageHandler.cs` |
| `GetRentOrBuyoutOfferMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/GetRentOrBuyoutOfferMessageHandler.cs` |
| `GetRentableSpaceConfigMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/GetRentableSpaceConfigMessageHandler.cs` |
| `GetRoomEntryDataMessageHandler` | `Vortex.PacketHandlers/Room/Engine/GetRoomEntryDataMessageHandler.cs` |
| `GetRoomEntryTileMessageHandler` | `Vortex.PacketHandlers/Room/Layout/GetRoomEntryTileMessageHandler.cs` |
| `GetYoutubeDisplayStatusMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/GetYoutubeDisplayStatusMessageHandler.cs` |
| `GiveSupplementToPetMessageHandler` | `Vortex.PacketHandlers/Room/Engine/GiveSupplementToPetMessageHandler.cs` |
| `HarvestPetMessageHandler` | `Vortex.PacketHandlers/Room/Pets/HarvestPetMessageHandler.cs` |
| `KickUserMessageHandler` | `Vortex.PacketHandlers/Room/Action/KickUserMessageHandler.cs` |
| `LetUserInMessageHandler` | `Vortex.PacketHandlers/Room/Action/LetUserInMessageHandler.cs` |
| `LookToMessageHandler` | `Vortex.PacketHandlers/Room/Avatar/LookToMessageHandler.cs` |
| `MountPetMessageHandler` | `Vortex.PacketHandlers/Room/Engine/MountPetMessageHandler.cs` |
| `MoveAvatarMessageHandler` | `Vortex.PacketHandlers/Room/Engine/MoveAvatarMessageHandler.cs` |
| `MoveObjectMessageHandler` | `Vortex.PacketHandlers/Room/Engine/MoveObjectMessageHandler.cs` |
| `MovePetMessageHandler` | `Vortex.PacketHandlers/Room/Engine/MovePetMessageHandler.cs` |
| `MoveWallItemMessageHandler` | `Vortex.PacketHandlers/Room/Engine/MoveWallItemMessageHandler.cs` |
| `MuteAllInRoomMessageHandler` | `Vortex.PacketHandlers/Room/Action/MuteAllInRoomMessageHandler.cs` |
| `MuteUserMessageHandler` | `Vortex.PacketHandlers/Room/Action/MuteUserMessageHandler.cs` |
| `OpenFlatConnectionMessageHandler` | `Vortex.PacketHandlers/Room/Session/OpenFlatConnectionMessageHandler.cs` |
| `OpenMysteryTrophyMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/OpenMysteryTrophyMessageHandler.cs` |
| `OpenPetPackageMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/OpenPetPackageMessageHandler.cs` |
| `PassCarryItemMessageHandler` | `Vortex.PacketHandlers/Room/Avatar/PassCarryItemMessageHandler.cs` |
| `PassCarryItemToPetMessageHandler` | `Vortex.PacketHandlers/Room/Avatar/PassCarryItemToPetMessageHandler.cs` |
| `PetSelectedMessageHandler` | `Vortex.PacketHandlers/Room/Pets/PetSelectedMessageHandler.cs` |
| `PickupObjectMessageHandler` | `Vortex.PacketHandlers/Room/Engine/PickupObjectMessageHandler.cs` |
| `PlaceBotMessageHandler` | `Vortex.PacketHandlers/Room/Engine/PlaceBotMessageHandler.cs` |
| `PlaceObjectMessageHandler` | `Vortex.PacketHandlers/Room/Engine/PlaceObjectMessageHandler.cs` |
| `PlacePetMessageHandler` | `Vortex.PacketHandlers/Room/Engine/PlacePetMessageHandler.cs` |
| `PlacePostItMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/PlacePostItMessageHandler.cs` |
| `PresentOpenMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/PresentOpenMessageHandler.cs` |
| `QuitMessageHandler` | `Vortex.PacketHandlers/Room/Session/QuitMessageHandler.cs` |
| `RemoveAllRightsMessageHandler` | `Vortex.PacketHandlers/Room/Action/RemoveAllRightsMessageHandler.cs` |
| `RemoveBotFromFlatMessageHandler` | `Vortex.PacketHandlers/Room/Engine/RemoveBotFromFlatMessageHandler.cs` |
| `RemoveItemMessageHandler` | `Vortex.PacketHandlers/Room/Engine/RemoveItemMessageHandler.cs` |
| `RemovePetFromFlatMessageHandler` | `Vortex.PacketHandlers/Room/Engine/RemovePetFromFlatMessageHandler.cs` |
| `RemoveRightsMessageHandler` | `Vortex.PacketHandlers/Room/Action/RemoveRightsMessageHandler.cs` |
| `RemoveSaddleFromPetMessageHandler` | `Vortex.PacketHandlers/Room/Engine/RemoveSaddleFromPetMessageHandler.cs` |
| `RentableSpaceCancelRentMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/RentableSpaceCancelRentMessageHandler.cs` |
| `RentableSpaceRentMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/RentableSpaceRentMessageHandler.cs` |
| `RentableSpaceStatusMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/RentableSpaceStatusMessageHandler.cs` |
| `RespectPetMessageHandler` | `Vortex.PacketHandlers/Room/Pets/RespectPetMessageHandler.cs` |
| `RoomDimmerChangeStateMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/RoomDimmerChangeStateMessageHandler.cs` |
| `RoomDimmerGetPresetsMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/RoomDimmerGetPresetsMessageHandler.cs` |
| `RoomDimmerSavePresetMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/RoomDimmerSavePresetMessageHandler.cs` |
| `SetAreaHideDataMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/SetAreaHideDataMessageHandler.cs` |
| `SetClothingChangeDataMessageHandler` | `Vortex.PacketHandlers/Room/Engine/SetClothingChangeDataMessageHandler.cs` |
| `SetCustomStackingHeightMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/SetCustomStackingHeightMessageHandler.cs` |
| `SetItemDataMessageHandler` | `Vortex.PacketHandlers/Room/Engine/SetItemDataMessageHandler.cs` |
| `SetMannequinFigureMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/SetMannequinFigureMessageHandler.cs` |
| `SetMannequinNameMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/SetMannequinNameMessageHandler.cs` |
| `SetObjectDataMessageHandler` | `Vortex.PacketHandlers/Room/Engine/SetObjectDataMessageHandler.cs` |
| `SetRandomStateMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/SetRandomStateMessageHandler.cs` |
| `SetRoomBackgroundColorDataMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/SetRoomBackgroundColorDataMessageHandler.cs` |
| `SetYoutubeDisplayPlaylistMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/SetYoutubeDisplayPlaylistMessageHandler.cs` |
| `ShoutMessageHandler` | `Vortex.PacketHandlers/Room/Chat/ShoutMessageHandler.cs` |
| `SignMessageHandler` | `Vortex.PacketHandlers/Room/Avatar/SignMessageHandler.cs` |
| `SpinWheelOfFortuneMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/SpinWheelOfFortuneMessageHandler.cs` |
| `StartTypingMessageHandler` | `Vortex.PacketHandlers/Room/Chat/StartTypingMessageHandler.cs` |
| `ThrowDiceMessageHandler` | `Vortex.PacketHandlers/Room/Furniture/ThrowDiceMessageHandler.cs` |
| `TogglePetBreedingPermissionMessageHandler` | `Vortex.PacketHandlers/Room/Engine/TogglePetBreedingPermissionMessageHandler.cs` |
| `TogglePetRidingPermissionMessageHandler` | `Vortex.PacketHandlers/Room/Engine/TogglePetRidingPermissionMessageHandler.cs` |
| `UnbanUserFromRoomMessageHandler` | `Vortex.PacketHandlers/Room/Action/UnbanUserFromRoomMessageHandler.cs` |
| `UnmuteUserMessageHandler` | `Vortex.PacketHandlers/Room/Action/UnmuteUserMessageHandler.cs` |
| `UpdateFloorPropertiesMessageHandler` | `Vortex.PacketHandlers/Room/Layout/UpdateFloorPropertiesMessageHandler.cs` |
| `UseFurnitureMessageHandler` | `Vortex.PacketHandlers/Room/Engine/UseFurnitureMessageHandler.cs` |
| `UseWallItemMessageHandler` | `Vortex.PacketHandlers/Room/Engine/UseWallItemMessageHandler.cs` |
| `VortexApplyFurniDefinitionMessageHandler` | `Vortex.PacketHandlers/Room/Engine/VortexFurniDefinitionMessageHandlers.cs` |
| `VortexApplyFurniEditMessageHandler` | `Vortex.PacketHandlers/Room/Engine/VortexApplyFurniEditMessageHandler.cs` |
| `VortexGetFurniDefinitionMessageHandler` | `Vortex.PacketHandlers/Room/Engine/VortexFurniDefinitionMessageHandlers.cs` |
| `VortexGetFurniEditorDataMessageHandler` | `Vortex.PacketHandlers/Room/Engine/VortexGetFurniEditorDataMessageHandler.cs` |
| `WhisperMessageHandler` | `Vortex.PacketHandlers/Room/Chat/WhisperMessageHandler.cs` |

## RoomDirectory

| Handler | File |
|---|---|
| `RoomNetworkOpenConnectionMessageHandler` | `Vortex.PacketHandlers/RoomDirectory/RoomNetworkOpenConnectionMessageHandler.cs` |

## RoomSettings

| Handler | File |
|---|---|
| `DeleteRoomMessageHandler` | `Vortex.PacketHandlers/RoomSettings/DeleteRoomMessageHandler.cs` |
| `GetBannedUsersFromRoomMessageHandler` | `Vortex.PacketHandlers/RoomSettings/GetBannedUsersFromRoomMessageHandler.cs` |
| `GetCustomRoomFilterMessageHandler` | `Vortex.PacketHandlers/RoomSettings/GetCustomRoomFilterMessageHandler.cs` |
| `GetFlatControllersMessageHandler` | `Vortex.PacketHandlers/RoomSettings/GetFlatControllersMessageHandler.cs` |
| `GetRoomSettingsMessageHandler` | `Vortex.PacketHandlers/RoomSettings/GetRoomSettingsMessageHandler.cs` |
| `SaveRoomSettingsMessageHandler` | `Vortex.PacketHandlers/RoomSettings/SaveRoomSettingsMessageHandler.cs` |
| `UpdateRoomCategoryAndTradeSettingsMessageHandler` | `Vortex.PacketHandlers/RoomSettings/UpdateRoomCategoryAndTradeSettingsMessageHandler.cs` |
| `UpdateRoomFilterMessageHandler` | `Vortex.PacketHandlers/RoomSettings/UpdateRoomFilterMessageHandler.cs` |

## Sound

| Handler | File |
|---|---|
| `AddJukeboxDiskMessageHandler` | `Vortex.PacketHandlers/Sound/AddJukeboxDiskMessageHandler.cs` |
| `GetJukeboxPlayListMessageHandler` | `Vortex.PacketHandlers/Sound/GetJukeboxPlayListMessageHandler.cs` |
| `GetNowPlayingMessageHandler` | `Vortex.PacketHandlers/Sound/GetNowPlayingMessageHandler.cs` |
| `GetOfficialSongIdMessageHandler` | `Vortex.PacketHandlers/Sound/GetOfficialSongIdMessageHandler.cs` |
| `GetSongInfoMessageHandler` | `Vortex.PacketHandlers/Sound/GetSongInfoMessageHandler.cs` |
| `GetSoundMachinePlayListMessageHandler` | `Vortex.PacketHandlers/Sound/GetSoundMachinePlayListMessageHandler.cs` |
| `GetSoundSettingsMessageHandler` | `Vortex.PacketHandlers/Sound/GetSoundSettingsMessageHandler.cs` |
| `GetUserSongDisksMessageHandler` | `Vortex.PacketHandlers/Sound/GetUserSongDisksMessageHandler.cs` |
| `RemoveJukeboxDiskMessageHandler` | `Vortex.PacketHandlers/Sound/RemoveJukeboxDiskMessageHandler.cs` |

## Talent

| Handler | File |
|---|---|
| `GetTalentTrackLevelMessageHandler` | `Vortex.PacketHandlers/Talent/GetTalentTrackLevelMessageHandler.cs` |
| `GetTalentTrackMessageHandler` | `Vortex.PacketHandlers/Talent/GetTalentTrackMessageHandler.cs` |
| `GuideAdvertisementReadMessageHandler` | `Vortex.PacketHandlers/Talent/GuideAdvertisementReadMessageHandler.cs` |

## Tracking

| Handler | File |
|---|---|
| `EventLogMessageHandler` | `Vortex.PacketHandlers/Tracking/EventLogMessageHandler.cs` |
| `LagWarningReportMessageHandler` | `Vortex.PacketHandlers/Tracking/LagWarningReportMessageHandler.cs` |
| `LatencyPingReportMessageHandler` | `Vortex.PacketHandlers/Tracking/LatencyPingReportMessageHandler.cs` |
| `LatencyPingRequestMessageHandler` | `Vortex.PacketHandlers/Tracking/LatencyPingRequestMessageHandler.cs` |
| `PerformanceLogMessageHandler` | `Vortex.PacketHandlers/Tracking/PerformanceLogMessageHandler.cs` |

## UserClassification

| Handler | File |
|---|---|
| `PeerUsersClassificationMessageHandler` | `Vortex.PacketHandlers/UserClassification/PeerUsersClassificationMessageHandler.cs` |
| `RoomUsersClassificationMessageHandler` | `Vortex.PacketHandlers/UserClassification/RoomUsersClassificationMessageHandler.cs` |

## UserDefinedRoomEvents

| Handler | File |
|---|---|
| `ApplySnapshotMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/ApplySnapshotMessageHandler.cs` |
| `CloseWiredChestMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/CloseWiredChestMessageHandler.cs` |
| `DepositToWiredChestMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/DepositToWiredChestMessageHandler.cs` |
| `GetWiredChestTransactionsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/GetWiredChestTransactionsMessageHandler.cs` |
| `GetWiredContractContentsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/GetWiredContractContentsMessageHandler.cs` |
| `GetWiredRoomTransactionsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/GetWiredRoomTransactionsMessageHandler.cs` |
| `GetWiredTransactionDetailsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/GetWiredTransactionDetailsMessageHandler.cs` |
| `OpenMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/OpenMessageHandler.cs` |
| `OpenWiredChestMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/OpenWiredChestMessageHandler.cs` |
| `SaveWiredChestNotificationSettingsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/SaveWiredChestNotificationSettingsMessageHandler.cs` |
| `SaveWiredChestSettingsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/SaveWiredChestSettingsMessageHandler.cs` |
| `SaveWiredContractMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/SaveWiredContractMessageHandler.cs` |
| `SetAllWiredChestLocksMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/SetAllWiredChestLocksMessageHandler.cs` |
| `SetWiredChestLockMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/SetWiredChestLockMessageHandler.cs` |
| `UpdateActionMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/UpdateActionMessageHandler.cs` |
| `UpdateAddonMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/UpdateAddonMessageHandler.cs` |
| `UpdateConditionMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/UpdateConditionMessageHandler.cs` |
| `UpdateSelectorMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/UpdateSelectorMessageHandler.cs` |
| `UpdateTriggerMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/UpdateTriggerMessageHandler.cs` |
| `UpdateVariableMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/UpdateVariableMessageHandler.cs` |
| `UpgradeWiredChestMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/UpgradeWiredChestMessageHandler.cs` |
| `WiredClearErrorLogsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredClearErrorLogsMessageHandler.cs` |
| `WiredGetAllVariableHoldersMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredGetAllVariableHoldersMessageHandler.cs` |
| `WiredGetAllVariablesDiffsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredGetAllVariablesDiffsMessageHandler.cs` |
| `WiredGetAllVariablesHashMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredGetAllVariablesHashMessageHandler.cs` |
| `WiredGetErrorLogsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredGetErrorLogsMessageHandler.cs` |
| `WiredGetRoomLogsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredGetRoomLogsMessageHandler.cs` |
| `WiredGetRoomSettingsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredGetRoomSettingsMessageHandler.cs` |
| `WiredGetRoomStatsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredGetRoomStatsMessageHandler.cs` |
| `WiredGetUserPermanentVariablesMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredGetUserPermanentVariablesMessageHandler.cs` |
| `WiredGetVariableOwnersPageMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredGetVariableOwnersPageMessageHandler.cs` |
| `WiredGetVariablesForObjectMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredGetVariablesForObjectMessageHandler.cs` |
| `WiredSetObjectVariableValueMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredSetObjectVariableValueMessageHandler.cs` |
| `WiredSetPreferencesMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredSetPreferencesMessageHandler.cs` |
| `WiredSetRoomSettingsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredmenu/WiredSetRoomSettingsMessageHandler.cs` |
| `WiredTradeAcceptMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/WiredTradeAcceptMessageHandler.cs` |
| `WiredTradeCancelMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/WiredTradeCancelMessageHandler.cs` |
| `WiredTradeUpdateItemsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/WiredTradeUpdateItemsMessageHandler.cs` |
| `WithdrawAllFromWiredChestMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/WithdrawAllFromWiredChestMessageHandler.cs` |
| `WithdrawWiredChestCreditsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/WithdrawWiredChestCreditsMessageHandler.cs` |
| `WithdrawWiredChestItemsMessageHandler` | `Vortex.PacketHandlers/UserDefinedRoomEvents/Wiredtrading/WithdrawWiredChestItemsMessageHandler.cs` |

## Users

| Handler | File |
|---|---|
| `AddAdminRightsToMemberMessageHandler` | `Vortex.PacketHandlers/Users/AddAdminRightsToMemberMessageHandler.cs` |
| `ApproveAllMembershipRequestsMessageHandler` | `Vortex.PacketHandlers/Users/ApproveAllMembershipRequestsMessageHandler.cs` |
| `ApproveMembershipRequestMessageHandler` | `Vortex.PacketHandlers/Users/ApproveMembershipRequestMessageHandler.cs` |
| `ApproveNameMessageHandler` | `Vortex.PacketHandlers/Users/ApproveNameMessageHandler.cs` |
| `BlockListInitMessageHandler` | `Vortex.PacketHandlers/Users/BlockListInitMessageHandler.cs` |
| `BlockUserMessageHandler` | `Vortex.PacketHandlers/Users/BlockUserMessageHandler.cs` |
| `ChangeEmailMessageHandler` | `Vortex.PacketHandlers/Users/ChangeEmailMessageHandler.cs` |
| `CreateGuildMessageHandler` | `Vortex.PacketHandlers/Users/CreateGuildMessageHandler.cs` |
| `DeactivateGuildMessageHandler` | `Vortex.PacketHandlers/Users/DeactivateGuildMessageHandler.cs` |
| `DeselectFavouriteHabboGroupMessageHandler` | `Vortex.PacketHandlers/Users/DeselectFavouriteHabboGroupMessageHandler.cs` |
| `GetEmailStatusMessageHandler` | `Vortex.PacketHandlers/Users/GetEmailStatusMessageHandler.cs` |
| `GetExtendedProfileByNameMessageHandler` | `Vortex.PacketHandlers/Users/GetExtendedProfileByNameMessageHandler.cs` |
| `GetExtendedProfileMessageHandler` | `Vortex.PacketHandlers/Users/GetExtendedProfileMessageHandler.cs` |
| `GetGuildCreationInfoMessageHandler` | `Vortex.PacketHandlers/Users/GetGuildCreationInfoMessageHandler.cs` |
| `GetGuildEditInfoMessageHandler` | `Vortex.PacketHandlers/Users/GetGuildEditInfoMessageHandler.cs` |
| `GetGuildEditorDataMessageHandler` | `Vortex.PacketHandlers/Users/GetGuildEditorDataMessageHandler.cs` |
| `GetGuildMembersMessageHandler` | `Vortex.PacketHandlers/Users/GetGuildMembersMessageHandler.cs` |
| `GetGuildMembershipsMessageHandler` | `Vortex.PacketHandlers/Users/GetGuildMembershipsMessageHandler.cs` |
| `GetHabboGroupBadgesMessageHandler` | `Vortex.PacketHandlers/Users/GetHabboGroupBadgesMessageHandler.cs` |
| `GetHabboGroupDetailsMessageHandler` | `Vortex.PacketHandlers/Users/GetHabboGroupDetailsMessageHandler.cs` |
| `GetIgnoredUsersMessageHandler` | `Vortex.PacketHandlers/Users/GetIgnoredUsersMessageHandler.cs` |
| `GetMOTDMessageHandler` | `Vortex.PacketHandlers/Users/GetMOTDMessageHandler.cs` |
| `GetMemberGuildItemCountMessageHandler` | `Vortex.PacketHandlers/Users/GetMemberGuildItemCountMessageHandler.cs` |
| `GetRelationshipStatusInfoMessageHandler` | `Vortex.PacketHandlers/Users/GetRelationshipStatusInfoMessageHandler.cs` |
| `GetSelectedBadgesMessageHandler` | `Vortex.PacketHandlers/Users/GetSelectedBadgesMessageHandler.cs` |
| `GetUserNftChatStylesMessageHandler` | `Vortex.PacketHandlers/Users/GetUserNftChatStylesMessageHandler.cs` |
| `GiveStarGemToUserMessageHandler` | `Vortex.PacketHandlers/Users/GiveStarGemToUserMessageHandler.cs` |
| `IgnoreUserMessageHandler` | `Vortex.PacketHandlers/Users/IgnoreUserMessageHandler.cs` |
| `JoinHabboGroupMessageHandler` | `Vortex.PacketHandlers/Users/JoinHabboGroupMessageHandler.cs` |
| `KickMemberMessageHandler` | `Vortex.PacketHandlers/Users/KickMemberMessageHandler.cs` |
| `RejectMembershipRequestMessageHandler` | `Vortex.PacketHandlers/Users/RejectMembershipRequestMessageHandler.cs` |
| `RemoveAdminRightsFromMemberMessageHandler` | `Vortex.PacketHandlers/Users/RemoveAdminRightsFromMemberMessageHandler.cs` |
| `RespectUserMessageHandler` | `Vortex.PacketHandlers/Users/RespectUserMessageHandler.cs` |
| `ScrGetKickbackInfoMessageHandler` | `Vortex.PacketHandlers/Users/ScrGetKickbackInfoMessageHandler.cs` |
| `ScrGetUserInfoMessageHandler` | `Vortex.PacketHandlers/Users/ScrGetUserInfoMessageHandler.cs` |
| `SelectFavouriteHabboGroupMessageHandler` | `Vortex.PacketHandlers/Users/SelectFavouriteHabboGroupMessageHandler.cs` |
| `UnblockGroupMemberMessageHandler` | `Vortex.PacketHandlers/Users/UnblockGroupMemberMessageHandler.cs` |
| `UnblockUserMessageHandler` | `Vortex.PacketHandlers/Users/UnblockUserMessageHandler.cs` |
| `UnignoreUserMessageHandler` | `Vortex.PacketHandlers/Users/UnignoreUserMessageHandler.cs` |
| `UpdateGuildBadgeMessageHandler` | `Vortex.PacketHandlers/Users/UpdateGuildBadgeMessageHandler.cs` |
| `UpdateGuildColorsMessageHandler` | `Vortex.PacketHandlers/Users/UpdateGuildColorsMessageHandler.cs` |
| `UpdateGuildIdentityMessageHandler` | `Vortex.PacketHandlers/Users/UpdateGuildIdentityMessageHandler.cs` |
| `UpdateGuildSettingsMessageHandler` | `Vortex.PacketHandlers/Users/UpdateGuildSettingsMessageHandler.cs` |

## Vault

| Handler | File |
|---|---|
| `CreditVaultStatusMessageHandler` | `Vortex.PacketHandlers/Vault/CreditVaultStatusMessageHandler.cs` |
| `IncomeRewardClaimMessageHandler` | `Vortex.PacketHandlers/Vault/IncomeRewardClaimMessageHandler.cs` |
| `IncomeRewardStatusMessageHandler` | `Vortex.PacketHandlers/Vault/IncomeRewardStatusMessageHandler.cs` |
| `WithdrawCreditVaultMessageHandler` | `Vortex.PacketHandlers/Vault/WithdrawCreditVaultMessageHandler.cs` |

