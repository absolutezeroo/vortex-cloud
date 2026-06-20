# Full delta between WIN63 source-master and Vortex stack (production-ready / AI version)
Date: 2026-06-16 15:59:35
Source: WIN63 source-master
Client: vortex-client
Emu: vortex-emulator

## Purpose of this version
- Transform the raw list into an execution-ready document.
- Add priority levels (P0/P1/P2) for your planning.


## Actionable priority
### P0 - WIREDS
Context: userdefinedroomevents is partially implemented on the AS3 client.
Goal: add transport + editor logic + action/condition execution.
- [ ] Missing outgoing userdefinedroomevents messages (20 expected): all items in the following section
- [ ] High-priority room incoming and outgoing messages
- [ ] Associated client handlers and logic
- [ ] Header/parser/serializer mapping on revision side for these flows

## Global summary
- Source incoming: 349
- Source outgoing: 519
- Source parser AS3: 532
- Headers Event: 519
- Headers Composer: 533
- Parsers C#: 501
- Serializers C#: 543
- Emu incoming: 501
- Emu outgoing: 513
- Missing source->client incoming: 170
- Missing source->client outgoing: 96
- Missing source->emu incoming: 489
- Missing source->emu outgoing: 491
- Missing incoming headers: 336
- Missing outgoing headers: 497
- Missing C# parsers: 522
- Missing C# serializers: 498

## Diffs source -> client: missing incoming
- Total: 170
### action (x5)
- [ ] CarryObjectMessageEvent
- [ ] DanceMessageEvent
- [ ] ExpressionMessageEvent
- [ ] SleepMessageEvent
- [ ] UseObjectMessageEvent

### advertisement (x1)
- [ ] InterstitialMessageEvent

### arena (x13)
- [ ] Game2ArenaEnteredMessageEvent
- [ ] Game2EnterArenaFailedMessageEvent
- [ ] Game2EnterArenaMessageEvent
- [ ] Game2GameChatFromPlayerMessageEvent
- [ ] Game2GameEndingMessageEvent
- [ ] Game2GameRejoinMessageEvent
- [ ] Game2PlayerExitedGameArenaMessageEvent
- [ ] Game2PlayerRematchesMessageEvent
- [ ] Game2StageEndingMessageEvent
- [ ] Game2StageLoadMessageEvent
- [ ] Game2StageRunningMessageEvent
- [ ] Game2StageStartingMessageEvent
- [ ] Game2StageStillLoadingMessageEvent

### availability (x5)
- [ ] AvailabilityStatusMessageEvent
- [ ] InfoHotelClosedMessageEvent
- [ ] InfoHotelClosingMessageEvent
- [ ] LoginFailedHotelClosedMessageEvent
- [ ] MaintenanceStatusMessageEvent

### callforhelp (x1)
- [ ] MyCfhReportStatusMessageEvent

### camera (x6)
- [ ] CameraPublishStatusMessageEvent
- [ ] CameraPurchaseOKMessageEvent
- [ ] CameraStorageUrlMessageEvent
- [ ] CompetitionStatusMessageEvent
- [ ] InitCameraMessageEvent
- [ ] ThumbnailStatusMessageEvent

### campaign (x2)
- [ ] CampaignCalendarDataMessageEvent
- [ ] CampaignCalendarDoorOpenedMessageEvent

### catalog (x5)
- [ ] LtdRaffleEnteredMessageEvent
- [ ] LtdRaffleResultMessageEvent
- [ ] RecyclerFinishedMessageEvent
- [ ] RecyclerPrizesMessageEvent
- [ ] RecyclerStatusMessageEvent

### chat (x6)
- [ ] ChatMessageEvent
- [ ] FloodControlMessageEvent
- [ ] RoomFilterSettingsMessageEvent
- [ ] ShoutMessageEvent
- [ ] UserTypingMessageEvent
- [ ] WhisperMessageEvent

### collectibles (x20)
- [ ] CollectableMintableItemTypesMessageEvent
- [ ] CollectibleMintableItemResultMessageEvent
- [ ] CollectibleMintingEnabledMessageEvent
- [ ] CollectibleMintTokenCountMessageEvent
- [ ] CollectibleMintTokenOffersMessageEvent
- [ ] CollectibleWalletAddressesMessageEvent
- [ ] EmeraldBalanceMessageEvent
- [ ] NftBonusItemClaimResultMessageEvent
- [ ] NftClaimResultMessageEvent
- [ ] NftClaimsMessageEvent
- [ ] NftCollectionsMessageEvent
- [ ] NftCollectionsScoreMessageEvent
- [ ] NftRewardItemClaimResultMessageEvent
- [ ] NftStoreOffersMessageEvent
- [ ] NftStorePurchaseMessageEvent
- [ ] NftTransferAssetsResultMessageEvent
- [ ] NftTransferFeeMessageEvent
- [ ] RedeemNftLootBoxResultMessageEvent
- [ ] RedeemNftLootBoxStateMessageEvent
- [ ] SilverBalanceMessageEvent

### competition (x6)
- [ ] CompetitionEntrySubmitResultMessageEvent
- [ ] CompetitionVotingInfoMessageEvent
- [ ] CurrentTimingCodeMessageEvent
- [ ] IsUserPartOfCompetitionMessageEvent
- [ ] NoOwnedRoomsAlertMessageEvent
- [ ] SecondsUntilMessageEvent

### crafting (x4)
- [ ] CraftableProductsMessageEvent
- [ ] CraftingRecipeMessageEvent
- [ ] CraftingRecipesAvailableMessageEvent
- [ ] CraftingResultMessageEvent

### customfilter (x2)
- [ ] GetCustomFilterResultMessageEvent
- [ ] ModifyCustomFilterResultMessageEvent

### directory (x15)
- [ ] Game2AccountGameStatusMessageEvent
- [ ] Game2GameCancelledMessageEvent
- [ ] Game2GameCreatedMessageEvent
- [ ] Game2GameDirectoryStatusMessageEvent
- [ ] Game2GameLongDataMessageEvent
- [ ] Game2GameNotFoundMessageEvent
- [ ] Game2GameStartedMessageEvent
- [ ] Game2InArenaQueueMessageEvent
- [ ] Game2JoiningGameFailedMessageEvent
- [ ] Game2StartCounterMessageEvent
- [ ] Game2StartingGameFailedMessageEvent
- [ ] Game2StopCounterMessageEvent
- [ ] Game2UserBlockedMessageEvent
- [ ] Game2UserJoinedGameMessageEvent
- [ ] Game2UserLeftGameMessageEvent

### engine (x6)
- [ ] BuildersClubPlacementWarningMessageEvent
- [ ] ItemsStateUpdateMessageEvent
- [ ] ItemStateUpdateMessageEvent
- [ ] ObjectRemoveConfirmMessageEvent
- [ ] ObjectRemoveMultipleMessageEvent
- [ ] WiredMovementsMessageEvent

### friendfurni (x3)
- [ ] FriendFurniCancelLockMessageEvent
- [ ] FriendFurniOtherLockConfirmedMessageEvent
- [ ] FriendFurniStartConfirmationMessageEvent

### furniture (x1)
- [ ] AreaHideMessageEvent

### gifts (x3)
- [ ] PhoneCollectionStateMessageEvent
- [ ] TryPhoneNumberResultMessageEvent
- [ ] TryVerificationCodeResultMessageEvent

### groupforums (x9)
- [ ] ForumDataMessageEvent
- [ ] ForumsListMessageEvent
- [ ] ForumThreadsMessageEvent
- [ ] PostMessageMessageEvent
- [ ] PostThreadMessageEvent
- [ ] ThreadMessagesMessageEvent
- [ ] UnreadForumsCountMessageEvent
- [ ] UpdateMessageMessageEvent
- [ ] UpdateThreadMessageEvent

### ingame (x2)
- [ ] Game2FullGameStatusMessageEvent
- [ ] Game2GameStatusMessageEvent

### layout (x2)
- [ ] RoomEntryTileMessageEvent
- [ ] RoomOccupiedTilesMessageEvent

### lobby (x4)
- [ ] AchievementResolutionCompletedMessageEvent
- [ ] AchievementResolutionProgressMessageEvent
- [ ] AchievementResolutionsMessageEvent
- [ ] UserGameAchievementsMessageEvent

### moderation (x1)
- [ ] BanInfoMessageEvent

### mysterybox (x4)
- [ ] CancelMysteryBoxWaitMessageEvent
- [ ] GotMysteryBoxPrizeMessageEvent
- [ ] MysteryBoxKeysMessageEvent
- [ ] ShowMysteryBoxWaitMessageEvent

### nft (x8)
- [ ] NftEmeraldConvertResultMessageEvent
- [ ] TradeNftAssetInventoryMessageEvent
- [ ] TradeNftAssetsMessageEvent
- [ ] UserNftChatStylesMessageEvent
- [ ] UserNftWardrobeMessageEvent
- [ ] UserNftWardrobeSelectionMessageEvent
- [ ] UserPurchasableChatStyleChangedMessageEvent
- [ ] UserPurchasableChatStylesMessageEvent

### perk (x1)
- [ ] PerkAllowancesMessageEvent

### permissions (x3)
- [ ] YouAreControllerMessageEvent
- [ ] YouAreNotControllerMessageEvent
- [ ] YouAreOwnerMessageEvent

### pets (x1)
- [ ] PetReceivedMessageEvent

### quest (x3)
- [ ] DailyTasksActiveListMessageEvent
- [ ] DailyTasksTasksAddedMessageEvent
- [ ] DailyTasksTaskUpdateMessageEvent

### session (x12)
- [ ] CantConnectMessageEvent
- [ ] CloseConnectionMessageEvent
- [ ] FlatAccessibleMessageEvent
- [ ] GamePlayerValueMessageEvent
- [ ] HanditemConfigurationMessageEvent
- [ ] OpenConnectionMessageEvent
- [ ] RoomForwardMessageEvent
- [ ] RoomQueueStatusMessageEvent
- [ ] RoomReadyMessageEvent
- [ ] YouAreNotSpectatorMessageEvent
- [ ] YouArePlayingGameMessageEvent
- [ ] YouAreSpectatorMessageEvent

### talent (x3)
- [ ] TalentLevelUpMessageEvent
- [ ] TalentTrackLevelMessageEvent
- [ ] TalentTrackMessageEvent

### tracking (x1)
- [ ] LatencyPingResponseMessageEvent

### trading (x2)
- [ ] TradeSilverFeeMessageEvent
- [ ] TradeSilverSetMessageEvent

### treasurehunt (x3)
- [ ] TreasureHuntFailMessageEvent
- [ ] TreasureHuntFirstWinnerMessageEvent
- [ ] TreasureHuntUpdateMessageEvent

### userclassification (x1)
- [ ] UserClassificationMessageEvent

### userdefinedroomevents (x1)
- [ ] WiredEnvironmentMessageEvent

### users (x2)
- [ ] BlockListMessageEvent
- [ ] BlockUserUpdateMessageEvent

### vault (x3)
- [ ] IncomeRewardClaimResponseMessageEvent
- [ ] IncomeRewardNotificationMessageEvent
- [ ] IncomeRewardStatusMessageEvent

## Diffs source -> client: missing outgoing
- Total: 96
### avatar (x1)
- [ ] ChangeUserNameInRoomMessageComposer

### camera (x1)
- [ ] RequestCameraConfigurationMessageComposer

### catalog (x10)
- [ ] GetBonusRareInfoMessageComposer
- [ ] GetBundleDiscountRulesetComposer
- [ ] GetCatalogPageWithEarliestExpiryComposer
- [ ] GetClubGiftMessageComposer
- [ ] GetGiftWrappingConfigurationComposer
- [ ] GetLimitedOfferAppearingNextComposer
- [ ] GetRecyclerPrizesMessageComposer
- [ ] GetRecyclerStatusMessageComposer
- [ ] GetSeasonalCalendarDailyComposer
- [ ] RecycleItemsMessageComposer

### chat (x2)
- [ ] CancelTypingMessageComposer
- [ ] StartTypingMessageComposer

### collectibles (x17)
- [ ] ClaimNftClaimsMessageComposer
- [ ] GetCollectibleMintableItemTypesMessageComposer
- [ ] GetCollectibleMintingEnabledMessageComposer
- [ ] GetCollectibleMintTokensMessageComposer
- [ ] GetCollectibleWalletAddressesMessageComposer
- [ ] GetCollectorScoreMessageComposer
- [ ] GetMintTokenOffersMessageComposer
- [ ] GetNftClaimsMessageComposer
- [ ] GetNftCollectionsMessageComposer
- [ ] GetNftStoreOffersMessageComposer
- [ ] GetNftTransferFeeMessageComposer
- [ ] MintItemMessageComposer
- [ ] NftCollectiblesClaimBonusItemMessageComposer
- [ ] NftCollectiblesClaimRewardItemMessageComposer
- [ ] NftStorePurchaseMessageComposer
- [ ] NftTransferAssetsMessageComposer
- [ ] PurchaseMintTokenMessageComposer

### competition (x1)
- [ ] RoomCompetitionInitMessageComposer

### customfilter (x3)
- [ ] AddToCustomFilterMessageComposer
- [ ] GetCustomFilterMessageComposer
- [ ] RemoveFromCustomFilterMessageComposer

### engine (x1)
- [ ] ClickFurniMessageComposer

### friendlist (x4)
- [ ] FriendListUpdateMessageComposer
- [ ] GetFriendRequestsMessageComposer
- [ ] GetMessengerHistoryComposer
- [ ] MessengerInitMessageComposer

### furniture (x3)
- [ ] RoomDimmerChangeStateMessageComposer
- [ ] RoomDimmerGetPresetsMessageComposer
- [ ] SetAreaHideDataComposer

### help (x1)
- [ ] AppealCfhMessageComposer

### landingview (x1)
- [ ] GetPromoArticlesMessageComposer

### layout (x2)
- [ ] GetOccupiedTilesMessageComposer
- [ ] GetRoomEntryTileMessageComposer

### marketplace (x2)
- [ ] BuyMarketplaceTokensMessageComposer
- [ ] GetMarketplaceCanMakeOfferMessageComposer

### navigator (x2)
- [ ] CanCreateRoomMessageComposer
- [ ] GetUserEventCatsMessageComposer

### nft (x8)
- [ ] AddNftToTradeComposer
- [ ] GetNftCreditsMessageComposer
- [ ] GetNftTradeInventoryComposer
- [ ] GetSelectedNftWardrobeOutfitMessageComposer
- [ ] GetSilverMessageComposer
- [ ] GetUserNftWardrobeMessageComposer
- [ ] RemoveNftFromTradeComposer
- [ ] SaveUserNftWardrobeMessageComposer

### nux (x1)
- [ ] SelectInitialRoomComposer

### quest (x7)
- [ ] ClaimDailyTaskComposer
- [ ] GetCommunityGoalProgressMessageComposer
- [ ] GetDailyTasksComposer
- [ ] GetQuestsMessageComposer
- [ ] GetSeasonalQuestsOnlyMessageComposer
- [ ] OpenQuestTrackerMessageComposer
- [ ] RejectQuestMessageComposer

### session (x1)
- [ ] QuitMessageComposer

### talent (x1)
- [ ] GuideAdvertisementReadMessageComposer

### trading (x1)
- [ ] SilverFeeMessageComposer

### treasurehunt (x1)
- [ ] ProgressTreasureHuntMessageComposer

### userdefinedroomevents (x4)
- [ ] UpdateAddonMessageComposer
- [ ] UpdateSelectorMessageComposer
- [ ] UpdateVariableMessageComposer
- [ ] WiredClickUserMessageComposer

### users (x5)
- [ ] BlockListInitComposer
- [ ] BlockUserMessageComposer
- [ ] GetUserNftChatStylesMessageComposer
- [ ] ReplenishRespectMessageComposer
- [ ] UnblockUserMessageComposer

### variablesmanagement (x3)
- [ ] WiredGetUserPermanentVariablesComposer
- [ ] WiredGetVariableOwnersPageComposer
- [ ] WiredSetUserPermanentVariableComposer

### wiredmenu (x13)
- [ ] WiredClearErrorLogsMessageComposer
- [ ] WiredGetAllVariableHoldersMessageComposer
- [ ] WiredGetAllVariablesDiffsMessageComposer
- [ ] WiredGetAllVariablesHashMessageComposer
- [ ] WiredGetErrorLogsMessageComposer
- [ ] WiredGetRoomLogsComposer
- [ ] WiredGetRoomSettingsMessageComposer
- [ ] WiredGetRoomStatsMessageComposer
- [ ] WiredGetVariablesForObjectMessageComposer
- [ ] WiredSetObjectVariableValueMessageComposer
- [ ] WiredSetPreferencesMessageComposer
- [ ] WiredSetRoomSettingsMessageComposer
- [ ] WiredUpdateRoomComposer

## Diffs source -> emu: missing incoming
- Total: 489
### achievements (x1)
- [ ] GetAchievementsMessage

### action (x11)
- [ ] AmbassadorAlertMessage
- [ ] AssignRightsMessage
- [ ] BanUserWithDurationMessage
- [ ] KickUserMessage
- [ ] LetUserInMessage
- [ ] MuteAllInRoomMessage
- [ ] MuteUserMessage
- [ ] RemoveAllRightsMessage
- [ ] RemoveRightsMessage
- [ ] UnbanUserFromRoomMessage
- [ ] UnmuteUserMessage

### advertisement (x2)
- [ ] GetInterstitialMessage
- [ ] InterstitialShownMessage

### arena (x4)
- [ ] Game2ExitGameMessage
- [ ] Game2GameChatMessage
- [ ] Game2LoadStageReadyMessage
- [ ] Game2PlayAgainMessage

### avatar (x14)
- [ ] AvatarExpressionMessage
- [ ] ChangeMottoMessage
- [ ] ChangePostureMessage
- [ ] ChangeUserNameInRoomMessage
- [ ] ChangeUserNameMessage
- [ ] CheckUserNameMessage
- [ ] CustomizeAvatarWithFurniMessage
- [ ] DropCarryItemMessage
- [ ] GetWardrobeMessage
- [ ] LookToMessage
- [ ] PassCarryItemMessage
- [ ] PassCarryItemToPetMessage
- [ ] SaveWardrobeOutfitMessage
- [ ] SignMessage

### badges (x5)
- [ ] GetBadgePointLimitsMessage
- [ ] GetBadgesMessage
- [ ] GetIsBadgeRequestFulfilledMessage
- [ ] RequestABadgeMessage
- [ ] SetActivatedBadgesMessage

### bots (x3)
- [ ] CommandBotMessage
- [ ] GetBotCommandConfigurationDataMessage
- [ ] GetBotInventoryMessage

### camera (x5)
- [ ] PhotoCompetitionMessage
- [ ] PublishPhotoMessage
- [ ] PurchasePhotoMessage
- [ ] RenderRoomMessage
- [ ] RequestCameraConfigurationMessage

### campaign (x2)
- [ ] OpenCampaignCalendarDoorAsStaffMessage
- [ ] OpenCampaignCalendarDoorMessage

### catalog (x33)
- [ ] BuildersClubPlaceRoomItemMessage
- [ ] BuildersClubPlaceWallItemMessage
- [ ] BuildersClubQueryFurniCountMessage
- [ ] ChargeFireworkMessage
- [ ] GetBonusRareInfoMessage
- [ ] GetBundleDiscountRulesetMessage
- [ ] GetCatalogIndexMessage
- [ ] GetCatalogPageMessage
- [ ] GetCatalogPageWithEarliestExpiryMessage
- [ ] GetClubGiftInfoMessage
- [ ] GetClubOffersMessage
- [ ] GetGiftWrappingConfigurationMessage
- [ ] GetHabboClubExtendOfferMessage
- [ ] GetIsOfferGiftableMessage
- [ ] GetLimitedOfferAppearingNextMessage
- [ ] GetNextTargetedOfferMessage
- [ ] GetProductOfferMessage
- [ ] GetRoomAdPurchaseInfoMessage
- [ ] GetSeasonalCalendarDailyOfferMessage
- [ ] GetSellablePetPalettesMessage
- [ ] GetTargetedOfferMessage
- [ ] MarkCatalogNewAdditionsPageOpenedMessage
- [ ] PurchaseBasicMembershipExtensionMessage
- [ ] PurchaseFromCatalogAsGiftMessage
- [ ] PurchaseFromCatalogMessage
- [ ] PurchaseRoomAdMessageMessage
- [ ] PurchaseTargetedOfferMessage
- [ ] PurchaseVipMembershipExtensionMessage
- [ ] RedeemVoucherMessage
- [ ] RoomAdPurchaseInitiatedMessage
- [ ] SelectClubGiftMessage
- [ ] SetTargetedOfferStateMessage
- [ ] ShopTargetedOfferViewedMessage

### chat (x2)
- [ ] CancelTypingMessage
- [ ] StartTypingMessage

### collectibles (x13)
- [ ] GetCollectibleMintableItemTypesMessage
- [ ] GetCollectibleMintingEnabledMessage
- [ ] GetCollectibleMintTokensMessage
- [ ] GetCollectibleWalletAddressesMessage
- [ ] GetCollectorScoreMessage
- [ ] GetMintTokenOffersMessage
- [ ] GetNftCollectionsMessage
- [ ] GetNftTransferFeeMessage
- [ ] MintItemMessage
- [ ] NftCollectiblesClaimBonusItemMessage
- [ ] NftCollectiblesClaimRewardItemMessage
- [ ] NftTransferAssetsMessage
- [ ] PurchaseMintTokenMessage

### competition (x9)
- [ ] ForwardToACompetitionRoomMessage
- [ ] ForwardToASubmittableRoomMessage
- [ ] ForwardToRandomCompetitionRoomMessage
- [ ] GetCurrentTimingCodeMessage
- [ ] GetIsUserPartOfCompetitionMessage
- [ ] GetSecondsUntilMessage
- [ ] RoomCompetitionInitMessage
- [ ] SubmitRoomToCompetitionMessage
- [ ] VoteForRoomMessage

### crafting (x5)
- [ ] CraftMessage
- [ ] CraftSecretMessage
- [ ] GetCraftableProductsMessage
- [ ] GetCraftingRecipeMessage
- [ ] GetCraftingRecipesAvailableMessage

### directory (x5)
- [ ] Game2CheckGameDirectoryStatusMessage
- [ ] Game2GetAccountGameStatusMessage
- [ ] Game2LeaveGameMessage
- [ ] Game2QuickJoinGameMessage
- [ ] Game2StartSnowWarMessage

### engine (x27)
- [ ] ClickCharacterMessage
- [ ] ClickFurniMessage
- [ ] GetFurnitureAliasesMessage
- [ ] GetItemDataMessage
- [ ] GetPetCommandsMessage
- [ ] GetRoomEntryDataMessage
- [ ] GiveSupplementToPetMessage
- [ ] MountPetMessage
- [ ] MoveAvatarMessage
- [ ] MoveObjectMessage
- [ ] MovePetMessage
- [ ] MoveWallItemMessage
- [ ] PickupObjectMessage
- [ ] PlaceBotMessage
- [ ] PlaceObjectMessage
- [ ] PlacePetMessage
- [ ] RemoveBotFromFlatMessage
- [ ] RemoveItemMessage
- [ ] RemovePetFromFlatMessage
- [ ] RemoveSaddleFromPetMessage
- [ ] SetClothingChangeDataMessage
- [ ] SetItemDataMessage
- [ ] SetObjectDataMessage
- [ ] TogglePetBreedingPermissionMessage
- [ ] TogglePetRidingPermissionMessage
- [ ] UseFurnitureMessage
- [ ] UseWallItemMessage

### friendfurni (x1)
- [ ] FriendFurniConfirmLockMessage

### friendlist (x15)
- [ ] AcceptFriendMessage
- [ ] DeclineFriendMessage
- [ ] FindNewFriendsMessage
- [ ] FollowFriendMessage
- [ ] FriendListUpdateMessage
- [ ] GetFriendRequestsMessage
- [ ] GetMessengerHistoryMessage
- [ ] HabboSearchMessage
- [ ] MessengerInitMessage
- [ ] RemoveFriendMessage
- [ ] RequestFriendMessage
- [ ] SendMsgMessage
- [ ] SendRoomInviteMessage
- [ ] SetRelationshipStatusMessage
- [ ] VisitUserMessage

### furni (x3)
- [ ] RequestFurniInventoryMessage
- [ ] RequestFurniInventoryWhenNotInRoomMessage
- [ ] RequestRoomPropertySetMessage

### furniture (x28)
- [ ] AddSpamWallPostItMessage
- [ ] ControlYoutubeDisplayPlaybackMessage
- [ ] CreditFurniRedeemMessage
- [ ] DiceOffMessage
- [ ] EnterOneWayDoorMessage
- [ ] ExtendRentOrBuyoutFurniMessage
- [ ] ExtendRentOrBuyoutStripItemMessage
- [ ] GetGuildFurniContextMenuInfoMessage
- [ ] GetRentOrBuyoutOfferMessage
- [ ] GetYoutubeDisplayStatusMessage
- [ ] OpenMysteryTrophyMessage
- [ ] OpenPetPackageMessage
- [ ] PlacePostItMessage
- [ ] PresentOpenMessage
- [ ] RentableSpaceCancelRentMessage
- [ ] RentableSpaceRentMessage
- [ ] RoomDimmerChangeStateMessage
- [ ] RoomDimmerGetPresetsMessage
- [ ] RoomDimmerSavePresetMessage
- [ ] SetAreaHideDataMessage
- [ ] SetCustomStackingHeightMessage
- [ ] SetMannequinFigureMessage
- [ ] SetMannequinNameMessage
- [ ] SetRandomStateMessage
- [ ] SetRoomBackgroundColorDataMessage
- [ ] SetYoutubeDisplayPlaylistMessage
- [ ] SpinWheelOfFortuneMessage
- [ ] ThrowDiceMessage

### gifts (x5)
- [ ] class_200Message
- [ ] ResetPhoneNumberStateMessage
- [ ] SetPhoneNumberVerificationStatusMessage
- [ ] TryPhoneNumberMessage
- [ ] VerifyCodeMessage

### groupforums (x10)
- [ ] GetForumsListMessage
- [ ] GetForumStatsMessage
- [ ] GetMessagesMessage
- [ ] GetThreadMessage
- [ ] GetThreadsMessage
- [ ] GetUnreadForumsCountMessage
- [ ] ModerateMessageMessage
- [ ] ModerateThreadMessage
- [ ] UpdateForumReadMarkerMessage
- [ ] UpdateForumSettingsMessage

### handshake (x9)
- [ ] ClientHelloMessage
- [ ] CompleteDiffieHandshakeMessage
- [ ] DisconnectMessage
- [ ] InfoRetrieveMessage
- [ ] InitDiffieHandshakeMessage
- [ ] PongMessage
- [ ] SSOTicketMessage
- [ ] UniqueIdMessage
- [ ] VersionCheckMessage

### help (x26)
- [ ] CallForHelpFromForumMessageMessage
- [ ] CallForHelpFromForumThreadMessage
- [ ] CallForHelpFromIMMessage
- [ ] CallForHelpFromPhotoMessage
- [ ] CallForHelpFromSelfieMessage
- [ ] CallForHelpMessage
- [ ] ChatReviewGuideDecidesOnOfferMessage
- [ ] ChatReviewGuideDetachedMessage
- [ ] ChatReviewGuideVoteMessage
- [ ] ChatReviewSessionCreateMessage
- [ ] DeletePendingCallsForHelpMessage
- [ ] GetCfhStatusMessage
- [ ] GetGuideReportingStatusMessage
- [ ] GetPendingCallsForHelpMessage
- [ ] GetQuizQuestionsMessage
- [ ] GuideSessionCreateMessage
- [ ] GuideSessionFeedbackMessage
- [ ] GuideSessionGetRequesterRoomMessage
- [ ] GuideSessionGuideDecidesMessage
- [ ] GuideSessionInviteRequesterMessage
- [ ] GuideSessionIsTypingMessage
- [ ] GuideSessionOnDutyUpdateMessage
- [ ] GuideSessionReportMessage
- [ ] GuideSessionRequesterCancelsMessage
- [ ] GuideSessionResolvedMessage
- [ ] PostQuizAnswersMessage

### hotlooks (x1)
- [ ] GetHotLooksMessage

### ingame (x5)
- [ ] Game2MakeSnowballMessage
- [ ] Game2RequestFullStatusUpdateMessage
- [ ] Game2SetUserMoveTargetMessage
- [ ] Game2ThrowSnowballAtHumanMessage
- [ ] Game2ThrowSnowballAtPositionMessage

### landingview (x1)
- [ ] GetPromoArticlesMessage

### layout (x3)
- [ ] GetOccupiedTilesMessage
- [ ] GetRoomEntryTileMessage
- [ ] UpdateFloorPropertiesMessage

### lobby (x3)
- [ ] class_165Message
- [ ] GetResolutionAchievementsMessage
- [ ] GetUserGameAchievementsMessage

### marketplace (x10)
- [ ] BuyMarketplaceOfferMessage
- [ ] BuyMarketplaceTokensMessage
- [ ] CancelMarketplaceOfferMessage
- [ ] GetMarketplaceCanMakeOfferMessage
- [ ] GetMarketplaceConfigurationMessage
- [ ] GetMarketplaceItemStatsMessage
- [ ] GetMarketplaceOffersMessage
- [ ] GetMarketplaceOwnOffersMessage
- [ ] MakeOfferMessage
- [ ] RedeemMarketplaceOfferCreditsMessage

### moderator (x21)
- [ ] CloseIssueDefaultActionMessage
- [ ] CloseIssuesMessage
- [ ] DefaultSanctionMessage
- [ ] GetCfhChatlogMessage
- [ ] GetModeratorRoomInfoMessage
- [ ] GetModeratorUserInfoMessage
- [ ] GetRoomChatlogMessage
- [ ] GetRoomVisitsMessage
- [ ] GetUserChatlogMessage
- [ ] ModAlertMessage
- [ ] ModBanMessage
- [ ] ModerateRoomMessage
- [ ] ModeratorActionMessage
- [ ] ModKickMessage
- [ ] ModMessageMessage
- [ ] ModMuteMessage
- [ ] ModToolPreferencesMessage
- [ ] ModToolSanctionMessage
- [ ] ModTradingLockMessage
- [ ] PickIssuesMessage
- [ ] ReleaseIssuesMessage

### mysterybox (x1)
- [ ] MysteryBoxWaitingCanceledMessage

### navigator (x36)
- [ ] AddFavouriteRoomMessage
- [ ] CancelEventMessage
- [ ] CanCreateRoomMessage
- [ ] CompetitionRoomsSearchMessage
- [ ] ConvertGlobalRoomIdMessage
- [ ] CreateFlatMessage
- [ ] DeleteFavouriteRoomMessage
- [ ] EditEventMessage
- [ ] ForwardToARandomPromotedRoomMessage
- [ ] ForwardToSomeRoomMessage
- [ ] GetGuestRoomMessage
- [ ] GetOfficialRoomsMessage
- [ ] GetPopularRoomTagsMessage
- [ ] GetUserEventCatsMessage
- [ ] GetUserFlatCatsMessage
- [ ] GuildBaseSearchMessage
- [ ] MyFavouriteRoomsSearchMessage
- [ ] MyFrequentRoomHistorySearchMessage
- [ ] MyFriendsRoomsSearchMessage
- [ ] MyGuildBasesSearchMessage
- [ ] MyRecommendedRoomsMessage
- [ ] MyRoomHistorySearchMessage
- [ ] MyRoomRightsSearchMessage
- [ ] MyRoomsSearchMessage
- [ ] PopularRoomsSearchMessage
- [ ] RateFlatMessage
- [ ] RemoveOwnRoomRightsRoomMessage
- [ ] RoomAdEventTabAdClickedMessage
- [ ] RoomAdEventTabViewedMessage
- [ ] RoomAdSearchMessage
- [ ] RoomsWhereMyFriendsAreSearchMessage
- [ ] RoomsWithHighestScoreSearchMessage
- [ ] RoomTextSearchMessage
- [ ] SetRoomSessionTagsMessage
- [ ] ToggleStaffPickMessage
- [ ] UpdateHomeRoomMessage

### newnavigator (x7)
- [ ] NavigatorAddCollapsedCategoryMessage
- [ ] NavigatorAddSavedSearchMessage
- [ ] NavigatorDeleteSavedSearchMessage
- [ ] NavigatorRemoveCollapsedCategoryMessage
- [ ] NavigatorSetSearchCodeViewModeMessage
- [ ] NewNavigatorInitMessage
- [ ] NewNavigatorSearchMessage

### nft (x5)
- [ ] GetNftCreditsMessage
- [ ] GetSelectedNftWardrobeOutfitMessage
- [ ] GetSilverMessage
- [ ] GetUserNftWardrobeMessage
- [ ] SaveUserNftWardrobeMessage

### notifications (x2)
- [ ] ResetUnseenItemIdsMessage
- [ ] ResetUnseenItemsMessage

### nux (x3)
- [ ] NewUserExperienceGetGiftsMessage
- [ ] NewUserExperienceScriptProceedMessage
- [ ] SelectInitialRoomMessage

### pets (x8)
- [ ] BreedPetsMessage
- [ ] CancelPetBreedingMessage
- [ ] ConfirmPetBreedingMessage
- [ ] CustomizePetWithFurniMessage
- [ ] GetPetInfoMessage
- [ ] GetPetInventoryMessage
- [ ] PetSelectedMessage
- [ ] RespectPetMessage

### poll (x3)
- [ ] PollAnswerMessage
- [ ] PollRejectMessage
- [ ] PollStartMessage

### preferences (x7)
- [ ] SetChatPreferencesMessage
- [ ] SetChatStylePreferenceMessage
- [ ] SetIgnoreRoomInvitesMessage
- [ ] SetNewNavigatorWindowPreferencesMessage
- [ ] SetRoomCameraPreferencesMessage
- [ ] SetSoundSettingsMessage
- [ ] SetUIFlagsMessage

### purse (x1)
- [ ] GetCreditsInfoMessage

### quest (x17)
- [ ] AcceptQuestMessage
- [ ] ActivateQuestMessage
- [ ] CancelQuestMessage
- [ ] class_493Message
- [ ] class_735Message
- [ ] FriendRequestQuestCompleteMessage
- [ ] GetCommunityGoalHallOfFameMessage
- [ ] GetCommunityGoalProgressMessage
- [ ] GetConcurrentUsersGoalProgressMessage
- [ ] GetConcurrentUsersRewardMessage
- [ ] GetDailyQuestMessage
- [ ] GetDailyTasksMessage
- [ ] GetQuestsMessage
- [ ] GetSeasonalQuestsOnlyMessage
- [ ] OpenQuestTrackerMessage
- [ ] RejectQuestMessage
- [ ] StartCampaignMessage

### register (x1)
- [ ] UpdateFigureDataMessage

### roomdirectory (x1)
- [ ] RoomNetworkOpenConnectionMessage

### roomsettings (x8)
- [ ] DeleteRoomMessage
- [ ] GetBannedUsersFromRoomMessage
- [ ] GetCustomRoomFilterMessage
- [ ] GetFlatControllersMessage
- [ ] GetRoomSettingsMessage
- [ ] SaveRoomSettingsMessage
- [ ] UpdateRoomCategoryAndTradeSettingsMessage
- [ ] UpdateRoomFilterMessage

### score (x10)
- [ ] Game2GetFriendsLeaderboardMessage
- [ ] Game2GetTotalGroupLeaderboardMessage
- [ ] Game2GetTotalLeaderboardMessage
- [ ] Game2GetWeeklyFriendsLeaderboardMessage
- [ ] Game2GetWeeklyGroupLeaderboardMessage
- [ ] Game2GetWeeklyLeaderboardMessage
- [ ] GetFriendsWeeklyCompetitiveLeaderboardMessage
- [ ] GetWeeklyCompetitiveLeaderboardMessage
- [ ] GetWeeklyGameRewardMessage
- [ ] GetWeeklyGameRewardWinnersMessage

### session (x3)
- [ ] ChangeQueueMessage
- [ ] OpenFlatConnectionMessage
- [ ] QuitMessage

### sound (x9)
- [ ] AddJukeboxDiskMessage
- [ ] GetJukeboxPlayListMessage
- [ ] GetNowPlayingMessage
- [ ] GetOfficialSongIdMessage
- [ ] GetSongInfoMessage
- [ ] GetSoundMachinePlayListMessage
- [ ] GetSoundSettingsMessage
- [ ] GetUserSongDisksMessage
- [ ] RemoveJukeboxDiskMessage

### talent (x3)
- [ ] GetTalentTrackLevelMessage
- [ ] GetTalentTrackMessage
- [ ] GuideAdvertisementReadMessage

### tracking (x5)
- [ ] EventLogMessage
- [ ] LagWarningReportMessage
- [ ] LatencyPingReportMessage
- [ ] LatencyPingRequestMessage
- [ ] PerformanceLogMessage

### trading (x10)
- [ ] AcceptTradingMessage
- [ ] AddItemsToTradeMessage
- [ ] AddItemToTradeMessage
- [ ] CloseTradingMessage
- [ ] ConfirmAcceptTradingMessage
- [ ] ConfirmDeclineTradingMessage
- [ ] OpenTradingMessage
- [ ] RemoveItemFromTradeMessage
- [ ] SilverFeeMessage
- [ ] UnacceptTradingMessage

### userclassification (x2)
- [ ] PeerUsersClassificationMessage
- [ ] RoomUsersClassificationMessage

### userdefinedroomevents (x9)
- [ ] ApplySnapshotMessage
- [ ] OpenMessage
- [ ] UpdateActionMessage
- [ ] UpdateAddonMessage
- [ ] UpdateConditionMessage
- [ ] UpdateSelectorMessage
- [ ] UpdateTriggerMessage
- [ ] UpdateVariableMessage
- [ ] UpdateWiredMessage

### users (x41)
- [ ] AddAdminRightsToMemberMessage
- [ ] ApproveAllMembershipRequestsMessage
- [ ] ApproveMembershipRequestMessage
- [ ] BlockListInitMessage
- [ ] BlockUserMessage
- [ ] ChangeEmailMessage
- [ ] CreateGuildMessage
- [ ] DeactivateGuildMessage
- [ ] DeselectFavouriteHabboGroupMessage
- [ ] GetEmailStatusMessage
- [ ] GetExtendedProfileByNameMessage
- [ ] GetExtendedProfileMessage
- [ ] GetGuildCreationInfoMessage
- [ ] GetGuildEditInfoMessage
- [ ] GetGuildEditorDataMessage
- [ ] GetGuildMembershipsMessage
- [ ] GetGuildMembersMessage
- [ ] GetHabboGroupBadgesMessage
- [ ] GetHabboGroupDetailsMessage
- [ ] GetIgnoredUsersMessage
- [ ] GetMemberGuildItemCountMessage
- [ ] GetMOTDMessage
- [ ] GetRelationshipStatusInfoMessage
- [ ] GetSelectedBadgesMessage
- [ ] GetUserNftChatStylesMessage
- [ ] GiveStarGemToUserMessage
- [ ] IgnoreUserMessage
- [ ] JoinHabboGroupMessage
- [ ] KickMemberMessage
- [ ] RejectMembershipRequestMessage
- [ ] RemoveAdminRightsFromMemberMessage
- [ ] ScrGetKickbackInfoMessage
- [ ] ScrGetUserInfoMessage
- [ ] SelectFavouriteHabboGroupMessage
- [ ] UnblockGroupMemberMessage
- [ ] UnblockUserMessage
- [ ] UnignoreUserMessage
- [ ] UpdateGuildBadgeMessage
- [ ] UpdateGuildColorsMessage
- [ ] UpdateGuildIdentityMessage
- [ ] UpdateGuildSettingsMessage

### vault (x3)
- [ ] CreditVaultStatusMessage
- [ ] IncomeRewardClaimMessage
- [ ] WithdrawCreditVaultMessage

### votes (x1)
- [ ] CommunityGoalVoteMessage

### wiredmenu (x11)
- [ ] WiredClearErrorLogsMessage
- [ ] WiredGetAllVariableHoldersMessage
- [ ] WiredGetAllVariablesDiffsMessage
- [ ] WiredGetAllVariablesHashMessage
- [ ] WiredGetErrorLogsMessage
- [ ] WiredGetRoomSettingsMessage
- [ ] WiredGetRoomStatsMessage
- [ ] WiredGetVariablesForObjectMessage
- [ ] WiredSetObjectVariableValueMessage
- [ ] WiredSetPreferencesMessage
- [ ] WiredSetRoomSettingsMessage

## Diffs source -> emu: missing outgoing
- Total: 491
### achievements (x3)
- [ ] AchievementEventMessageComposer
- [ ] AchievementsEventMessageComposer
- [ ] AchievementsScoreEventMessageComposer

### action (x5)
- [ ] AvatarEffectMessageComposer
- [ ] CarryObjectMessageComposer
- [ ] ExpressionMessageComposer
- [ ] SleepMessageComposer
- [ ] UseObjectMessageComposer

### advertisement (x2)
- [ ] InterstitialMessageComposer
- [ ] RoomAdErrorEventMessageComposer

### arena (x13)
- [ ] Game2ArenaEnteredMessageComposer
- [ ] Game2EnterArenaFailedMessageComposer
- [ ] Game2EnterArenaMessageComposer
- [ ] Game2GameChatFromPlayerMessageComposer
- [ ] Game2GameEndingMessageComposer
- [ ] Game2GameRejoinMessageComposer
- [ ] Game2PlayerExitedGameArenaMessageComposer
- [ ] Game2PlayerRematchesMessageComposer
- [ ] Game2StageEndingMessageComposer
- [ ] Game2StageLoadMessageComposer
- [ ] Game2StageRunningMessageComposer
- [ ] Game2StageStartingMessageComposer
- [ ] Game2StageStillLoadingMessageComposer

### availability (x5)
- [ ] AvailabilityStatusMessageComposer
- [ ] InfoHotelClosedMessageComposer
- [ ] InfoHotelClosingMessageComposer
- [ ] LoginFailedHotelClosedMessageComposer
- [ ] MaintenanceStatusMessageComposer

### avatar (x4)
- [ ] ChangeUserNameResultMessageComposer
- [ ] CheckUserNameResultMessageComposer
- [ ] FigureUpdateEventMessageComposer
- [ ] WardrobeMessageComposer

### avatareffect (x3)
- [ ] AvatarEffectAddedMessageComposer
- [ ] AvatarEffectExpiredMessageComposer
- [ ] AvatarEffectsMessageComposer

### badges (x4)
- [ ] BadgePointLimitsEventMessageComposer
- [ ] BadgeReceivedEventMessageComposer
- [ ] BadgesEventMessageComposer
- [ ] IsBadgeRequestFulfilledEventMessageComposer

### bots (x8)
- [ ] BotAddedToInventoryEventMessageComposer
- [ ] BotCommandConfigurationMessageComposer
- [ ] BotErrorMessageComposer
- [ ] BotForceOpenContextMenuMessageComposer
- [ ] BotInventoryEventMessageComposer
- [ ] BotRemovedFromInventoryEventMessageComposer
- [ ] BotSkillListUpdateMessageComposer
- [ ] class_1600MessageComposer

### callforhelp (x3)
- [ ] CfhSanctionMessageComposer
- [ ] CfhTopicsInitMessageComposer
- [ ] SanctionStatusEventMessageComposer

### camera (x7)
- [ ] CameraPublishStatusMessageComposer
- [ ] CameraPurchaseOKMessageComposer
- [ ] CameraStorageUrlMessageComposer
- [ ] class_1476MessageComposer
- [ ] CompetitionStatusMessageComposer
- [ ] InitCameraMessageComposer
- [ ] ThumbnailStatusMessageComposer

### campaign (x2)
- [ ] CampaignCalendarDataMessageComposer
- [ ] CampaignCalendarDoorOpenedMessageComposer

### catalog (x29)
- [ ] BonusRareInfoMessageComposer
- [ ] BuildersClubSubscriptionStatusMessageComposer
- [ ] BundleDiscountRulesetMessageComposer
- [ ] CatalogIndexMessageComposer
- [ ] CatalogPageMessageComposer
- [ ] CatalogPageWithEarliestExpiryMessageComposer
- [ ] CatalogPublishedMessageComposer
- [ ] ClubGiftInfoEventMessageComposer
- [ ] ClubGiftSelectedEventMessageComposer
- [ ] FigureSetIdsMessage
- [ ] GiftReceiverNotFoundEventMessageComposer
- [ ] GiftWrappingConfigurationEventMessageComposer
- [ ] HabboClubExtendOfferMessageComposer
- [ ] HabboClubOffersMessageComposer
- [ ] LimitedEditionSoldOutEventMessageComposer
- [ ] LimitedOfferAppearingNextMessageComposer
- [ ] NotEnoughBalanceMessageComposer
- [ ] ProductOfferEventMessageComposer
- [ ] PurchaseErrorMessageComposer
- [ ] PurchaseNotAllowedMessageComposer
- [ ] PurchaseOKMessageComposer
- [ ] RoomAdPurchaseInfoEventMessageComposer
- [ ] SeasonalCalendarDailyOfferMessageComposer
- [ ] SellablePetPalettesMessageComposer
- [ ] SnowWarGameTokensMessageMessageComposer
- [ ] TargetedOfferEventMessageComposer
- [ ] TargetedOfferNotFoundEventMessageComposer
- [ ] VoucherRedeemErrorMessageComposer
- [ ] VoucherRedeemOkMessageComposer

### chat (x5)
- [ ] FloodControlMessageComposer
- [ ] RemainingMutePeriodMessageComposer
- [ ] RoomChatSettingsMessageComposer
- [ ] RoomFilterSettingsMessageComposer
- [ ] UserTypingMessageComposer

### clothing (x3)
- [ ] class_1487MessageComposer
- [ ] class_1594MessageComposer
- [ ] FigureSetIdsEventMessageComposer

### collectibles (x14)
- [ ] CollectableMintableItemTypesMessageComposer
- [ ] CollectibleMintableItemResultMessageComposer
- [ ] CollectibleMintingEnabledMessageComposer
- [ ] CollectibleMintTokenCountMessageComposer
- [ ] CollectibleMintTokenOffersMessageComposer
- [ ] CollectibleWalletAddressesMessageComposer
- [ ] EmeraldBalanceMessageComposer
- [ ] NftBonusItemClaimResultMessageComposer
- [ ] NftCollectionsMessageComposer
- [ ] NftCollectionsScoreMessageComposer
- [ ] NftRewardItemClaimResultMessageComposer
- [ ] NftTransferAssetsResultMessageComposer
- [ ] NftTransferFeeMessageComposer
- [ ] SilverBalanceMessageComposer

### competition (x6)
- [ ] CompetitionEntrySubmitResultMessageComposer
- [ ] CompetitionVotingInfoMessageComposer
- [ ] CurrentTimingCodeMessageComposer
- [ ] IsUserPartOfCompetitionMessageComposer
- [ ] NoOwnedRoomsAlertMessageComposer
- [ ] SecondsUntilMessageComposer

### crafting (x4)
- [ ] CraftableProductsMessageComposer
- [ ] CraftingRecipeMessageComposer
- [ ] CraftingRecipesAvailableMessageComposer
- [ ] CraftingResultMessageComposer

### directory (x15)
- [ ] Game2AccountGameStatusMessageMessageComposer
- [ ] Game2GameCancelledMessageMessageComposer
- [ ] Game2GameCreatedMessageComposer
- [ ] Game2GameDirectoryStatusMessageMessageComposer
- [ ] Game2GameLongDataMessageComposer
- [ ] Game2GameNotFoundMessageMessageComposer
- [ ] Game2GameStartedMessageComposer
- [ ] Game2InArenaQueueMessageMessageComposer
- [ ] Game2JoiningGameFailedMessageMessageComposer
- [ ] Game2StartCounterMessageMessageComposer
- [ ] Game2StartingGameFailedMessageMessageComposer
- [ ] Game2StopCounterMessageMessageComposer
- [ ] Game2UserBlockedMessageMessageComposer
- [ ] Game2UserJoinedGameMessageComposer
- [ ] Game2UserLeftGameMessageMessageComposer

### engine (x31)
- [ ] BuildersClubPlacementWarningMessageComposer
- [ ] FavoriteMembershipUpdateMessageComposer
- [ ] FloorHeightMapMessageComposer
- [ ] FurnitureAliasesMessageComposer
- [ ] HeightMapMessageComposer
- [ ] HeightMapUpdateMessageComposer
- [ ] ItemAddMessageComposer
- [ ] ItemDataUpdateMessageComposer
- [ ] ItemRemoveMessageComposer
- [ ] ItemsMessageComposer
- [ ] ItemsStateUpdateMessageComposer
- [ ] ItemStateUpdateMessageComposer
- [ ] ItemUpdateMessageComposer
- [ ] ObjectAddMessageComposer
- [ ] ObjectDataUpdateMessageComposer
- [ ] ObjectRemoveConfirmMessageComposer
- [ ] ObjectRemoveMessageComposer
- [ ] ObjectRemoveMultipleMessageComposer
- [ ] ObjectsDataUpdateMessageComposer
- [ ] ObjectsMessageComposer
- [ ] ObjectUpdateMessageComposer
- [ ] RoomEntryInfoMessageComposer
- [ ] RoomPropertyMessageComposer
- [ ] RoomVisualizationSettingsMessageComposer
- [ ] SlideObjectBundleMessageComposer
- [ ] SpecialRoomEffectMessageComposer
- [ ] UserChangeMessageComposer
- [ ] UserRemoveMessageComposer
- [ ] UsersMessageComposer
- [ ] UserUpdateMessageComposer
- [ ] WiredMovementsMessageComposer

### error (x1)
- [ ] ErrorReportEventMessageComposer

### friendfurni (x3)
- [ ] FriendFurniCancelLockMessageComposer
- [ ] FriendFurniOtherLockConfirmedMessageComposer
- [ ] FriendFurniStartConfirmationMessageComposer

### friendlist (x16)
- [ ] AcceptFriendResultMessageComposer
- [ ] ConsoleMessageHistoryMessageComposer
- [ ] FindFriendsProcessResultMessageComposer
- [ ] FollowFriendFailedMessageComposer
- [ ] FriendListFragmentMessageComposer
- [ ] FriendNotificationMessageComposer
- [ ] FriendRequestsMessageComposer
- [ ] HabboSearchResultMessageComposer
- [ ] InstantMessageErrorMessageComposer
- [ ] MessengerErrorMessageComposer
- [ ] MiniMailNewMessageComposer
- [ ] MiniMailUnreadCountMessageComposer
- [ ] NewConsoleMessageMessageComposer
- [ ] NewFriendRequestMessageComposer
- [ ] RoomInviteErrorMessageComposer
- [ ] RoomInviteMessageComposer

### furni (x5)
- [ ] FurniListAddOrUpdateEventMessageComposer
- [ ] FurniListEventMessageComposer
- [ ] FurniListInvalidateEventMessageComposer
- [ ] FurniListRemoveEventMessageComposer
- [ ] PostItPlacedEventMessageComposer

### furniture (x18)
- [ ] AreaHideMessageComposer
- [ ] CustomStackingHeightUpdateMessageComposer
- [ ] CustomUserNotificationMessageComposer
- [ ] DiceValueMessageComposer
- [ ] FurniRentOrBuyoutOfferMessageComposer
- [ ] GuildFurniContextMenuInfoMessageComposer
- [ ] OneWayDoorStatusMessageComposer
- [ ] OpenPetPackageRequestedMessageComposer
- [ ] OpenPetPackageResultMessageComposer
- [ ] PresentOpenedMessageComposer
- [ ] RentableSpaceRentFailedMessageComposer
- [ ] RentableSpaceRentOkMessageComposer
- [ ] RequestSpamWallPostItMessageComposer
- [ ] RoomDimmerPresetsMessageComposer
- [ ] RoomMessageNotificationMessageComposer
- [ ] YoutubeControlVideoMessageComposer
- [ ] YoutubeDisplayPlaylistsMessageComposer
- [ ] YoutubeDisplayVideoMessageComposer

### gifts (x3)
- [ ] PhoneCollectionStateMessageComposer
- [ ] TryPhoneNumberResultMessageComposer
- [ ] TryVerificationCodeResultMessageComposer

### groupforums (x7)
- [ ] ForumDataMessageComposer
- [ ] ForumsListMessageComposer
- [ ] ForumThreadsMessageComposer
- [ ] PostThreadMessageComposer
- [ ] ThreadMessagesMessageComposer
- [ ] UnreadForumsCountMessageComposer
- [ ] UpdateMessageMessageComposer

### handshake (x10)
- [ ] AuthenticationOKMessage
- [ ] DisconnectReasonEventMessageComposer
- [ ] GenericErrorMessage
- [ ] IdentityAccountsEventMessageComposer
- [ ] IsFirstLoginOfDayMessage
- [ ] NoobnessLevelMessage
- [ ] PingMessage
- [ ] UniqueMachineIdMessage
- [ ] UserObjectMessage
- [ ] UserRightsMessage

### help (x31)
- [ ] CallForHelpDisabledNotifyMessageComposer
- [ ] CallForHelpPendingCallsDeletedMessageComposer
- [ ] CallForHelpPendingCallsMessageComposer
- [ ] CallForHelpReplyMessageComposer
- [ ] CallForHelpResultMessageComposer
- [ ] ChatReviewSessionDetachedMessageComposer
- [ ] ChatReviewSessionOfferedToGuideMessageComposer
- [ ] ChatReviewSessionResultsMessageComposer
- [ ] ChatReviewSessionStartedMessageComposer
- [ ] ChatReviewSessionVotingStatusMessageComposer
- [ ] class_1177MessageComposer
- [ ] class_1263MessageComposer
- [ ] class_1405MessageComposer
- [ ] class_1469MessageComposer
- [ ] class_1561MessageComposer
- [ ] class_1582MessageComposer
- [ ] GuideOnDutyStatusMessageComposer
- [ ] GuideReportingStatusMessageComposer
- [ ] GuideSessionAttachedMessageComposer
- [ ] GuideSessionDetachedMessageComposer
- [ ] GuideSessionEndedMessageComposer
- [ ] GuideSessionErrorMessageComposer
- [ ] GuideSessionInvitedToGuideRoomMessageComposer
- [ ] GuideSessionPartnerIsTypingMessageComposer
- [ ] GuideSessionRequesterRoomMessageComposer
- [ ] GuideSessionStartedMessageComposer
- [ ] GuideTicketCreationResultMessageComposer
- [ ] GuideTicketResolutionMessageComposer
- [ ] IssueCloseNotificationMessageComposer
- [ ] QuizDataMessageComposer
- [ ] QuizResultsMessageComposer

### hotlooks (x1)
- [ ] HotLooksMessageComposer

### ingame (x2)
- [ ] Game2FullGameStatusMessageComposer
- [ ] Game2GameStatusMessageComposer

### landingview (x1)
- [ ] PromoArticlesMessageComposer

### layout (x2)
- [ ] RoomEntryTileMessageComposer
- [ ] RoomOccupiedTilesMessageComposer

### lobby (x4)
- [ ] AchievementResolutionCompletedMessageComposer
- [ ] AchievementResolutionProgressMessageComposer
- [ ] AchievementResolutionsMessageComposer
- [ ] UserGameAchievementsMessageMessageComposer

### marketplace (x8)
- [ ] MarketplaceBuyOfferResultEventMessageComposer
- [ ] MarketplaceCancelOfferResultEventMessageComposer
- [ ] MarketplaceCanMakeOfferResultMessageComposer
- [ ] MarketplaceConfigurationEventMessageComposer
- [ ] MarketplaceItemStatsEventMessageComposer
- [ ] MarketplaceMakeOfferResultMessageComposer
- [ ] MarketPlaceOffersEventMessageComposer
- [ ] MarketPlaceOwnOffersEventMessageComposer

### moderation (x15)
- [ ] CfhChatlogEventMessageComposer
- [ ] IssueDeletedMessageComposer
- [ ] IssueInfoMessageComposer
- [ ] IssuePickFailedMessageComposer
- [ ] ModeratorActionResultMessageComposer
- [ ] ModeratorCautionEventMessageComposer
- [ ] ModeratorInitMessageComposer
- [ ] ModeratorMessageComposer
- [ ] ModeratorRoomInfoEventMessageComposer
- [ ] ModeratorToolPreferencesEventMessageComposer
- [ ] ModeratorUserInfoEventMessageComposer
- [ ] RoomChatlogEventMessageComposer
- [ ] RoomVisitsEventMessageComposer
- [ ] UserBannedMessageComposer
- [ ] UserChatlogEventMessageComposer

### mysterybox (x4)
- [ ] CancelMysteryBoxWaitMessageComposer
- [ ] GotMysteryBoxPrizeMessageComposer
- [ ] MysteryBoxKeysMessageComposer
- [ ] ShowMysteryBoxWaitMessageComposer

### navigator (x19)
- [ ] CategoriesWithVisitorCountMessageComposer
- [ ] CompetitionRoomsDataMessageComposer
- [ ] ConvertedRoomIdMessageComposer
- [ ] DoorbellMessageComposer
- [ ] FavouriteChangedMessageComposer
- [ ] FavouritesMessageComposer
- [ ] FlatAccessDeniedMessageComposer
- [ ] FlatCreatedMessageComposer
- [ ] GetGuestRoomResultMessageComposer
- [ ] GuestRoomSearchResultMessageComposer
- [ ] NavigatorSettingsMessageComposer
- [ ] OfficialRoomsMessageComposer
- [ ] PopularRoomTagsResultMessageComposer
- [ ] RoomEventCancelMessageComposer
- [ ] RoomEventMessageComposer
- [ ] RoomInfoUpdatedMessageComposer
- [ ] RoomRatingMessageComposer
- [ ] UserEventCatsMessageComposer
- [ ] UserFlatCatsMessageComposer

### newnavigator (x6)
- [ ] NavigatorCollapsedCategoriesMessage
- [ ] NavigatorLiftedRoomsMessage
- [ ] NavigatorMetaDataMessage
- [ ] NavigatorSavedSearchesMessage
- [ ] NavigatorSearchResultBlocksMessageComposer
- [ ] NewNavigatorPreferencesMessageComposer

### nft (x3)
- [ ] UserNftChatStylesMessageComposer
- [ ] UserNftWardrobeMessageComposer
- [ ] UserNftWardrobeSelectionMessageComposer

### notifications (x13)
- [ ] ActivityPointsMessageComposer
- [ ] ClubGiftNotificationEventMessageComposer
- [ ] ElementPointerMessageComposer
- [ ] HabboAchievementNotificationMessageComposer
- [ ] HabboActivityPointNotificationMessageComposer
- [ ] HabboBroadcastMessageComposer
- [ ] InfoFeedEnableMessageComposer
- [ ] MOTDNotificationEventMessageComposer
- [ ] NotificationDialogMessageComposer
- [ ] OfferRewardDeliveredMessageComposer
- [ ] PetLevelNotificationEventMessageComposer
- [ ] RestoreClientMessageComposer
- [ ] UnseenItemsEventMessageComposer

### nux (x2)
- [ ] NewUserExperienceGiftOfferEventMessageComposer
- [ ] NewUserExperienceNotCompleteEventMessageComposer

### perk (x2)
- [ ] CitizenshipVipOfferPromoEnabledEventMessageComposer
- [ ] PerkAllowancesMessageComposer

### permissions (x3)
- [ ] YouAreControllerMessageComposer
- [ ] YouAreNotControllerMessageComposer
- [ ] YouAreOwnerMessageComposer

### pets (x18)
- [ ] ConfirmBreedingRequestEventMessageComposer
- [ ] ConfirmBreedingResultEventMessageComposer
- [ ] GoToBreedingNestFailureEventMessageComposer
- [ ] NestBreedingSuccessEventMessageComposer
- [ ] PetAddedToInventoryEventMessageComposer
- [ ] PetBreedingEventMessageComposer
- [ ] PetBreedingResultEventMessageComposer
- [ ] PetCommandsMessageComposer
- [ ] PetExperienceMessageComposer
- [ ] PetFigureUpdateMessageComposer
- [ ] PetInfoMessageComposer
- [ ] PetInventoryEventMessageComposer
- [ ] PetLevelUpdateMessageComposer
- [ ] PetPlacingErrorMessageComposer
- [ ] PetReceivedMessageComposer
- [ ] PetRemovedFromInventoryEventMessageComposer
- [ ] PetRespectFailedMessageComposer
- [ ] PetStatusUpdateMessageComposer

### poll (x6)
- [ ] PollContentsEventMessageComposer
- [ ] PollErrorEventMessageComposer
- [ ] PollOfferEventMessageComposer
- [ ] QuestionAnsweredEventMessageComposer
- [ ] QuestionEventMessageComposer
- [ ] QuestionFinishedEventMessageComposer

### preferences (x1)
- [ ] AccountPreferencesEventMessageComposer

### purse (x1)
- [ ] CreditBalanceEventMessageComposer

### quest (x11)
- [ ] class_1139MessageComposer
- [ ] CommunityGoalHallOfFameMessageComposer
- [ ] CommunityGoalProgressMessageComposer
- [ ] ConcurrentUsersGoalProgressMessageComposer
- [ ] EpicPopupMessageComposer
- [ ] QuestCancelledMessageComposer
- [ ] QuestCompletedMessageComposer
- [ ] QuestDailyMessageComposer
- [ ] QuestMessageComposer
- [ ] QuestsMessageComposer
- [ ] SeasonalQuestsMessageComposer

### roomsettings (x11)
- [ ] BannedUsersFromRoomEventMessageComposer
- [ ] FlatControllerAddedEventMessageComposer
- [ ] FlatControllerRemovedEventMessageComposer
- [ ] FlatControllersEventMessageComposer
- [ ] NoSuchFlatEventMessageComposer
- [ ] RoomSettingsDataEventMessageComposer
- [ ] RoomSettingsErrorEventMessageComposer
- [ ] RoomSettingsSavedEventMessageComposer
- [ ] RoomSettingsSaveErrorEventMessageComposer
- [ ] ShowEnforceRoomCategoryDialogEventMessageComposer
- [ ] UserUnbannedFromRoomEventMessageComposer

### score (x4)
- [ ] Game2GroupLeaderboardMessageComposer
- [ ] Game2LeaderboardMessageComposer
- [ ] WeeklyGameRewardEventMessageComposer
- [ ] WeeklyGameRewardWinnersEventMessageComposer

### session (x12)
- [ ] CantConnectMessageComposer
- [ ] CloseConnectionMessageComposer
- [ ] FlatAccessibleMessageComposer
- [ ] GamePlayerValueMessageComposer
- [ ] HanditemConfigurationMessageComposer
- [ ] OpenConnectionMessageComposer
- [ ] RoomForwardMessageComposer
- [ ] RoomQueueStatusMessageComposer
- [ ] RoomReadyMessageComposer
- [ ] YouAreNotSpectatorMessageComposer
- [ ] YouArePlayingGameMessageComposer
- [ ] YouAreSpectatorMessageComposer

### sound (x8)
- [ ] JukeboxPlayListFullMessageComposer
- [ ] JukeboxSongDisksMessageComposer
- [ ] NowPlayingMessageComposer
- [ ] OfficialSongIdMessageComposer
- [ ] PlayListMessageComposer
- [ ] PlayListSongAddedMessageComposer
- [ ] TraxSongInfoMessageComposer
- [ ] UserSongDisksInventoryMessageComposer

### talent (x3)
- [ ] TalentLevelUpMessageComposer
- [ ] TalentTrackLevelMessageComposer
- [ ] TalentTrackMessageComposer

### tracking (x1)
- [ ] LatencyPingResponseMessage

### trading (x13)
- [ ] class_1547MessageComposer
- [ ] TradeOpenFailedEventPaserMessageComposer
- [ ] TradeSilverFeeMessageComposer
- [ ] TradeSilverSetMessageComposer
- [ ] TradingAcceptEventMessageComposer
- [ ] TradingCloseEventMessageComposer
- [ ] TradingCompletedEventMessageComposer
- [ ] TradingConfirmationEventMessageComposer
- [ ] TradingItemListEventMessageComposer
- [ ] TradingNotOpenEventMessageComposer
- [ ] TradingOpenEventMessageComposer
- [ ] TradingOtherNotAllowedEventMessageComposer
- [ ] TradingYouAreNotAllowedEventMessageComposer

### userclassification (x1)
- [ ] UserClassificationMessageComposer

### userdefinedroomevents (x9)
- [ ] WiredFurniActionEventMessageComposer
- [ ] WiredFurniAddonEventMessageComposer
- [ ] WiredFurniConditionEventMessageComposer
- [ ] WiredFurniSelectorEventMessageComposer
- [ ] WiredFurniTriggerEventMessageComposer
- [ ] WiredFurniVariableEventMessageComposer
- [ ] WiredRewardResultMessageComposer
- [ ] WiredSaveSuccessEventMessageComposer
- [ ] WiredValidationErrorEventMessageComposer

### users (x36)
- [ ] AccountSafetyLockStatusChangeMessageComposer
- [ ] BlockListMessageComposer
- [ ] BlockUserUpdateMessageComposer
- [ ] ChangeEmailResultEventMessageComposer
- [ ] EmailStatusResultEventMessageComposer
- [ ] ExtendedProfileChangedMessageComposer
- [ ] ExtendedProfileMessageComposer
- [ ] GroupDetailsChangedMessageComposer
- [ ] GroupMembershipRequestedMessageComposer
- [ ] GuildCreatedMessageComposer
- [ ] GuildCreationInfoMessageComposer
- [ ] GuildEditFailedMessageComposer
- [ ] GuildEditInfoMessageComposer
- [ ] GuildEditorDataMessageComposer
- [ ] GuildMemberFurniCountInHQMessageComposer
- [ ] GuildMemberMgmtFailedMessageComposer
- [ ] GuildMembershipRejectedMessageComposer
- [ ] GuildMembershipsMessageComposer
- [ ] GuildMembershipUpdatedMessageComposer
- [ ] GuildMembersMessageComposer
- [ ] HabboGroupBadgesMessageComposer
- [ ] HabboGroupDeactivatedMessageComposer
- [ ] HabboGroupDetailsMessageComposer
- [ ] HabboGroupJoinFailedMessageComposer
- [ ] HabboUserBadgesMessageComposer
- [ ] HandItemReceivedMessageComposer
- [ ] IgnoredUsersMessageComposer
- [ ] IgnoreResultMessageComposer
- [ ] InClientLinkMessageComposer
- [ ] PetRespectNotificationEventMessageComposer
- [ ] PetSupplementedNotificationEventMessageComposer
- [ ] RelationshipStatusInfoEventMessageComposer
- [ ] RespectNotificationMessageComposer
- [ ] ScrSendKickbackInfoMessageComposer
- [ ] ScrSendUserInfoMessageComposer
- [ ] UserNameChangedMessageComposer

### vault (x1)
- [ ] IncomeRewardClaimResponseMessageComposer

### votes (x1)
- [ ] CommunityVoteReceivedEventMessageComposer

### wiredmenu (x9)
- [ ] WiredAllVariableHoldersEventMessageComposer
- [ ] WiredAllVariablesDiffsEventMessageComposer
- [ ] WiredAllVariablesHashEventMessageComposer
- [ ] WiredErrorLogsEventMessageComposer
- [ ] WiredMenuErrorEventMessageComposer
- [ ] WiredPermissionsEventMessageComposer
- [ ] WiredRoomSettingsEventMessageComposer
- [ ] WiredRoomStatsEventMessageComposer
- [ ] WiredVariablesForObjectEventMessageComposer

## Revision: headers manquants incoming (Headers.cs)
- Total: 336
### action (x5)
- [ ] AvatarEffectMessageEvent
- [ ] CarryObjectMessageEvent
- [ ] ExpressionMessageEvent
- [ ] SleepMessageEvent
- [ ] UseObjectMessageEvent

### advertisement (x1)
- [ ] InterstitialMessageEvent

### arena (x13)
- [ ] Game2ArenaEnteredMessageEvent
- [ ] Game2EnterArenaFailedMessageEvent
- [ ] Game2EnterArenaMessageEvent
- [ ] Game2GameChatFromPlayerMessageEvent
- [ ] Game2GameEndingMessageEvent
- [ ] Game2GameRejoinMessageEvent
- [ ] Game2PlayerExitedGameArenaMessageEvent
- [ ] Game2PlayerRematchesMessageEvent
- [ ] Game2StageEndingMessageEvent
- [ ] Game2StageLoadMessageEvent
- [ ] Game2StageRunningMessageEvent
- [ ] Game2StageStartingMessageEvent
- [ ] Game2StageStillLoadingMessageEvent

### availability (x5)
- [ ] AvailabilityStatusMessageEvent
- [ ] InfoHotelClosedMessageEvent
- [ ] InfoHotelClosingMessageEvent
- [ ] LoginFailedHotelClosedMessageEvent
- [ ] MaintenanceStatusMessageEvent

### avatar (x3)
- [ ] ChangeUserNameResultMessageEvent
- [ ] CheckUserNameResultMessageEvent
- [ ] WardrobeMessageEvent

### avatareffect (x3)
- [ ] AvatarEffectAddedMessageEvent
- [ ] AvatarEffectExpiredMessageEvent
- [ ] AvatarEffectsMessageEvent

### callforhelp (x3)
- [ ] CfhSanctionMessageEvent
- [ ] CfhTopicsInitMessageEvent
- [ ] MyCfhReportStatusMessageEvent

### camera (x6)
- [ ] CameraPublishStatusMessageEvent
- [ ] CameraPurchaseOKMessageEvent
- [ ] CameraStorageUrlMessageEvent
- [ ] CompetitionStatusMessageEvent
- [ ] InitCameraMessageEvent
- [ ] ThumbnailStatusMessageEvent

### campaign (x2)
- [ ] CampaignCalendarDataMessageEvent
- [ ] CampaignCalendarDoorOpenedMessageEvent

### catalog (x25)
- [ ] BonusRareInfoMessageEvent
- [ ] BuildersClubFurniCountMessageEvent
- [ ] BuildersClubSubscriptionStatusMessageEvent
- [ ] BundleDiscountRulesetMessageEvent
- [ ] CatalogIndexMessageEvent
- [ ] CatalogPageMessageEvent
- [ ] CatalogPageWithEarliestExpiryMessageEvent
- [ ] CatalogPublishedMessageEvent
- [ ] HabboClubExtendOfferMessageEvent
- [ ] HabboClubOffersMessageEvent
- [ ] LimitedOfferAppearingNextMessageEvent
- [ ] LtdRaffleEnteredMessageEvent
- [ ] LtdRaffleResultMessageEvent
- [ ] NotEnoughBalanceMessageEvent
- [ ] PurchaseErrorMessageEvent
- [ ] PurchaseNotAllowedMessageEvent
- [ ] PurchaseOKMessageEvent
- [ ] RecyclerFinishedMessageEvent
- [ ] RecyclerPrizesMessageEvent
- [ ] RecyclerStatusMessageEvent
- [ ] SeasonalCalendarDailyOfferMessageEvent
- [ ] SellablePetPalettesMessageEvent
- [ ] SnowWarGameTokensMessageEvent
- [ ] VoucherRedeemErrorMessageEvent
- [ ] VoucherRedeemOkMessageEvent

### chat (x4)
- [ ] FloodControlMessageEvent
- [ ] RoomChatSettingsMessageEvent
- [ ] RoomFilterSettingsMessageEvent
- [ ] UserTypingMessageEvent

### collectibles (x19)
- [ ] CollectableMintableItemTypesMessageEvent
- [ ] CollectibleMintableItemResultMessageEvent
- [ ] CollectibleMintingEnabledMessageEvent
- [ ] CollectibleMintTokenCountMessageEvent
- [ ] CollectibleMintTokenOffersMessageEvent
- [ ] CollectibleWalletAddressesMessageEvent
- [ ] EmeraldBalanceMessageEvent
- [ ] NftBonusItemClaimResultMessageEvent
- [ ] NftClaimResultMessageEvent
- [ ] NftClaimsMessageEvent
- [ ] NftCollectionsMessageEvent
- [ ] NftCollectionsScoreMessageEvent
- [ ] NftRewardItemClaimResultMessageEvent
- [ ] NftStoreOffersMessageEvent
- [ ] NftTransferAssetsResultMessageEvent
- [ ] NftTransferFeeMessageEvent
- [ ] RedeemNftLootBoxResultMessageEvent
- [ ] RedeemNftLootBoxStateMessageEvent
- [ ] SilverBalanceMessageEvent

### competition (x6)
- [ ] CompetitionEntrySubmitResultMessageEvent
- [ ] CompetitionVotingInfoMessageEvent
- [ ] CurrentTimingCodeMessageEvent
- [ ] IsUserPartOfCompetitionMessageEvent
- [ ] NoOwnedRoomsAlertMessageEvent
- [ ] SecondsUntilMessageEvent

### crafting (x4)
- [ ] CraftableProductsMessageEvent
- [ ] CraftingRecipeMessageEvent
- [ ] CraftingRecipesAvailableMessageEvent
- [ ] CraftingResultMessageEvent

### customfilter (x2)
- [ ] GetCustomFilterResultMessageEvent
- [ ] ModifyCustomFilterResultMessageEvent

### directory (x15)
- [ ] Game2AccountGameStatusMessageEvent
- [ ] Game2GameCancelledMessageEvent
- [ ] Game2GameCreatedMessageEvent
- [ ] Game2GameDirectoryStatusMessageEvent
- [ ] Game2GameLongDataMessageEvent
- [ ] Game2GameNotFoundMessageEvent
- [ ] Game2GameStartedMessageEvent
- [ ] Game2InArenaQueueMessageEvent
- [ ] Game2JoiningGameFailedMessageEvent
- [ ] Game2StartCounterMessageEvent
- [ ] Game2StartingGameFailedMessageEvent
- [ ] Game2StopCounterMessageEvent
- [ ] Game2UserBlockedMessageEvent
- [ ] Game2UserJoinedGameMessageEvent
- [ ] Game2UserLeftGameMessageEvent

### engine (x30)
- [ ] BuildersClubPlacementWarningMessageEvent
- [ ] FavoriteMembershipUpdateMessageEvent
- [ ] FloorHeightMapMessageEvent
- [ ] FurnitureAliasesMessageEvent
- [ ] HeightMapMessageEvent
- [ ] HeightMapUpdateMessageEvent
- [ ] ItemAddMessageEvent
- [ ] ItemDataUpdateMessageEvent
- [ ] ItemRemoveMessageEvent
- [ ] ItemsMessageEvent
- [ ] ItemsStateUpdateMessageEvent
- [ ] ItemStateUpdateMessageEvent
- [ ] ItemUpdateMessageEvent
- [ ] ObjectAddMessageEvent
- [ ] ObjectDataUpdateMessageEvent
- [ ] ObjectRemoveConfirmMessageEvent
- [ ] ObjectRemoveMessageEvent
- [ ] ObjectRemoveMultipleMessageEvent
- [ ] ObjectsDataUpdateMessageEvent
- [ ] ObjectsMessageEvent
- [ ] ObjectUpdateMessageEvent
- [ ] RoomEntryInfoMessageEvent
- [ ] RoomPropertyMessageEvent
- [ ] SlideObjectBundleMessageEvent
- [ ] SpecialRoomEffectMessageEvent
- [ ] UserChangeMessageEvent
- [ ] UserRemoveMessageEvent
- [ ] UsersMessageEvent
- [ ] UserUpdateMessageEvent
- [ ] WiredMovementsMessageEvent

### friendfurni (x3)
- [ ] FriendFurniCancelLockMessageEvent
- [ ] FriendFurniOtherLockConfirmedMessageEvent
- [ ] FriendFurniStartConfirmationMessageEvent

### friendlist (x3)
- [ ] FriendListFragmentMessageEvent
- [ ] MiniMailNewMessageEvent
- [ ] NewConsoleMessageEvent

### furniture (x18)
- [ ] AreaHideMessageEvent
- [ ] CustomStackingHeightUpdateMessageEvent
- [ ] CustomUserNotificationMessageEvent
- [ ] DiceValueMessageEvent
- [ ] FurniRentOrBuyoutOfferMessageEvent
- [ ] GuildFurniContextMenuInfoMessageEvent
- [ ] OneWayDoorStatusMessageEvent
- [ ] OpenPetPackageRequestedMessageEvent
- [ ] OpenPetPackageResultMessageEvent
- [ ] PresentOpenedMessageEvent
- [ ] RentableSpaceRentFailedMessageEvent
- [ ] RentableSpaceRentOkMessageEvent
- [ ] RequestSpamWallPostItMessageEvent
- [ ] RoomDimmerPresetsMessageEvent
- [ ] RoomMessageNotificationMessageEvent
- [ ] YoutubeControlVideoMessageEvent
- [ ] YoutubeDisplayPlaylistsMessageEvent
- [ ] YoutubeDisplayVideoMessageEvent

### gifts (x3)
- [ ] PhoneCollectionStateMessageEvent
- [ ] TryPhoneNumberResultMessageEvent
- [ ] TryVerificationCodeResultMessageEvent

### groupforums (x7)
- [ ] ForumDataMessageEvent
- [ ] ForumsListMessageEvent
- [ ] ForumThreadsMessageEvent
- [ ] PostThreadMessageEvent
- [ ] ThreadMessagesMessageEvent
- [ ] UnreadForumsCountMessageEvent
- [ ] UpdateMessageMessageEvent

### handshake (x4)
- [ ] AuthenticationOKMessageEvent
- [ ] NoobnessLevelMessageEvent
- [ ] PingMessageEvent
- [ ] UserRightsMessageEvent

### help (x25)
- [ ] CallForHelpDisabledNotifyMessageEvent
- [ ] CallForHelpPendingCallsDeletedMessageEvent
- [ ] CallForHelpPendingCallsMessageEvent
- [ ] CallForHelpReplyMessageEvent
- [ ] CallForHelpResultMessageEvent
- [ ] ChatReviewSessionDetachedMessageEvent
- [ ] ChatReviewSessionOfferedToGuideMessageEvent
- [ ] ChatReviewSessionResultsMessageEvent
- [ ] ChatReviewSessionStartedMessageEvent
- [ ] ChatReviewSessionVotingStatusMessageEvent
- [ ] GuideOnDutyStatusMessageEvent
- [ ] GuideReportingStatusMessageEvent
- [ ] GuideSessionAttachedMessageEvent
- [ ] GuideSessionDetachedMessageEvent
- [ ] GuideSessionEndedMessageEvent
- [ ] GuideSessionErrorMessageEvent
- [ ] GuideSessionInvitedToGuideRoomMessageEvent
- [ ] GuideSessionPartnerIsTypingMessageEvent
- [ ] GuideSessionRequesterRoomMessageEvent
- [ ] GuideSessionStartedMessageEvent
- [ ] GuideTicketCreationResultMessageEvent
- [ ] GuideTicketResolutionMessageEvent
- [ ] IssueCloseNotificationMessageEvent
- [ ] QuizDataMessageEvent
- [ ] QuizResultsMessageEvent

### hotlooks (x1)
- [ ] HotLooksMessageEvent

### ingame (x2)
- [ ] Game2FullGameStatusMessageEvent
- [ ] Game2GameStatusMessageEvent

### landingview (x1)
- [ ] PromoArticlesMessageEvent

### layout (x2)
- [ ] RoomEntryTileMessageEvent
- [ ] RoomOccupiedTilesMessageEvent

### lobby (x4)
- [ ] AchievementResolutionCompletedMessageEvent
- [ ] AchievementResolutionProgressMessageEvent
- [ ] AchievementResolutionsMessageEvent
- [ ] UserGameAchievementsMessageEvent

### moderation (x8)
- [ ] BanInfoMessageEvent
- [ ] IssueDeletedMessageEvent
- [ ] IssueInfoMessageEvent
- [ ] IssuePickFailedMessageEvent
- [ ] ModeratorActionResultMessageEvent
- [ ] ModeratorInitMessageEvent
- [ ] ModeratorMessageEvent
- [ ] UserBannedMessageEvent

### mysterybox (x4)
- [ ] CancelMysteryBoxWaitMessageEvent
- [ ] GotMysteryBoxPrizeMessageEvent
- [ ] MysteryBoxKeysMessageEvent
- [ ] ShowMysteryBoxWaitMessageEvent

### navigator (x4)
- [ ] CompetitionRoomsDataMessageEvent
- [ ] DoorbellMessageEvent
- [ ] FlatAccessDeniedMessageEvent
- [ ] NavigatorCollapsedCategoriesMessageEvent

### nft (x8)
- [ ] NftEmeraldConvertResultMessageEvent
- [ ] TradeNftAssetInventoryMessageEvent
- [ ] TradeNftAssetsMessageEvent
- [ ] UserNftChatStylesMessageEvent
- [ ] UserNftWardrobeMessageEvent
- [ ] UserNftWardrobeSelectionMessageEvent
- [ ] UserPurchasableChatStyleChangedMessageEvent
- [ ] UserPurchasableChatStylesMessageEvent

### notifications (x9)
- [ ] ActivityPointsMessageEvent
- [ ] ElementPointerMessageEvent
- [ ] HabboAchievementNotificationMessageEvent
- [ ] HabboActivityPointNotificationMessageEvent
- [ ] HabboBroadcastMessageEvent
- [ ] InfoFeedEnableMessageEvent
- [ ] NotificationDialogMessageEvent
- [ ] OfferRewardDeliveredMessageEvent
- [ ] RestoreClientMessageEvent

### perk (x1)
- [ ] PerkAllowancesMessageEvent

### permissions (x3)
- [ ] YouAreControllerMessageEvent
- [ ] YouAreNotControllerMessageEvent
- [ ] YouAreOwnerMessageEvent

### pets (x3)
- [ ] PetCommandsMessageEvent
- [ ] PetInfoMessageEvent
- [ ] PetReceivedMessageEvent

### quest (x13)
- [ ] CommunityGoalHallOfFameMessageEvent
- [ ] CommunityGoalProgressMessageEvent
- [ ] ConcurrentUsersGoalProgressMessageEvent
- [ ] DailyTasksActiveListMessageEvent
- [ ] DailyTasksTasksAddedMessageEvent
- [ ] DailyTasksTaskUpdateMessageEvent
- [ ] EpicPopupMessageEvent
- [ ] QuestCancelledMessageEvent
- [ ] QuestCompletedMessageEvent
- [ ] QuestDailyMessageEvent
- [ ] QuestMessageEvent
- [ ] QuestsMessageEvent
- [ ] SeasonalQuestsMessageEvent

### session (x12)
- [ ] CantConnectMessageEvent
- [ ] CloseConnectionMessageEvent
- [ ] FlatAccessibleMessageEvent
- [ ] GamePlayerValueMessageEvent
- [ ] HanditemConfigurationMessageEvent
- [ ] OpenConnectionMessageEvent
- [ ] RoomForwardMessageEvent
- [ ] RoomQueueStatusMessageEvent
- [ ] RoomReadyMessageEvent
- [ ] YouAreNotSpectatorMessageEvent
- [ ] YouArePlayingGameMessageEvent
- [ ] YouAreSpectatorMessageEvent

### sound (x8)
- [ ] JukeboxPlayListFullMessageEvent
- [ ] JukeboxSongDisksMessageEvent
- [ ] NowPlayingMessageEvent
- [ ] OfficialSongIdMessageEvent
- [ ] PlayListMessageEvent
- [ ] PlayListSongAddedMessageEvent
- [ ] TraxSongInfoMessageEvent
- [ ] UserSongDisksInventoryMessageEvent

### talent (x3)
- [ ] TalentLevelUpMessageEvent
- [ ] TalentTrackLevelMessageEvent
- [ ] TalentTrackMessageEvent

### tracking (x1)
- [ ] LatencyPingResponseMessageEvent

### trading (x2)
- [ ] TradeSilverFeeMessageEvent
- [ ] TradeSilverSetMessageEvent

### treasurehunt (x3)
- [ ] TreasureHuntFailMessageEvent
- [ ] TreasureHuntFirstWinnerMessageEvent
- [ ] TreasureHuntUpdateMessageEvent

### userclassification (x1)
- [ ] UserClassificationMessageEvent

### userdefinedroomevents (x2)
- [ ] WiredEnvironmentMessageEvent
- [ ] WiredRewardResultMessageEvent

### users (x30)
- [ ] AccountSafetyLockStatusChangeMessageEvent
- [ ] BlockListMessageEvent
- [ ] BlockUserUpdateMessageEvent
- [ ] ExtendedProfileChangedMessageEvent
- [ ] ExtendedProfileMessageEvent
- [ ] GroupDetailsChangedMessageEvent
- [ ] GroupMembershipRequestedMessageEvent
- [ ] GuildCreatedMessageEvent
- [ ] GuildCreationInfoMessageEvent
- [ ] GuildEditFailedMessageEvent
- [ ] GuildEditInfoMessageEvent
- [ ] GuildEditorDataMessageEvent
- [ ] GuildMemberFurniCountInHQMessageEvent
- [ ] GuildMemberMgmtFailedMessageEvent
- [ ] GuildMembershipRejectedMessageEvent
- [ ] GuildMembershipsMessageEvent
- [ ] GuildMembershipUpdatedMessageEvent
- [ ] GuildMembersMessageEvent
- [ ] HabboGroupBadgesMessageEvent
- [ ] HabboGroupDeactivatedMessageEvent
- [ ] HabboGroupDetailsMessageEvent
- [ ] HabboGroupJoinFailedMessageEvent
- [ ] HabboUserBadgesMessageEvent
- [ ] HandItemReceivedMessageEvent
- [ ] IgnoredUsersMessageEvent
- [ ] IgnoreResultMessageEvent
- [ ] InClientLinkMessageEvent
- [ ] RespectNotificationMessageEvent
- [ ] ScrSendKickbackInfoMessageEvent
- [ ] UserNameChangedMessageEvent

### vault (x2)
- [ ] IncomeRewardClaimResponseMessageEvent
- [ ] IncomeRewardNotificationMessageEvent

## Revision: headers manquants outgoing (Headers.cs)
- Total: 497
### achievements (x1)
- [ ] GetAchievementsComposer

### action (x10)
- [ ] AmbassadorAlertMessageComposer
- [ ] AssignRightsMessageComposer
- [ ] BanUserWithDurationMessageComposer
- [ ] KickUserMessageComposer
- [ ] LetUserInMessageComposer
- [ ] MuteUserMessageComposer
- [ ] RemoveAllRightsMessageComposer
- [ ] RemoveRightsMessageComposer
- [ ] UnbanUserFromRoomMessageComposer
- [ ] UnmuteUserMessageComposer

### advertisement (x2)
- [ ] GetInterstitialMessageComposer
- [ ] InterstitialShownMessageComposer

### arena (x4)
- [ ] Game2ExitGameMessageComposer
- [ ] Game2GameChatMessageComposer
- [ ] Game2LoadStageReadyMessageComposer
- [ ] Game2PlayAgainMessageComposer

### avatar (x14)
- [ ] AvatarExpressionMessageComposer
- [ ] ChangeMottoMessageComposer
- [ ] ChangePostureMessageComposer
- [ ] ChangeUserNameInRoomMessageComposer
- [ ] ChangeUserNameMessageComposer
- [ ] CheckUserNameMessageComposer
- [ ] CustomizeAvatarWithFurniMessageComposer
- [ ] DropCarryItemMessageComposer
- [ ] GetWardrobeMessageComposer
- [ ] LookToMessageComposer
- [ ] PassCarryItemMessageComposer
- [ ] PassCarryItemToPetMessageComposer
- [ ] SaveWardrobeOutfitMessageComposer
- [ ] SignMessageComposer

### badges (x5)
- [ ] GetBadgePointLimitsComposer
- [ ] GetBadgesComposer
- [ ] GetIsBadgeRequestFulfilledComposer
- [ ] RequestABadgeComposer
- [ ] SetActivatedBadgesComposer

### bots (x3)
- [ ] CommandBotComposer
- [ ] GetBotCommandConfigurationDataComposer
- [ ] GetBotInventoryComposer

### camera (x6)
- [ ] PhotoCompetitionMessageComposer
- [ ] PublishPhotoMessageComposer
- [ ] PurchasePhotoMessageComposer
- [ ] RenderRoomMessageComposer
- [ ] RenderRoomThumbnailMessageComposer
- [ ] RequestCameraConfigurationMessageComposer

### campaign (x2)
- [ ] OpenCampaignCalendarDoorAsStaffComposer
- [ ] OpenCampaignCalendarDoorComposer

### catalog (x36)
- [ ] BuildersClubPlaceRoomItemMessageComposer
- [ ] BuildersClubPlaceWallItemMessageComposer
- [ ] BuildersClubQueryFurniCountMessageComposer
- [ ] GetBonusRareInfoMessageComposer
- [ ] GetBundleDiscountRulesetComposer
- [ ] GetCatalogIndexComposer
- [ ] GetCatalogPageComposer
- [ ] GetCatalogPageWithEarliestExpiryComposer
- [ ] GetClubGiftMessageComposer
- [ ] GetClubOffersMessageComposer
- [ ] GetGiftWrappingConfigurationComposer
- [ ] GetHabboClubExtendOfferMessageComposer
- [ ] GetIsOfferGiftableComposer
- [ ] GetLimitedOfferAppearingNextComposer
- [ ] GetNextTargetedOfferComposer
- [ ] GetProductOfferComposer
- [ ] GetRecyclerPrizesMessageComposer
- [ ] GetRecyclerStatusMessageComposer
- [ ] GetRoomAdPurchaseInfoComposer
- [ ] GetSeasonalCalendarDailyComposer
- [ ] GetSellablePetPalettesComposer
- [ ] GetSnowWarGameTokensOfferComposer
- [ ] MarkCatalogNewAdditionsPageOpenedComposer
- [ ] PurchaseBasicMembershipExtensionComposer
- [ ] PurchaseFromCatalogAsGiftComposer
- [ ] PurchaseFromCatalogComposer
- [ ] PurchaseRoomAdMessageComposer
- [ ] PurchaseSnowWarGameTokensOfferComposer
- [ ] PurchaseTargetedOfferComposer
- [ ] PurchaseVipMembershipExtensionComposer
- [ ] RecycleItemsMessageComposer
- [ ] RedeemVoucherMessageComposer
- [ ] RoomAdPurchaseInitiatedComposer
- [ ] SelectClubGiftComposer
- [ ] SetTargetedOfferStateComposer
- [ ] ShopTargetedOfferViewedComposer

### chat (x2)
- [ ] CancelTypingMessageComposer
- [ ] StartTypingMessageComposer

### collectibles (x16)
- [ ] ClaimNftClaimsMessageComposer
- [ ] GetCollectibleMintableItemTypesMessageComposer
- [ ] GetCollectibleMintingEnabledMessageComposer
- [ ] GetCollectibleMintTokensMessageComposer
- [ ] GetCollectibleWalletAddressesMessageComposer
- [ ] GetCollectorScoreMessageComposer
- [ ] GetMintTokenOffersMessageComposer
- [ ] GetNftClaimsMessageComposer
- [ ] GetNftCollectionsMessageComposer
- [ ] GetNftStoreOffersMessageComposer
- [ ] GetNftTransferFeeMessageComposer
- [ ] MintItemMessageComposer
- [ ] NftCollectiblesClaimBonusItemMessageComposer
- [ ] NftCollectiblesClaimRewardItemMessageComposer
- [ ] NftTransferAssetsMessageComposer
- [ ] PurchaseMintTokenMessageComposer

### competition (x9)
- [ ] ForwardToACompetitionRoomMessageComposer
- [ ] ForwardToASubmittableRoomMessageComposer
- [ ] ForwardToRandomCompetitionRoomMessageComposer
- [ ] GetCurrentTimingCodeMessageComposer
- [ ] GetIsUserPartOfCompetitionMessageComposer
- [ ] GetSecondsUntilMessageComposer
- [ ] RoomCompetitionInitMessageComposer
- [ ] SubmitRoomToCompetitionMessageComposer
- [ ] VoteForRoomMessageComposer

### crafting (x5)
- [ ] CraftComposer
- [ ] CraftSecretComposer
- [ ] GetCraftableProductsComposer
- [ ] GetCraftingRecipeComposer
- [ ] GetCraftingRecipesAvailableComposer

### customfilter (x3)
- [ ] AddToCustomFilterMessageComposer
- [ ] GetCustomFilterMessageComposer
- [ ] RemoveFromCustomFilterMessageComposer

### directory (x5)
- [ ] Game2CheckGameDirectoryStatusMessageComposer
- [ ] Game2GetAccountGameStatusMessageComposer
- [ ] Game2LeaveGameMessageComposer
- [ ] Game2QuickJoinGameMessageComposer
- [ ] Game2StartSnowWarMessageComposer

### engine (x27)
- [ ] ClickFurniMessageComposer
- [ ] CompostPlantMessageComposer
- [ ] GetFurnitureAliasesMessageComposer
- [ ] GetItemDataMessageComposer
- [ ] GetPetCommandsMessageComposer
- [ ] GiveSupplementToPetMessageComposer
- [ ] HarvestPetMessageComposer
- [ ] MountPetMessageComposer
- [ ] MoveAvatarMessageComposer
- [ ] MoveObjectMessageComposer
- [ ] MovePetMessageComposer
- [ ] MoveWallItemMessageComposer
- [ ] PickupObjectMessageComposer
- [ ] PlaceBotMessageComposer
- [ ] PlaceObjectMessageComposer
- [ ] PlacePetMessageComposer
- [ ] RemoveBotFromFlatMessageComposer
- [ ] RemoveItemMessageComposer
- [ ] RemovePetFromFlatMessageComposer
- [ ] RemoveSaddleFromPetMessageComposer
- [ ] SetClothingChangeDataMessageComposer
- [ ] SetItemDataMessageComposer
- [ ] SetObjectDataMessageComposer
- [ ] TogglePetBreedingPermissionMessageComposer
- [ ] TogglePetRidingPermissionMessageComposer
- [ ] UseFurnitureMessageComposer
- [ ] UseWallItemMessageComposer

### friendfurni (x1)
- [ ] FriendFurniConfirmLockMessageComposer

### friendlist (x13)
- [ ] AcceptFriendMessageComposer
- [ ] DeclineFriendMessageComposer
- [ ] FindNewFriendsMessageComposer
- [ ] FollowFriendMessageComposer
- [ ] GetFriendRequestsMessageComposer
- [ ] GetMessengerHistoryComposer
- [ ] HabboSearchMessageComposer
- [ ] RemoveFriendMessageComposer
- [ ] RequestFriendMessageComposer
- [ ] SendMsgMessageComposer
- [ ] SendRoomInviteMessageComposer
- [ ] SetRelationshipStatusMessageComposer
- [ ] VisitUserMessageComposer

### furni (x2)
- [ ] RequestFurniInventoryComposer
- [ ] RequestFurniInventoryWhenNotInRoomComposer

### furniture (x28)
- [ ] AddSpamWallPostItMessageComposer
- [ ] ControlYoutubeDisplayPlaybackMessageComposer
- [ ] CreditFurniRedeemMessageComposer
- [ ] DiceOffMessageComposer
- [ ] EnterOneWayDoorMessageComposer
- [ ] ExtendRentOrBuyoutFurniMessageComposer
- [ ] ExtendRentOrBuyoutStripItemMessageComposer
- [ ] GetGuildFurniContextMenuInfoMessageComposer
- [ ] GetRentOrBuyoutOfferMessageComposer
- [ ] GetYoutubeDisplayStatusMessageComposer
- [ ] OpenMysteryTrophyMessageComposer
- [ ] OpenPetPackageMessageComposer
- [ ] PlacePostItMessageComposer
- [ ] PresentOpenMessageComposer
- [ ] RentableSpaceCancelRentMessageComposer
- [ ] RentableSpaceRentMessageComposer
- [ ] RoomDimmerChangeStateMessageComposer
- [ ] RoomDimmerGetPresetsMessageComposer
- [ ] RoomDimmerSavePresetMessageComposer
- [ ] SetAreaHideDataComposer
- [ ] SetCustomStackingHeightComposer
- [ ] SetMannequinFigureComposer
- [ ] SetMannequinNameComposer
- [ ] SetRandomStateMessageComposer
- [ ] SetRoomBackgroundColorDataComposer
- [ ] SetYoutubeDisplayPlaylistMessageComposer
- [ ] SpinWheelOfFortuneMessageComposer
- [ ] ThrowDiceMessageComposer

### gifts (x4)
- [ ] ResetPhoneNumberStateMessageComposer
- [ ] SetPhoneNumberVerificationStatusMessageComposer
- [ ] TryPhoneNumberMessageComposer
- [ ] VerifyCodeMessageComposer

### groupforums (x10)
- [ ] GetForumsListMessageComposer
- [ ] GetForumStatsMessageComposer
- [ ] GetMessagesMessageComposer
- [ ] GetThreadMessageComposer
- [ ] GetThreadsMessageComposer
- [ ] GetUnreadForumsCountMessageComposer
- [ ] ModerateMessageMessageComposer
- [ ] ModerateThreadMessageComposer
- [ ] UpdateForumReadMarkerMessageComposer
- [ ] UpdateForumSettingsMessageComposer

### handshake (x7)
- [ ] ClientHelloMessageComposer
- [ ] DisconnectMessageComposer
- [ ] InfoRetrieveMessageComposer
- [ ] PongMessageComposer
- [ ] SSOTicketMessageComposer
- [ ] UniqueIDMessageComposer
- [ ] VersionCheckMessageComposer

### help (x27)
- [ ] AppealCfhMessageComposer
- [ ] CallForHelpFromForumMessageMessageComposer
- [ ] CallForHelpFromForumThreadMessageComposer
- [ ] CallForHelpFromIMMessageComposer
- [ ] CallForHelpFromPhotoMessageComposer
- [ ] CallForHelpFromSelfieMessageComposer
- [ ] CallForHelpMessageComposer
- [ ] ChatReviewGuideDecidesOnOfferMessageComposer
- [ ] ChatReviewGuideDetachedMessageComposer
- [ ] ChatReviewGuideVoteMessageComposer
- [ ] ChatReviewSessionCreateMessageComposer
- [ ] DeletePendingCallsForHelpMessageComposer
- [ ] GetCfhStatusMessageComposer
- [ ] GetGuideReportingStatusMessageComposer
- [ ] GetPendingCallsForHelpMessageComposer
- [ ] GetQuizQuestionsComposer
- [ ] GuideSessionCreateMessageComposer
- [ ] GuideSessionFeedbackMessageComposer
- [ ] GuideSessionGetRequesterRoomMessageComposer
- [ ] GuideSessionGuideDecidesMessageComposer
- [ ] GuideSessionInviteRequesterMessageComposer
- [ ] GuideSessionIsTypingMessageComposer
- [ ] GuideSessionOnDutyUpdateMessageComposer
- [ ] GuideSessionReportMessageComposer
- [ ] GuideSessionRequesterCancelsMessageComposer
- [ ] GuideSessionResolvedMessageComposer
- [ ] PostQuizAnswersComposer

### hotlooks (x1)
- [ ] GetHotLooksMessageComposer

### ingame (x5)
- [ ] Game2MakeSnowballMessageComposer
- [ ] Game2RequestFullStatusUpdateMessageComposer
- [ ] Game2SetUserMoveTargetMessageComposer
- [ ] Game2ThrowSnowballAtHumanMessageComposer
- [ ] Game2ThrowSnowballAtPositionMessageComposer

### landingview (x1)
- [ ] GetPromoArticlesMessageComposer

### layout (x3)
- [ ] GetOccupiedTilesMessageComposer
- [ ] GetRoomEntryTileMessageComposer
- [ ] UpdateFloorPropertiesMessageComposer

### lobby (x2)
- [ ] GetResolutionAchievementsMessageComposer
- [ ] GetUserGameAchievementsMessageComposer

### marketplace (x10)
- [ ] BuyMarketplaceOfferMessageComposer
- [ ] BuyMarketplaceTokensMessageComposer
- [ ] CancelMarketplaceOfferMessageComposer
- [ ] GetMarketplaceCanMakeOfferMessageComposer
- [ ] GetMarketplaceConfigurationMessageComposer
- [ ] GetMarketplaceItemStatsComposer
- [ ] GetMarketplaceOffersMessageComposer
- [ ] GetMarketplaceOwnOffersMessageComposer
- [ ] MakeOfferMessageComposer
- [ ] RedeemMarketplaceOfferCreditsMessageComposer

### moderator (x21)
- [ ] CloseIssueDefaultActionMessageComposer
- [ ] CloseIssuesMessageComposer
- [ ] DefaultSanctionMessageComposer
- [ ] GetCfhChatlogMessageComposer
- [ ] GetModeratorRoomInfoMessageComposer
- [ ] GetModeratorUserInfoMessageComposer
- [ ] GetRoomChatlogMessageComposer
- [ ] GetRoomVisitsMessageComposer
- [ ] GetUserChatlogMessageComposer
- [ ] ModAlertMessageComposer
- [ ] ModBanMessageComposer
- [ ] ModerateRoomMessageComposer
- [ ] ModeratorActionMessageComposer
- [ ] ModKickMessageComposer
- [ ] ModMessageMessageComposer
- [ ] ModMuteMessageComposer
- [ ] ModToolPreferencesComposer
- [ ] ModToolSanctionComposer
- [ ] ModTradingLockMessageComposer
- [ ] PickIssuesMessageComposer
- [ ] ReleaseIssuesMessageComposer

### mysterybox (x1)
- [ ] MysteryBoxWaitingCanceledMessageComposer

### navigator (x35)
- [ ] AddFavouriteRoomMessageComposer
- [ ] CancelEventMessageComposer
- [ ] CompetitionRoomsSearchMessageComposer
- [ ] ConvertGlobalRoomIdMessageComposer
- [ ] CreateFlatMessageComposer
- [ ] DeleteFavouriteRoomMessageComposer
- [ ] EditEventMessageComposer
- [ ] ForwardToARandomPromotedRoomMessageComposer
- [ ] ForwardToSomeRoomMessageComposer
- [ ] GetGuestRoomMessageComposer
- [ ] GetOfficialRoomsMessageComposer
- [ ] GetPopularRoomTagsMessageComposer
- [ ] GetUserEventCatsMessageComposer
- [ ] GetUserFlatCatsMessageComposer
- [ ] GuildBaseSearchMessageComposer
- [ ] MyFavouriteRoomsSearchMessageComposer
- [ ] MyFrequentRoomHistorySearchMessageComposer
- [ ] MyFriendsRoomsSearchMessageComposer
- [ ] MyGuildBasesSearchMessageComposer
- [ ] MyRecommendedRoomsMessageComposer
- [ ] MyRoomHistorySearchMessageComposer
- [ ] MyRoomRightsSearchMessageComposer
- [ ] MyRoomsSearchMessageComposer
- [ ] PopularRoomsSearchMessageComposer
- [ ] RateFlatMessageComposer
- [ ] RemoveOwnRoomRightsRoomMessageComposer
- [ ] RoomAdEventTabAdClickedComposer
- [ ] RoomAdEventTabViewedComposer
- [ ] RoomAdSearchMessageComposer
- [ ] RoomsWhereMyFriendsAreSearchMessageComposer
- [ ] RoomsWithHighestScoreSearchMessageComposer
- [ ] RoomTextSearchMessageComposer
- [ ] SetRoomSessionTagsMessageComposer
- [ ] ToggleStaffPickMessageComposer
- [ ] UpdateHomeRoomMessageComposer

### newnavigator (x7)
- [ ] NavigatorAddCollapsedCategoryMessageComposer
- [ ] NavigatorAddSavedSearchComposer
- [ ] NavigatorDeleteSavedSearchComposer
- [ ] NavigatorRemoveCollapsedCategoryMessageComposer
- [ ] NavigatorSetSearchCodeViewModeMessageComposer
- [ ] NewNavigatorInitComposer
- [ ] NewNavigatorSearchComposer

### nft (x8)
- [ ] AddNftToTradeComposer
- [ ] GetNftCreditsMessageComposer
- [ ] GetNftTradeInventoryComposer
- [ ] GetSelectedNftWardrobeOutfitMessageComposer
- [ ] GetSilverMessageComposer
- [ ] GetUserNftWardrobeMessageComposer
- [ ] RemoveNftFromTradeComposer
- [ ] SaveUserNftWardrobeMessageComposer

### notifications (x2)
- [ ] ResetUnseenItemIdsComposer
- [ ] ResetUnseenItemsComposer

### nux (x2)
- [ ] NewUserExperienceGetGiftsMessageComposer
- [ ] NewUserExperienceScriptProceedComposer

### pets (x8)
- [ ] BreedPetsMessageComposer
- [ ] CancelPetBreedingComposer
- [ ] ConfirmPetBreedingComposer
- [ ] CustomizePetWithFurniComposer
- [ ] GetPetInfoMessageComposer
- [ ] GetPetInventoryComposer
- [ ] PetSelectedMessageComposer
- [ ] RespectPetMessageComposer

### poll (x3)
- [ ] PollAnswerComposer
- [ ] PollRejectComposer
- [ ] PollStartComposer

### preferences (x7)
- [ ] SetChatPreferencesMessageComposer
- [ ] SetChatStylePreferenceComposer
- [ ] SetIgnoreRoomInvitesMessageComposer
- [ ] SetNewNavigatorWindowPreferencesMessageComposer
- [ ] SetRoomCameraPreferencesMessageComposer
- [ ] SetSoundSettingsComposer
- [ ] SetUIFlagsMessageComposer

### purse (x1)
- [ ] GetCreditsInfoComposer

### quest (x16)
- [ ] AcceptQuestMessageComposer
- [ ] ActivateQuestMessageComposer
- [ ] CancelQuestMessageComposer
- [ ] ClaimDailyTaskComposer
- [ ] FriendRequestQuestCompleteMessageComposer
- [ ] GetCommunityGoalHallOfFameMessageComposer
- [ ] GetCommunityGoalProgressMessageComposer
- [ ] GetConcurrentUsersGoalProgressMessageComposer
- [ ] GetConcurrentUsersRewardMessageComposer
- [ ] GetDailyQuestMessageComposer
- [ ] GetDailyTasksComposer
- [ ] GetQuestsMessageComposer
- [ ] GetSeasonalQuestsOnlyMessageComposer
- [ ] OpenQuestTrackerMessageComposer
- [ ] RejectQuestMessageComposer
- [ ] StartCampaignMessageComposer

### register (x1)
- [ ] UpdateFigureDataMessageComposer

### roomdirectory (x1)
- [ ] RoomNetworkOpenConnectionMessageComposer

### roomsettings (x8)
- [ ] DeleteRoomMessageComposer
- [ ] GetBannedUsersFromRoomMessageComposer
- [ ] GetCustomRoomFilterMessageComposer
- [ ] GetFlatControllersMessageComposer
- [ ] GetRoomSettingsMessageComposer
- [ ] SaveRoomSettingsMessageComposer
- [ ] UpdateRoomCategoryAndTradeSettingsComposer
- [ ] UpdateRoomFilterMessageComposer

### score (x10)
- [ ] Game2GetFriendsLeaderboardComposer
- [ ] Game2GetTotalGroupLeaderboardComposer
- [ ] Game2GetTotalLeaderboardComposer
- [ ] Game2GetWeeklyFriendsLeaderboardComposer
- [ ] Game2GetWeeklyGroupLeaderboardComposer
- [ ] Game2GetWeeklyLeaderboardComposer
- [ ] GetFriendsWeeklyCompetitiveLeaderboardComposer
- [ ] GetWeeklyCompetitiveLeaderboardComposer
- [ ] GetWeeklyGameRewardComposer
- [ ] GetWeeklyGameRewardWinnersComposer

### session (x3)
- [ ] ChangeQueueMessageComposer
- [ ] OpenFlatConnectionMessageComposer
- [ ] QuitMessageComposer

### sound (x9)
- [ ] AddJukeboxDiskComposer
- [ ] GetJukeboxPlayListMessageComposer
- [ ] GetNowPlayingMessageComposer
- [ ] GetOfficialSongIdMessageComposer
- [ ] GetSongInfoMessageComposer
- [ ] GetSoundMachinePlayListMessageComposer
- [ ] GetSoundSettingsComposer
- [ ] GetUserSongDisksMessageComposer
- [ ] RemoveJukeboxDiskComposer

### talent (x3)
- [ ] GetTalentTrackLevelMessageComposer
- [ ] GetTalentTrackMessageComposer
- [ ] GuideAdvertisementReadMessageComposer

### tracking (x5)
- [ ] EventLogMessageComposer
- [ ] LagWarningReportMessageComposer
- [ ] LatencyPingReportMessageComposer
- [ ] LatencyPingRequestMessageComposer
- [ ] PerformanceLogMessageComposer

### trading (x10)
- [ ] AcceptTradingComposer
- [ ] AddItemsToTradeComposer
- [ ] AddItemToTradeComposer
- [ ] CloseTradingComposer
- [ ] ConfirmAcceptTradingComposer
- [ ] ConfirmDeclineTradingComposer
- [ ] OpenTradingComposer
- [ ] RemoveItemFromTradeComposer
- [ ] SilverFeeMessageComposer
- [ ] UnacceptTradingComposer

### treasurehunt (x1)
- [ ] ProgressTreasureHuntMessageComposer

### userclassification (x2)
- [ ] PeerUsersClassificationMessageComposer
- [ ] RoomUsersClassificationMessageComposer

### userdefinedroomevents (x8)
- [ ] ApplySnapshotMessageComposer
- [ ] UpdateActionMessageComposer
- [ ] UpdateAddonMessageComposer
- [ ] UpdateConditionMessageComposer
- [ ] UpdateSelectorMessageComposer
- [ ] UpdateTriggerMessageComposer
- [ ] UpdateVariableMessageComposer
- [ ] WiredClickUserMessageComposer

### users (x42)
- [ ] AddAdminRightsToMemberMessageComposer
- [ ] ApproveAllMembershipRequestsMessageComposer
- [ ] ApproveMembershipRequestMessageComposer
- [ ] BlockListInitComposer
- [ ] BlockUserMessageComposer
- [ ] ChangeEmailComposer
- [ ] CreateGuildMessageComposer
- [ ] DeactivateGuildMessageComposer
- [ ] DeselectFavouriteHabboGroupMessageComposer
- [ ] GetEmailStatusComposer
- [ ] GetExtendedProfileByNameMessageComposer
- [ ] GetExtendedProfileMessageComposer
- [ ] GetGuildCreationInfoMessageComposer
- [ ] GetGuildEditInfoMessageComposer
- [ ] GetGuildEditorDataMessageComposer
- [ ] GetGuildMembershipsMessageComposer
- [ ] GetGuildMembersMessageComposer
- [ ] GetHabboGroupBadgesMessageComposer
- [ ] GetHabboGroupDetailsMessageComposer
- [ ] GetIgnoredUsersMessageComposer
- [ ] GetMemberGuildItemCountMessageComposer
- [ ] GetMOTDMessageComposer
- [ ] GetRelationshipStatusInfoMessageComposer
- [ ] GetSelectedBadgesMessageComposer
- [ ] GetUserNftChatStylesMessageComposer
- [ ] IgnoreUserMessageComposer
- [ ] JoinHabboGroupMessageComposer
- [ ] KickMemberMessageComposer
- [ ] RejectMembershipRequestMessageComposer
- [ ] RemoveAdminRightsFromMemberMessageComposer
- [ ] ReplenishRespectMessageComposer
- [ ] RespectUserMessageComposer
- [ ] ScrGetKickbackInfoMessageComposer
- [ ] ScrGetUserInfoMessageComposer
- [ ] SelectFavouriteHabboGroupMessageComposer
- [ ] UnblockGroupMemberMessageComposer
- [ ] UnblockUserMessageComposer
- [ ] UnignoreUserMessageComposer
- [ ] UpdateGuildBadgeMessageComposer
- [ ] UpdateGuildColorsMessageComposer
- [ ] UpdateGuildIdentityMessageComposer
- [ ] UpdateGuildSettingsMessageComposer

### variablesmanagement (x3)
- [ ] WiredGetUserPermanentVariablesComposer
- [ ] WiredGetVariableOwnersPageComposer
- [ ] WiredSetUserPermanentVariableComposer

### vault (x2)
- [ ] IncomeRewardClaimMessageComposer
- [ ] WithdrawCreditVaultMessageComposer

### votes (x1)
- [ ] CommunityGoalVoteMessageComposer

### wiredmenu (x13)
- [ ] WiredClearErrorLogsMessageComposer
- [ ] WiredGetAllVariableHoldersMessageComposer
- [ ] WiredGetAllVariablesDiffsMessageComposer
- [ ] WiredGetAllVariablesHashMessageComposer
- [ ] WiredGetErrorLogsMessageComposer
- [ ] WiredGetRoomLogsComposer
- [ ] WiredGetRoomSettingsMessageComposer
- [ ] WiredGetRoomStatsMessageComposer
- [ ] WiredGetVariablesForObjectMessageComposer
- [ ] WiredSetObjectVariableValueMessageComposer
- [ ] WiredSetPreferencesMessageComposer
- [ ] WiredSetRoomSettingsMessageComposer
- [ ] WiredUpdateRoomComposer

## Revision: parsers C# manquants
- Total: 522
### achievements (x3)
- [ ] AchievementEventParser
- [ ] AchievementsEventParser
- [ ] AchievementsScoreEventParser

### action (x5)
- [ ] AvatarEffectMessageEventParser
- [ ] CarryObjectMessageEventParser
- [ ] ExpressionMessageEventParser
- [ ] SleepMessageEventParser
- [ ] UseObjectMessageEventParser

### advertisement (x2)
- [ ] InterstitialMessageEventParser
- [ ] RoomAdErrorEventParser

### arena (x13)
- [ ] Game2ArenaEnteredMessageEventParser
- [ ] Game2EnterArenaFailedMessageEventParser
- [ ] Game2EnterArenaMessageEventParser
- [ ] Game2GameChatFromPlayerMessageEventParser
- [ ] Game2GameEndingMessageEventParser
- [ ] Game2GameRejoinMessageEventParser
- [ ] Game2PlayerExitedGameArenaMessageEventParser
- [ ] Game2PlayerRematchesMessageEventParser
- [ ] Game2StageEndingMessageEventParser
- [ ] Game2StageLoadMessageEventParser
- [ ] Game2StageRunningMessageEventParser
- [ ] Game2StageStartingMessageEventParser
- [ ] Game2StageStillLoadingMessageEventParser

### availability (x5)
- [ ] AvailabilityStatusMessageEventParser
- [ ] InfoHotelClosedMessageEventParser
- [ ] InfoHotelClosingMessageEventParser
- [ ] LoginFailedHotelClosedMessageEventParser
- [ ] MaintenanceStatusMessageEventParser

### avatar (x4)
- [ ] ChangeUserNameResultMessageEventParser
- [ ] CheckUserNameResultMessageEventParser
- [ ] FigureUpdateEventParser
- [ ] WardrobeMessageEventParser

### avatareffect (x3)
- [ ] AvatarEffectAddedMessageEventParser
- [ ] AvatarEffectExpiredMessageEventParser
- [ ] AvatarEffectsMessageEventParser

### badges (x4)
- [ ] BadgePointLimitsEventParser
- [ ] BadgeReceivedEventParser
- [ ] BadgesEventParser
- [ ] IsBadgeRequestFulfilledEventParser

### bots (x7)
- [ ] BotAddedToInventoryEventParser
- [ ] BotCommandConfigurationEventParser
- [ ] BotErrorEventParser
- [ ] BotForceOpenContextMenuEventParser
- [ ] BotInventoryEventParser
- [ ] BotRemovedFromInventoryEventParser
- [ ] BotSkillListUpdateEventParser

### callforhelp (x4)
- [ ] CfhSanctionMessageEventParser
- [ ] CfhTopicsInitMessageEventParser
- [ ] MyCfhReportStatusMessageEventParser
- [ ] SanctionStatusEventParser

### camera (x6)
- [ ] CameraPublishStatusMessageEventParser
- [ ] CameraPurchaseOKMessageEventParser
- [ ] CameraStorageUrlMessageEventParser
- [ ] CompetitionStatusMessageEventParser
- [ ] InitCameraMessageEventParser
- [ ] ThumbnailStatusMessageEventParser

### campaign (x2)
- [ ] CampaignCalendarDataMessageEventParser
- [ ] CampaignCalendarDoorOpenedMessageEventParser

### catalog (x34)
- [ ] BonusRareInfoMessageEventParser
- [ ] BuildersClubFurniCountMessageEventParser
- [ ] BuildersClubSubscriptionStatusMessageEventParser
- [ ] BundleDiscountRulesetMessageEventParser
- [ ] CatalogIndexMessageEventParser
- [ ] CatalogPageMessageEventParser
- [ ] CatalogPageWithEarliestExpiryMessageEventParser
- [ ] CatalogPublishedMessageEventParser
- [ ] ClubGiftInfoEventParser
- [ ] ClubGiftSelectedEventParser
- [ ] GiftReceiverNotFoundEventParser
- [ ] GiftWrappingConfigurationEventParser
- [ ] HabboClubExtendOfferMessageEventParser
- [ ] HabboClubOffersMessageEventParser
- [ ] LimitedEditionSoldOutEventParser
- [ ] LimitedOfferAppearingNextMessageEventParser
- [ ] LtdRaffleEnteredMessageEventParser
- [ ] LtdRaffleResultMessageEventParser
- [ ] NotEnoughBalanceMessageEventParser
- [ ] ProductOfferEventParser
- [ ] PurchaseErrorMessageEventParser
- [ ] PurchaseNotAllowedMessageEventParser
- [ ] PurchaseOKMessageEventParser
- [ ] RecyclerFinishedMessageEventParser
- [ ] RecyclerPrizesMessageEventParser
- [ ] RecyclerStatusMessageEventParser
- [ ] RoomAdPurchaseInfoEventParser
- [ ] SeasonalCalendarDailyOfferMessageEventParser
- [ ] SellablePetPalettesMessageEventParser
- [ ] SnowWarGameTokensMessageParser
- [ ] TargetedOfferEventParser
- [ ] TargetedOfferNotFoundEventParser
- [ ] VoucherRedeemErrorMessageEventParser
- [ ] VoucherRedeemOkMessageEventParser

### chat (x5)
- [ ] FloodControlMessageEventParser
- [ ] RemainingMutePeriodEventParser
- [ ] RoomChatSettingsMessageEventParser
- [ ] RoomFilterSettingsMessageEventParser
- [ ] UserTypingMessageEventParser

### clothing (x1)
- [ ] FigureSetIdsEventParser

### collectibles (x20)
- [ ] CollectableMintableItemTypesMessageEventParser
- [ ] CollectibleMintableItemResultMessageEventParser
- [ ] CollectibleMintingEnabledMessageEventParser
- [ ] CollectibleMintTokenCountMessageEventParser
- [ ] CollectibleMintTokenOffersMessageEventParser
- [ ] CollectibleWalletAddressesMessageEventParser
- [ ] EmeraldBalanceMessageEventParser
- [ ] NftBonusItemClaimResultMessageEventParser
- [ ] NftClaimResultMessageEventParser
- [ ] NftClaimsMessageEventParser
- [ ] NftCollectionsMessageEventParser
- [ ] NftCollectionsScoreMessageEventParser
- [ ] NftRewardItemClaimResultMessageEventParser
- [ ] NftStoreOffersMessageEventParser
- [ ] NftStorePurchaseMessageEventParser
- [ ] NftTransferAssetsResultMessageEventParser
- [ ] NftTransferFeeMessageEventParser
- [ ] RedeemNftLootBoxResultMessageEventParser
- [ ] RedeemNftLootBoxStateMessageEventParser
- [ ] SilverBalanceMessageEventParser

### competition (x6)
- [ ] CompetitionEntrySubmitResultMessageEventParser
- [ ] CompetitionVotingInfoMessageEventParser
- [ ] CurrentTimingCodeMessageEventParser
- [ ] IsUserPartOfCompetitionMessageEventParser
- [ ] NoOwnedRoomsAlertMessageEventParser
- [ ] SecondsUntilMessageEventParser

### crafting (x4)
- [ ] CraftableProductsMessageEventParser
- [ ] CraftingRecipeMessageEventParser
- [ ] CraftingRecipesAvailableMessageEventParser
- [ ] CraftingResultMessageEventParser

### customfilter (x2)
- [ ] GetCustomFilterResultMessageEventParser
- [ ] ModifyCustomFilterResultMessageEventParser

### directory (x15)
- [ ] Game2AccountGameStatusMessageParser
- [ ] Game2GameCancelledMessageParser
- [ ] Game2GameCreatedMessageEventParser
- [ ] Game2GameDirectoryStatusMessageParser
- [ ] Game2GameLongDataMessageEventParser
- [ ] Game2GameNotFoundMessageParser
- [ ] Game2GameStartedMessageEventParser
- [ ] Game2InArenaQueueMessageParser
- [ ] Game2JoiningGameFailedMessageParser
- [ ] Game2StartCounterMessageParser
- [ ] Game2StartingGameFailedMessageParser
- [ ] Game2StopCounterMessageParser
- [ ] Game2UserBlockedMessageParser
- [ ] Game2UserJoinedGameMessageEventParser
- [ ] Game2UserLeftGameMessageParser

### engine (x31)
- [ ] BuildersClubPlacementWarningMessageEventParser
- [ ] FavoriteMembershipUpdateMessageEventParser
- [ ] FloorHeightMapMessageEventParser
- [ ] FurnitureAliasesMessageEventParser
- [ ] HeightMapMessageEventParser
- [ ] HeightMapUpdateMessageEventParser
- [ ] ItemAddMessageEventParser
- [ ] ItemDataUpdateMessageEventParser
- [ ] ItemRemoveMessageEventParser
- [ ] ItemsMessageEventParser
- [ ] ItemsStateUpdateMessageEventParser
- [ ] ItemStateUpdateMessageEventParser
- [ ] ItemUpdateMessageEventParser
- [ ] ObjectAddMessageEventParser
- [ ] ObjectDataUpdateMessageEventParser
- [ ] ObjectRemoveConfirmMessageEventParser
- [ ] ObjectRemoveMessageEventParser
- [ ] ObjectRemoveMultipleMessageEventParser
- [ ] ObjectsDataUpdateMessageEventParser
- [ ] ObjectsMessageEventParser
- [ ] ObjectUpdateMessageEventParser
- [ ] RoomEntryInfoMessageEventParser
- [ ] RoomPropertyMessageEventParser
- [ ] RoomVisualizationSettingsEventParser
- [ ] SlideObjectBundleMessageEventParser
- [ ] SpecialRoomEffectMessageEventParser
- [ ] UserChangeMessageEventParser
- [ ] UserRemoveMessageEventParser
- [ ] UsersMessageEventParser
- [ ] UserUpdateMessageEventParser
- [ ] WiredMovementsMessageEventParser

### error (x1)
- [ ] ErrorReportEventParser

### friendfurni (x3)
- [ ] FriendFurniCancelLockMessageEventParser
- [ ] FriendFurniOtherLockConfirmedMessageEventParser
- [ ] FriendFurniStartConfirmationMessageEventParser

### friendlist (x18)
- [ ] AcceptFriendResultEventParser
- [ ] ConsoleMessageHistoryEventParser
- [ ] FindFriendsProcessResultEventParser
- [ ] FollowFriendFailedEventParser
- [ ] FriendListFragmentMessageEventParser
- [ ] FriendListUpdateEventParser
- [ ] FriendNotificationEventParser
- [ ] FriendRequestsEventParser
- [ ] HabboSearchResultEventParser
- [ ] InstantMessageErrorEventParser
- [ ] MessengerErrorEventParser
- [ ] MessengerInitEventParser
- [ ] MiniMailNewMessageEventParser
- [ ] MiniMailUnreadCountEventParser
- [ ] NewConsoleMessageEventParser
- [ ] NewFriendRequestEventParser
- [ ] RoomInviteErrorEventParser
- [ ] RoomInviteEventParser

### furni (x6)
- [ ] FurniListAddOrUpdateEventParser
- [ ] FurniListEventParser
- [ ] FurniListInvalidateEventParser
- [ ] FurniListRemoveEventParser
- [ ] FurniListRemoveMultipleEventParser
- [ ] PostItPlacedEventParser

### furniture (x18)
- [ ] AreaHideMessageEventParser
- [ ] CustomStackingHeightUpdateMessageEventParser
- [ ] CustomUserNotificationMessageEventParser
- [ ] DiceValueMessageEventParser
- [ ] FurniRentOrBuyoutOfferMessageEventParser
- [ ] GuildFurniContextMenuInfoMessageEventParser
- [ ] OneWayDoorStatusMessageEventParser
- [ ] OpenPetPackageRequestedMessageEventParser
- [ ] OpenPetPackageResultMessageEventParser
- [ ] PresentOpenedMessageEventParser
- [ ] RentableSpaceRentFailedMessageEventParser
- [ ] RentableSpaceRentOkMessageEventParser
- [ ] RequestSpamWallPostItMessageEventParser
- [ ] RoomDimmerPresetsMessageEventParser
- [ ] RoomMessageNotificationMessageEventParser
- [ ] YoutubeControlVideoMessageEventParser
- [ ] YoutubeDisplayPlaylistsMessageEventParser
- [ ] YoutubeDisplayVideoMessageEventParser

### gifts (x3)
- [ ] PhoneCollectionStateMessageEventParser
- [ ] TryPhoneNumberResultMessageEventParser
- [ ] TryVerificationCodeResultMessageEventParser

### groupforums (x7)
- [ ] ForumDataMessageEventParser
- [ ] ForumsListMessageEventParser
- [ ] ForumThreadsMessageEventParser
- [ ] PostThreadMessageEventParser
- [ ] ThreadMessagesMessageEventParser
- [ ] UnreadForumsCountMessageEventParser
- [ ] UpdateMessageMessageEventParser

### handshake (x12)
- [ ] AuthenticationOKMessageEventParser
- [ ] CompleteDiffieHandshakeEventParser
- [ ] DisconnectReasonEventParser
- [ ] GenericErrorEventParser
- [ ] IdentityAccountsEventParser
- [ ] InitDiffieHandshakeEventParser
- [ ] IsFirstLoginOfDayEventParser
- [ ] NoobnessLevelMessageEventParser
- [ ] PingMessageEventParser
- [ ] UniqueMachineIDEventParser
- [ ] UserObjectEventParser
- [ ] UserRightsMessageEventParser

### help (x25)
- [ ] CallForHelpDisabledNotifyMessageEventParser
- [ ] CallForHelpPendingCallsDeletedMessageEventParser
- [ ] CallForHelpPendingCallsMessageEventParser
- [ ] CallForHelpReplyMessageEventParser
- [ ] CallForHelpResultMessageEventParser
- [ ] ChatReviewSessionDetachedMessageEventParser
- [ ] ChatReviewSessionOfferedToGuideMessageEventParser
- [ ] ChatReviewSessionResultsMessageEventParser
- [ ] ChatReviewSessionStartedMessageEventParser
- [ ] ChatReviewSessionVotingStatusMessageEventParser
- [ ] GuideOnDutyStatusMessageEventParser
- [ ] GuideReportingStatusMessageEventParser
- [ ] GuideSessionAttachedMessageEventParser
- [ ] GuideSessionDetachedMessageEventParser
- [ ] GuideSessionEndedMessageEventParser
- [ ] GuideSessionErrorMessageEventParser
- [ ] GuideSessionInvitedToGuideRoomMessageEventParser
- [ ] GuideSessionPartnerIsTypingMessageEventParser
- [ ] GuideSessionRequesterRoomMessageEventParser
- [ ] GuideSessionStartedMessageEventParser
- [ ] GuideTicketCreationResultMessageEventParser
- [ ] GuideTicketResolutionMessageEventParser
- [ ] IssueCloseNotificationMessageEventParser
- [ ] QuizDataMessageEventParser
- [ ] QuizResultsMessageEventParser

### hotlooks (x1)
- [ ] HotLooksMessageEventParser

### ingame (x2)
- [ ] Game2FullGameStatusMessageEventParser
- [ ] Game2GameStatusMessageEventParser

### landingview (x1)
- [ ] PromoArticlesMessageEventParser

### layout (x2)
- [ ] RoomEntryTileMessageEventParser
- [ ] RoomOccupiedTilesMessageEventParser

### lobby (x4)
- [ ] AchievementResolutionCompletedMessageEventParser
- [ ] AchievementResolutionProgressMessageEventParser
- [ ] AchievementResolutionsMessageEventParser
- [ ] UserGameAchievementsMessageParser

### marketplace (x8)
- [ ] MarketplaceBuyOfferResultEventParser
- [ ] MarketplaceCancelOfferResultEventParser
- [ ] MarketplaceCanMakeOfferResultParser
- [ ] MarketplaceConfigurationEventParser
- [ ] MarketplaceItemStatsEventParser
- [ ] MarketplaceMakeOfferResultParser
- [ ] MarketPlaceOffersEventParser
- [ ] MarketPlaceOwnOffersEventParser

### moderation (x16)
- [ ] BanInfoMessageEventParser
- [ ] CfhChatlogEventParser
- [ ] IssueDeletedMessageEventParser
- [ ] IssueInfoMessageEventParser
- [ ] IssuePickFailedMessageEventParser
- [ ] ModeratorActionResultMessageEventParser
- [ ] ModeratorCautionEventParser
- [ ] ModeratorInitMessageEventParser
- [ ] ModeratorMessageEventParser
- [ ] ModeratorRoomInfoEventParser
- [ ] ModeratorToolPreferencesEventParser
- [ ] ModeratorUserInfoEventParser
- [ ] RoomChatlogEventParser
- [ ] RoomVisitsEventParser
- [ ] UserBannedMessageEventParser
- [ ] UserChatlogEventParser

### mysterybox (x4)
- [ ] CancelMysteryBoxWaitMessageEventParser
- [ ] GotMysteryBoxPrizeMessageEventParser
- [ ] MysteryBoxKeysMessageEventParser
- [ ] ShowMysteryBoxWaitMessageEventParser

### navigator (x27)
- [ ] CanCreateRoomEventEventParser
- [ ] CanCreateRoomEventParser
- [ ] CategoriesWithVisitorCountEventParser
- [ ] CompetitionRoomsDataMessageEventParser
- [ ] ConvertedRoomIdEventParser
- [ ] DoorbellMessageEventParser
- [ ] FavouriteChangedEventParser
- [ ] FavouritesEventParser
- [ ] FlatAccessDeniedMessageEventParser
- [ ] FlatCreatedEventParser
- [ ] GetGuestRoomResultEventParser
- [ ] GuestRoomSearchResultEventParser
- [ ] NavigatorCollapsedCategoriesMessageEventParser
- [ ] NavigatorLiftedRoomsEventParser
- [ ] NavigatorMetaDataEventParser
- [ ] NavigatorSavedSearchesEventParser
- [ ] NavigatorSearchResultBlocksEventParser
- [ ] NavigatorSettingsEventParser
- [ ] NewNavigatorPreferencesEventParser
- [ ] OfficialRoomsEventParser
- [ ] PopularRoomTagsResultEventParser
- [ ] RoomEventCancelEventParser
- [ ] RoomEventEventParser
- [ ] RoomInfoUpdatedEventParser
- [ ] RoomRatingEventParser
- [ ] UserEventCatsEventParser
- [ ] UserFlatCatsEventParser

### nft (x8)
- [ ] NftEmeraldConvertResultMessageEventParser
- [ ] TradeNftAssetInventoryMessageEventParser
- [ ] TradeNftAssetsMessageEventParser
- [ ] UserNftChatStylesMessageEventParser
- [ ] UserNftWardrobeMessageEventParser
- [ ] UserNftWardrobeSelectionMessageEventParser
- [ ] UserPurchasableChatStyleChangedMessageEventParser
- [ ] UserPurchasableChatStylesMessageEventParser

### notifications (x13)
- [ ] ActivityPointsMessageEventParser
- [ ] ClubGiftNotificationEventParser
- [ ] ElementPointerMessageEventParser
- [ ] HabboAchievementNotificationMessageEventParser
- [ ] HabboActivityPointNotificationMessageEventParser
- [ ] HabboBroadcastMessageEventParser
- [ ] InfoFeedEnableMessageEventParser
- [ ] MOTDNotificationEventParser
- [ ] NotificationDialogMessageEventParser
- [ ] OfferRewardDeliveredMessageEventParser
- [ ] PetLevelNotificationEventParser
- [ ] RestoreClientMessageEventParser
- [ ] UnseenItemsEventParser

### nux (x3)
- [ ] NewUserExperienceGiftOfferEventParser
- [ ] NewUserExperienceNotCompleteEventParser
- [ ] SelectInitialRoomEventParser

### perk (x2)
- [ ] CitizenshipVipOfferPromoEnabledEventParser
- [ ] PerkAllowancesMessageEventParser

### permissions (x3)
- [ ] YouAreControllerMessageEventParser
- [ ] YouAreNotControllerMessageEventParser
- [ ] YouAreOwnerMessageEventParser

### pets (x18)
- [ ] ConfirmBreedingRequestEventParser
- [ ] ConfirmBreedingResultEventParser
- [ ] GoToBreedingNestFailureEventParser
- [ ] NestBreedingSuccessEventParser
- [ ] PetAddedToInventoryEventParser
- [ ] PetBreedingEventParser
- [ ] PetBreedingResultEventParser
- [ ] PetCommandsMessageEventParser
- [ ] PetExperienceEventParser
- [ ] PetFigureUpdateEventParser
- [ ] PetInfoMessageEventParser
- [ ] PetInventoryEventParser
- [ ] PetLevelUpdateEventParser
- [ ] PetPlacingErrorEventParser
- [ ] PetReceivedMessageEventParser
- [ ] PetRemovedFromInventoryEventParser
- [ ] PetRespectFailedEventParser
- [ ] PetStatusUpdateEventParser

### poll (x6)
- [ ] PollContentsEventParser
- [ ] PollErrorEventParser
- [ ] PollOfferEventParser
- [ ] QuestionAnsweredEventParser
- [ ] QuestionEventParser
- [ ] QuestionFinishedEventParser

### preferences (x1)
- [ ] AccountPreferencesEventParser

### purse (x1)
- [ ] CreditBalanceEventParser

### quest (x13)
- [ ] CommunityGoalHallOfFameMessageEventParser
- [ ] CommunityGoalProgressMessageEventParser
- [ ] ConcurrentUsersGoalProgressMessageEventParser
- [ ] DailyTasksActiveListMessageEventParser
- [ ] DailyTasksTasksAddedMessageEventParser
- [ ] DailyTasksTaskUpdateMessageEventParser
- [ ] EpicPopupMessageEventParser
- [ ] QuestCancelledMessageEventParser
- [ ] QuestCompletedMessageEventParser
- [ ] QuestDailyMessageEventParser
- [ ] QuestMessageEventParser
- [ ] QuestsMessageEventParser
- [ ] SeasonalQuestsMessageEventParser

### roomsettings (x12)
- [ ] BannedUsersFromRoomEventParser
- [ ] FlatControllerAddedEventParser
- [ ] FlatControllerRemovedEventParser
- [ ] FlatControllersEventParser
- [ ] MuteAllInRoomEventParser
- [ ] NoSuchFlatEventParser
- [ ] RoomSettingsDataEventParser
- [ ] RoomSettingsErrorEventParser
- [ ] RoomSettingsSavedEventParser
- [ ] RoomSettingsSaveErrorEventParser
- [ ] ShowEnforceRoomCategoryDialogEventParser
- [ ] UserUnbannedFromRoomEventParser

### score (x4)
- [ ] Game2GroupLeaderboardParser
- [ ] Game2LeaderboardParser
- [ ] Game2WeeklyGroupLeaderboardParser
- [ ] Game2WeeklyLeaderboardParser

### session (x12)
- [ ] CantConnectMessageEventParser
- [ ] CloseConnectionMessageEventParser
- [ ] FlatAccessibleMessageEventParser
- [ ] GamePlayerValueMessageEventParser
- [ ] HanditemConfigurationMessageEventParser
- [ ] OpenConnectionMessageEventParser
- [ ] RoomForwardMessageEventParser
- [ ] RoomQueueStatusMessageEventParser
- [ ] RoomReadyMessageEventParser
- [ ] YouAreNotSpectatorMessageEventParser
- [ ] YouArePlayingGameMessageEventParser
- [ ] YouAreSpectatorMessageEventParser

### sound (x8)
- [ ] JukeboxPlayListFullMessageEventParser
- [ ] JukeboxSongDisksMessageEventParser
- [ ] NowPlayingMessageEventParser
- [ ] OfficialSongIdMessageEventParser
- [ ] PlayListMessageEventParser
- [ ] PlayListSongAddedMessageEventParser
- [ ] TraxSongInfoMessageEventParser
- [ ] UserSongDisksInventoryMessageEventParser

### talent (x3)
- [ ] TalentLevelUpMessageEventParser
- [ ] TalentTrackLevelMessageEventParser
- [ ] TalentTrackMessageEventParser

### tracking (x1)
- [ ] LatencyPingResponseMessageEventParser

### trading (x12)
- [ ] TradeOpenFailedEventParser
- [ ] TradeSilverFeeMessageEventParser
- [ ] TradeSilverSetMessageEventParser
- [ ] TradingAcceptEventParser
- [ ] TradingCloseEventParser
- [ ] TradingCompletedEventParser
- [ ] TradingConfirmationEventParser
- [ ] TradingItemListEventParser
- [ ] TradingNotOpenEventParser
- [ ] TradingOpenEventParser
- [ ] TradingOtherNotAllowedEventParser
- [ ] TradingYouAreNotAllowedEventParser

### treasurehunt (x3)
- [ ] TreasureHuntFailMessageEventParser
- [ ] TreasureHuntFirstWinnerMessageEventParser
- [ ] TreasureHuntUpdateMessageEventParser

### userclassification (x1)
- [ ] UserClassificationMessageEventParser

### userdefinedroomevents (x12)
- [ ] OpenEventParser
- [ ] WiredClickUserResponseEventParser
- [ ] WiredEnvironmentMessageEventParser
- [ ] WiredFurniActionEventParser
- [ ] WiredFurniAddonEventParser
- [ ] WiredFurniConditionEventParser
- [ ] WiredFurniSelectorEventParser
- [ ] WiredFurniTriggerEventParser
- [ ] WiredFurniVariableEventParser
- [ ] WiredRewardResultMessageEventParser
- [ ] WiredSaveSuccessEventParser
- [ ] WiredValidationErrorEventParser

### users (x36)
- [ ] AccountSafetyLockStatusChangeMessageEventParser
- [ ] BlockListMessageEventParser
- [ ] BlockUserUpdateMessageEventParser
- [ ] ChangeEmailResultEventParser
- [ ] EmailStatusResultEventParser
- [ ] ExtendedProfileChangedMessageEventParser
- [ ] ExtendedProfileMessageEventParser
- [ ] GroupDetailsChangedMessageEventParser
- [ ] GroupMembershipRequestedMessageEventParser
- [ ] GuildCreatedMessageEventParser
- [ ] GuildCreationInfoMessageEventParser
- [ ] GuildEditFailedMessageEventParser
- [ ] GuildEditInfoMessageEventParser
- [ ] GuildEditorDataMessageEventParser
- [ ] GuildMemberFurniCountInHQMessageEventParser
- [ ] GuildMemberMgmtFailedMessageEventParser
- [ ] GuildMembershipRejectedMessageEventParser
- [ ] GuildMembershipsMessageEventParser
- [ ] GuildMembershipUpdatedMessageEventParser
- [ ] GuildMembersMessageEventParser
- [ ] HabboGroupBadgesMessageEventParser
- [ ] HabboGroupDeactivatedMessageEventParser
- [ ] HabboGroupDetailsMessageEventParser
- [ ] HabboGroupJoinFailedMessageEventParser
- [ ] HabboUserBadgesMessageEventParser
- [ ] HandItemReceivedMessageEventParser
- [ ] IgnoredUsersMessageEventParser
- [ ] IgnoreResultMessageEventParser
- [ ] InClientLinkMessageEventParser
- [ ] PetRespectNotificationEventParser
- [ ] PetSupplementedNotificationEventParser
- [ ] RelationshipStatusInfoEventParser
- [ ] RespectNotificationMessageEventParser
- [ ] ScrSendKickbackInfoMessageEventParser
- [ ] ScrSendUserInfoEventParser
- [ ] UserNameChangedMessageEventParser

### variablesmanagement (x3)
- [ ] WiredSetUserPermanentVariableResultEventParser
- [ ] WiredUserPermanentVariablesEventParser
- [ ] WiredUserVariablesListEventParser

### vault (x2)
- [ ] IncomeRewardClaimResponseMessageEventParser
- [ ] IncomeRewardNotificationMessageEventParser

### votes (x1)
- [ ] CommunityVoteReceivedEventParser

### wiredmenu (x10)
- [ ] WiredAllVariableHoldersEventParser
- [ ] WiredAllVariablesDiffsEventParser
- [ ] WiredAllVariablesHashEventParser
- [ ] WiredErrorLogsEventParser
- [ ] WiredMenuErrorEventParser
- [ ] WiredPermissionsEventParser
- [ ] WiredRoomLogsEventParser
- [ ] WiredRoomSettingsEventParser
- [ ] WiredRoomStatsEventParser
- [ ] WiredVariablesForObjectEventParser

## Revision: serializers C# manquants
- Total: 498
### achievements (x1)
- [ ] GetAchievementsComposer

### action (x10)
- [ ] AmbassadorAlertMessageComposer
- [ ] AssignRightsMessageComposer
- [ ] BanUserWithDurationMessageComposer
- [ ] KickUserMessageComposer
- [ ] LetUserInMessageComposer
- [ ] MuteUserMessageComposer
- [ ] RemoveAllRightsMessageComposer
- [ ] RemoveRightsMessageComposer
- [ ] UnbanUserFromRoomMessageComposer
- [ ] UnmuteUserMessageComposer

### advertisement (x2)
- [ ] GetInterstitialMessageComposer
- [ ] InterstitialShownMessageComposer

### arena (x4)
- [ ] Game2ExitGameMessageComposer
- [ ] Game2GameChatMessageComposer
- [ ] Game2LoadStageReadyMessageComposer
- [ ] Game2PlayAgainMessageComposer

### avatar (x14)
- [ ] AvatarExpressionMessageComposer
- [ ] ChangeMottoMessageComposer
- [ ] ChangePostureMessageComposer
- [ ] ChangeUserNameInRoomMessageComposer
- [ ] ChangeUserNameMessageComposer
- [ ] CheckUserNameMessageComposer
- [ ] CustomizeAvatarWithFurniMessageComposer
- [ ] DropCarryItemMessageComposer
- [ ] GetWardrobeMessageComposer
- [ ] LookToMessageComposer
- [ ] PassCarryItemMessageComposer
- [ ] PassCarryItemToPetMessageComposer
- [ ] SaveWardrobeOutfitMessageComposer
- [ ] SignMessageComposer

### badges (x5)
- [ ] GetBadgePointLimitsComposer
- [ ] GetBadgesComposer
- [ ] GetIsBadgeRequestFulfilledComposer
- [ ] RequestABadgeComposer
- [ ] SetActivatedBadgesComposer

### bots (x3)
- [ ] CommandBotComposer
- [ ] GetBotCommandConfigurationDataComposer
- [ ] GetBotInventoryComposer

### camera (x6)
- [ ] PhotoCompetitionMessageComposer
- [ ] PublishPhotoMessageComposer
- [ ] PurchasePhotoMessageComposer
- [ ] RenderRoomMessageComposer
- [ ] RenderRoomThumbnailMessageComposer
- [ ] RequestCameraConfigurationMessageComposer

### campaign (x2)
- [ ] OpenCampaignCalendarDoorAsStaffComposer
- [ ] OpenCampaignCalendarDoorComposer

### catalog (x36)
- [ ] BuildersClubPlaceRoomItemMessageComposer
- [ ] BuildersClubPlaceWallItemMessageComposer
- [ ] BuildersClubQueryFurniCountMessageComposer
- [ ] GetBonusRareInfoMessageComposer
- [ ] GetBundleDiscountRulesetComposer
- [ ] GetCatalogIndexComposer
- [ ] GetCatalogPageComposer
- [ ] GetCatalogPageWithEarliestExpiryComposer
- [ ] GetClubGiftMessageComposer
- [ ] GetClubOffersMessageComposer
- [ ] GetGiftWrappingConfigurationComposer
- [ ] GetHabboClubExtendOfferMessageComposer
- [ ] GetIsOfferGiftableComposer
- [ ] GetLimitedOfferAppearingNextComposer
- [ ] GetNextTargetedOfferComposer
- [ ] GetProductOfferComposer
- [ ] GetRecyclerPrizesMessageComposer
- [ ] GetRecyclerStatusMessageComposer
- [ ] GetRoomAdPurchaseInfoComposer
- [ ] GetSeasonalCalendarDailyComposer
- [ ] GetSellablePetPalettesComposer
- [ ] GetSnowWarGameTokensOfferComposer
- [ ] MarkCatalogNewAdditionsPageOpenedComposer
- [ ] PurchaseBasicMembershipExtensionComposer
- [ ] PurchaseFromCatalogAsGiftComposer
- [ ] PurchaseFromCatalogComposer
- [ ] PurchaseRoomAdMessageComposer
- [ ] PurchaseSnowWarGameTokensOfferComposer
- [ ] PurchaseTargetedOfferComposer
- [ ] PurchaseVipMembershipExtensionComposer
- [ ] RecycleItemsMessageComposer
- [ ] RedeemVoucherMessageComposer
- [ ] RoomAdPurchaseInitiatedComposer
- [ ] SelectClubGiftComposer
- [ ] SetTargetedOfferStateComposer
- [ ] ShopTargetedOfferViewedComposer

### chat (x2)
- [ ] CancelTypingMessageComposer
- [ ] StartTypingMessageComposer

### collectibles (x17)
- [ ] ClaimNftClaimsMessageComposer
- [ ] GetCollectibleMintableItemTypesMessageComposer
- [ ] GetCollectibleMintingEnabledMessageComposer
- [ ] GetCollectibleMintTokensMessageComposer
- [ ] GetCollectibleWalletAddressesMessageComposer
- [ ] GetCollectorScoreMessageComposer
- [ ] GetMintTokenOffersMessageComposer
- [ ] GetNftClaimsMessageComposer
- [ ] GetNftCollectionsMessageComposer
- [ ] GetNftStoreOffersMessageComposer
- [ ] GetNftTransferFeeMessageComposer
- [ ] MintItemMessageComposer
- [ ] NftCollectiblesClaimBonusItemMessageComposer
- [ ] NftCollectiblesClaimRewardItemMessageComposer
- [ ] NftStorePurchaseMessageComposer
- [ ] NftTransferAssetsMessageComposer
- [ ] PurchaseMintTokenMessageComposer

### competition (x9)
- [ ] ForwardToACompetitionRoomMessageComposer
- [ ] ForwardToASubmittableRoomMessageComposer
- [ ] ForwardToRandomCompetitionRoomMessageComposer
- [ ] GetCurrentTimingCodeMessageComposer
- [ ] GetIsUserPartOfCompetitionMessageComposer
- [ ] GetSecondsUntilMessageComposer
- [ ] RoomCompetitionInitMessageComposer
- [ ] SubmitRoomToCompetitionMessageComposer
- [ ] VoteForRoomMessageComposer

### crafting (x5)
- [ ] CraftComposer
- [ ] CraftSecretComposer
- [ ] GetCraftableProductsComposer
- [ ] GetCraftingRecipeComposer
- [ ] GetCraftingRecipesAvailableComposer

### customfilter (x3)
- [ ] AddToCustomFilterMessageComposer
- [ ] GetCustomFilterMessageComposer
- [ ] RemoveFromCustomFilterMessageComposer

### directory (x5)
- [ ] Game2CheckGameDirectoryStatusMessageComposer
- [ ] Game2GetAccountGameStatusMessageComposer
- [ ] Game2LeaveGameMessageComposer
- [ ] Game2QuickJoinGameMessageComposer
- [ ] Game2StartSnowWarMessageComposer

### engine (x27)
- [ ] ClickFurniMessageComposer
- [ ] CompostPlantMessageComposer
- [ ] GetFurnitureAliasesMessageComposer
- [ ] GetItemDataMessageComposer
- [ ] GetPetCommandsMessageComposer
- [ ] GiveSupplementToPetMessageComposer
- [ ] HarvestPetMessageComposer
- [ ] MountPetMessageComposer
- [ ] MoveAvatarMessageComposer
- [ ] MoveObjectMessageComposer
- [ ] MovePetMessageComposer
- [ ] MoveWallItemMessageComposer
- [ ] PickupObjectMessageComposer
- [ ] PlaceBotMessageComposer
- [ ] PlaceObjectMessageComposer
- [ ] PlacePetMessageComposer
- [ ] RemoveBotFromFlatMessageComposer
- [ ] RemoveItemMessageComposer
- [ ] RemovePetFromFlatMessageComposer
- [ ] RemoveSaddleFromPetMessageComposer
- [ ] SetClothingChangeDataMessageComposer
- [ ] SetItemDataMessageComposer
- [ ] SetObjectDataMessageComposer
- [ ] TogglePetBreedingPermissionMessageComposer
- [ ] TogglePetRidingPermissionMessageComposer
- [ ] UseFurnitureMessageComposer
- [ ] UseWallItemMessageComposer

### friendfurni (x1)
- [ ] FriendFurniConfirmLockMessageComposer

### friendlist (x13)
- [ ] AcceptFriendMessageComposer
- [ ] DeclineFriendMessageComposer
- [ ] FindNewFriendsMessageComposer
- [ ] FollowFriendMessageComposer
- [ ] GetFriendRequestsMessageComposer
- [ ] GetMessengerHistoryComposer
- [ ] HabboSearchMessageComposer
- [ ] RemoveFriendMessageComposer
- [ ] RequestFriendMessageComposer
- [ ] SendMsgMessageComposer
- [ ] SendRoomInviteMessageComposer
- [ ] SetRelationshipStatusMessageComposer
- [ ] VisitUserMessageComposer

### furni (x2)
- [ ] RequestFurniInventoryComposer
- [ ] RequestFurniInventoryWhenNotInRoomComposer

### furniture (x28)
- [ ] AddSpamWallPostItMessageComposer
- [ ] ControlYoutubeDisplayPlaybackMessageComposer
- [ ] CreditFurniRedeemMessageComposer
- [ ] DiceOffMessageComposer
- [ ] EnterOneWayDoorMessageComposer
- [ ] ExtendRentOrBuyoutFurniMessageComposer
- [ ] ExtendRentOrBuyoutStripItemMessageComposer
- [ ] GetGuildFurniContextMenuInfoMessageComposer
- [ ] GetRentOrBuyoutOfferMessageComposer
- [ ] GetYoutubeDisplayStatusMessageComposer
- [ ] OpenMysteryTrophyMessageComposer
- [ ] OpenPetPackageMessageComposer
- [ ] PlacePostItMessageComposer
- [ ] PresentOpenMessageComposer
- [ ] RentableSpaceCancelRentMessageComposer
- [ ] RentableSpaceRentMessageComposer
- [ ] RoomDimmerChangeStateMessageComposer
- [ ] RoomDimmerGetPresetsMessageComposer
- [ ] RoomDimmerSavePresetMessageComposer
- [ ] SetAreaHideDataComposer
- [ ] SetCustomStackingHeightComposer
- [ ] SetMannequinFigureComposer
- [ ] SetMannequinNameComposer
- [ ] SetRandomStateMessageComposer
- [ ] SetRoomBackgroundColorDataComposer
- [ ] SetYoutubeDisplayPlaylistMessageComposer
- [ ] SpinWheelOfFortuneMessageComposer
- [ ] ThrowDiceMessageComposer

### gifts (x4)
- [ ] ResetPhoneNumberStateMessageComposer
- [ ] SetPhoneNumberVerificationStatusMessageComposer
- [ ] TryPhoneNumberMessageComposer
- [ ] VerifyCodeMessageComposer

### groupforums (x10)
- [ ] GetForumsListMessageComposer
- [ ] GetForumStatsMessageComposer
- [ ] GetMessagesMessageComposer
- [ ] GetThreadMessageComposer
- [ ] GetThreadsMessageComposer
- [ ] GetUnreadForumsCountMessageComposer
- [ ] ModerateMessageMessageComposer
- [ ] ModerateThreadMessageComposer
- [ ] UpdateForumReadMarkerMessageComposer
- [ ] UpdateForumSettingsMessageComposer

### handshake (x7)
- [ ] ClientHelloMessageComposer
- [ ] DisconnectMessageComposer
- [ ] InfoRetrieveMessageComposer
- [ ] PongMessageComposer
- [ ] SSOTicketMessageComposer
- [ ] UniqueIDMessageComposer
- [ ] VersionCheckMessageComposer

### help (x27)
- [ ] AppealCfhMessageComposer
- [ ] CallForHelpFromForumMessageMessageComposer
- [ ] CallForHelpFromForumThreadMessageComposer
- [ ] CallForHelpFromIMMessageComposer
- [ ] CallForHelpFromPhotoMessageComposer
- [ ] CallForHelpFromSelfieMessageComposer
- [ ] CallForHelpMessageComposer
- [ ] ChatReviewGuideDecidesOnOfferMessageComposer
- [ ] ChatReviewGuideDetachedMessageComposer
- [ ] ChatReviewGuideVoteMessageComposer
- [ ] ChatReviewSessionCreateMessageComposer
- [ ] DeletePendingCallsForHelpMessageComposer
- [ ] GetCfhStatusMessageComposer
- [ ] GetGuideReportingStatusMessageComposer
- [ ] GetPendingCallsForHelpMessageComposer
- [ ] GetQuizQuestionsComposer
- [ ] GuideSessionCreateMessageComposer
- [ ] GuideSessionFeedbackMessageComposer
- [ ] GuideSessionGetRequesterRoomMessageComposer
- [ ] GuideSessionGuideDecidesMessageComposer
- [ ] GuideSessionInviteRequesterMessageComposer
- [ ] GuideSessionIsTypingMessageComposer
- [ ] GuideSessionOnDutyUpdateMessageComposer
- [ ] GuideSessionReportMessageComposer
- [ ] GuideSessionRequesterCancelsMessageComposer
- [ ] GuideSessionResolvedMessageComposer
- [ ] PostQuizAnswersComposer

### hotlooks (x1)
- [ ] GetHotLooksMessageComposer

### ingame (x5)
- [ ] Game2MakeSnowballMessageComposer
- [ ] Game2RequestFullStatusUpdateMessageComposer
- [ ] Game2SetUserMoveTargetMessageComposer
- [ ] Game2ThrowSnowballAtHumanMessageComposer
- [ ] Game2ThrowSnowballAtPositionMessageComposer

### landingview (x1)
- [ ] GetPromoArticlesMessageComposer

### layout (x3)
- [ ] GetOccupiedTilesMessageComposer
- [ ] GetRoomEntryTileMessageComposer
- [ ] UpdateFloorPropertiesMessageComposer

### lobby (x2)
- [ ] GetResolutionAchievementsMessageComposer
- [ ] GetUserGameAchievementsMessageComposer

### marketplace (x10)
- [ ] BuyMarketplaceOfferMessageComposer
- [ ] BuyMarketplaceTokensMessageComposer
- [ ] CancelMarketplaceOfferMessageComposer
- [ ] GetMarketplaceCanMakeOfferMessageComposer
- [ ] GetMarketplaceConfigurationMessageComposer
- [ ] GetMarketplaceItemStatsComposer
- [ ] GetMarketplaceOffersMessageComposer
- [ ] GetMarketplaceOwnOffersMessageComposer
- [ ] MakeOfferMessageComposer
- [ ] RedeemMarketplaceOfferCreditsMessageComposer

### moderator (x21)
- [ ] CloseIssueDefaultActionMessageComposer
- [ ] CloseIssuesMessageComposer
- [ ] DefaultSanctionMessageComposer
- [ ] GetCfhChatlogMessageComposer
- [ ] GetModeratorRoomInfoMessageComposer
- [ ] GetModeratorUserInfoMessageComposer
- [ ] GetRoomChatlogMessageComposer
- [ ] GetRoomVisitsMessageComposer
- [ ] GetUserChatlogMessageComposer
- [ ] ModAlertMessageComposer
- [ ] ModBanMessageComposer
- [ ] ModerateRoomMessageComposer
- [ ] ModeratorActionMessageComposer
- [ ] ModKickMessageComposer
- [ ] ModMessageMessageComposer
- [ ] ModMuteMessageComposer
- [ ] ModToolPreferencesComposer
- [ ] ModToolSanctionComposer
- [ ] ModTradingLockMessageComposer
- [ ] PickIssuesMessageComposer
- [ ] ReleaseIssuesMessageComposer

### mysterybox (x1)
- [ ] MysteryBoxWaitingCanceledMessageComposer

### navigator (x35)
- [ ] AddFavouriteRoomMessageComposer
- [ ] CancelEventMessageComposer
- [ ] CompetitionRoomsSearchMessageComposer
- [ ] ConvertGlobalRoomIdMessageComposer
- [ ] CreateFlatMessageComposer
- [ ] DeleteFavouriteRoomMessageComposer
- [ ] EditEventMessageComposer
- [ ] ForwardToARandomPromotedRoomMessageComposer
- [ ] ForwardToSomeRoomMessageComposer
- [ ] GetGuestRoomMessageComposer
- [ ] GetOfficialRoomsMessageComposer
- [ ] GetPopularRoomTagsMessageComposer
- [ ] GetUserEventCatsMessageComposer
- [ ] GetUserFlatCatsMessageComposer
- [ ] GuildBaseSearchMessageComposer
- [ ] MyFavouriteRoomsSearchMessageComposer
- [ ] MyFrequentRoomHistorySearchMessageComposer
- [ ] MyFriendsRoomsSearchMessageComposer
- [ ] MyGuildBasesSearchMessageComposer
- [ ] MyRecommendedRoomsMessageComposer
- [ ] MyRoomHistorySearchMessageComposer
- [ ] MyRoomRightsSearchMessageComposer
- [ ] MyRoomsSearchMessageComposer
- [ ] PopularRoomsSearchMessageComposer
- [ ] RateFlatMessageComposer
- [ ] RemoveOwnRoomRightsRoomMessageComposer
- [ ] RoomAdEventTabAdClickedComposer
- [ ] RoomAdEventTabViewedComposer
- [ ] RoomAdSearchMessageComposer
- [ ] RoomsWhereMyFriendsAreSearchMessageComposer
- [ ] RoomsWithHighestScoreSearchMessageComposer
- [ ] RoomTextSearchMessageComposer
- [ ] SetRoomSessionTagsMessageComposer
- [ ] ToggleStaffPickMessageComposer
- [ ] UpdateHomeRoomMessageComposer

### newnavigator (x7)
- [ ] NavigatorAddCollapsedCategoryMessageComposer
- [ ] NavigatorAddSavedSearchComposer
- [ ] NavigatorDeleteSavedSearchComposer
- [ ] NavigatorRemoveCollapsedCategoryMessageComposer
- [ ] NavigatorSetSearchCodeViewModeMessageComposer
- [ ] NewNavigatorInitComposer
- [ ] NewNavigatorSearchComposer

### nft (x8)
- [ ] AddNftToTradeComposer
- [ ] GetNftCreditsMessageComposer
- [ ] GetNftTradeInventoryComposer
- [ ] GetSelectedNftWardrobeOutfitMessageComposer
- [ ] GetSilverMessageComposer
- [ ] GetUserNftWardrobeMessageComposer
- [ ] RemoveNftFromTradeComposer
- [ ] SaveUserNftWardrobeMessageComposer

### notifications (x2)
- [ ] ResetUnseenItemIdsComposer
- [ ] ResetUnseenItemsComposer

### nux (x2)
- [ ] NewUserExperienceGetGiftsMessageComposer
- [ ] NewUserExperienceScriptProceedComposer

### pets (x8)
- [ ] BreedPetsMessageComposer
- [ ] CancelPetBreedingComposer
- [ ] ConfirmPetBreedingComposer
- [ ] CustomizePetWithFurniComposer
- [ ] GetPetInfoMessageComposer
- [ ] GetPetInventoryComposer
- [ ] PetSelectedMessageComposer
- [ ] RespectPetMessageComposer

### poll (x3)
- [ ] PollAnswerComposer
- [ ] PollRejectComposer
- [ ] PollStartComposer

### preferences (x7)
- [ ] SetChatPreferencesMessageComposer
- [ ] SetChatStylePreferenceComposer
- [ ] SetIgnoreRoomInvitesMessageComposer
- [ ] SetNewNavigatorWindowPreferencesMessageComposer
- [ ] SetRoomCameraPreferencesMessageComposer
- [ ] SetSoundSettingsComposer
- [ ] SetUIFlagsMessageComposer

### purse (x1)
- [ ] GetCreditsInfoComposer

### quest (x16)
- [ ] AcceptQuestMessageComposer
- [ ] ActivateQuestMessageComposer
- [ ] CancelQuestMessageComposer
- [ ] ClaimDailyTaskComposer
- [ ] FriendRequestQuestCompleteMessageComposer
- [ ] GetCommunityGoalHallOfFameMessageComposer
- [ ] GetCommunityGoalProgressMessageComposer
- [ ] GetConcurrentUsersGoalProgressMessageComposer
- [ ] GetConcurrentUsersRewardMessageComposer
- [ ] GetDailyQuestMessageComposer
- [ ] GetDailyTasksComposer
- [ ] GetQuestsMessageComposer
- [ ] GetSeasonalQuestsOnlyMessageComposer
- [ ] OpenQuestTrackerMessageComposer
- [ ] RejectQuestMessageComposer
- [ ] StartCampaignMessageComposer

### register (x1)
- [ ] UpdateFigureDataMessageComposer

### roomdirectory (x1)
- [ ] RoomNetworkOpenConnectionMessageComposer

### roomsettings (x8)
- [ ] DeleteRoomMessageComposer
- [ ] GetBannedUsersFromRoomMessageComposer
- [ ] GetCustomRoomFilterMessageComposer
- [ ] GetFlatControllersMessageComposer
- [ ] GetRoomSettingsMessageComposer
- [ ] SaveRoomSettingsMessageComposer
- [ ] UpdateRoomCategoryAndTradeSettingsComposer
- [ ] UpdateRoomFilterMessageComposer

### score (x10)
- [ ] Game2GetFriendsLeaderboardComposer
- [ ] Game2GetTotalGroupLeaderboardComposer
- [ ] Game2GetTotalLeaderboardComposer
- [ ] Game2GetWeeklyFriendsLeaderboardComposer
- [ ] Game2GetWeeklyGroupLeaderboardComposer
- [ ] Game2GetWeeklyLeaderboardComposer
- [ ] GetFriendsWeeklyCompetitiveLeaderboardComposer
- [ ] GetWeeklyCompetitiveLeaderboardComposer
- [ ] GetWeeklyGameRewardComposer
- [ ] GetWeeklyGameRewardWinnersComposer

### session (x3)
- [ ] ChangeQueueMessageComposer
- [ ] OpenFlatConnectionMessageComposer
- [ ] QuitMessageComposer

### sound (x9)
- [ ] AddJukeboxDiskComposer
- [ ] GetJukeboxPlayListMessageComposer
- [ ] GetNowPlayingMessageComposer
- [ ] GetOfficialSongIdMessageComposer
- [ ] GetSongInfoMessageComposer
- [ ] GetSoundMachinePlayListMessageComposer
- [ ] GetSoundSettingsComposer
- [ ] GetUserSongDisksMessageComposer
- [ ] RemoveJukeboxDiskComposer

### talent (x3)
- [ ] GetTalentTrackLevelMessageComposer
- [ ] GetTalentTrackMessageComposer
- [ ] GuideAdvertisementReadMessageComposer

### tracking (x5)
- [ ] EventLogMessageComposer
- [ ] LagWarningReportMessageComposer
- [ ] LatencyPingReportMessageComposer
- [ ] LatencyPingRequestMessageComposer
- [ ] PerformanceLogMessageComposer

### trading (x10)
- [ ] AcceptTradingComposer
- [ ] AddItemsToTradeComposer
- [ ] AddItemToTradeComposer
- [ ] CloseTradingComposer
- [ ] ConfirmAcceptTradingComposer
- [ ] ConfirmDeclineTradingComposer
- [ ] OpenTradingComposer
- [ ] RemoveItemFromTradeComposer
- [ ] SilverFeeMessageComposer
- [ ] UnacceptTradingComposer

### treasurehunt (x1)
- [ ] ProgressTreasureHuntMessageComposer

### userclassification (x2)
- [ ] PeerUsersClassificationMessageComposer
- [ ] RoomUsersClassificationMessageComposer

### userdefinedroomevents (x8)
- [ ] ApplySnapshotMessageComposer
- [ ] UpdateActionMessageComposer
- [ ] UpdateAddonMessageComposer
- [ ] UpdateConditionMessageComposer
- [ ] UpdateSelectorMessageComposer
- [ ] UpdateTriggerMessageComposer
- [ ] UpdateVariableMessageComposer
- [ ] WiredClickUserMessageComposer

### users (x42)
- [ ] AddAdminRightsToMemberMessageComposer
- [ ] ApproveAllMembershipRequestsMessageComposer
- [ ] ApproveMembershipRequestMessageComposer
- [ ] BlockListInitComposer
- [ ] BlockUserMessageComposer
- [ ] ChangeEmailComposer
- [ ] CreateGuildMessageComposer
- [ ] DeactivateGuildMessageComposer
- [ ] DeselectFavouriteHabboGroupMessageComposer
- [ ] GetEmailStatusComposer
- [ ] GetExtendedProfileByNameMessageComposer
- [ ] GetExtendedProfileMessageComposer
- [ ] GetGuildCreationInfoMessageComposer
- [ ] GetGuildEditInfoMessageComposer
- [ ] GetGuildEditorDataMessageComposer
- [ ] GetGuildMembershipsMessageComposer
- [ ] GetGuildMembersMessageComposer
- [ ] GetHabboGroupBadgesMessageComposer
- [ ] GetHabboGroupDetailsMessageComposer
- [ ] GetIgnoredUsersMessageComposer
- [ ] GetMemberGuildItemCountMessageComposer
- [ ] GetMOTDMessageComposer
- [ ] GetRelationshipStatusInfoMessageComposer
- [ ] GetSelectedBadgesMessageComposer
- [ ] GetUserNftChatStylesMessageComposer
- [ ] IgnoreUserMessageComposer
- [ ] JoinHabboGroupMessageComposer
- [ ] KickMemberMessageComposer
- [ ] RejectMembershipRequestMessageComposer
- [ ] RemoveAdminRightsFromMemberMessageComposer
- [ ] ReplenishRespectMessageComposer
- [ ] RespectUserMessageComposer
- [ ] ScrGetKickbackInfoMessageComposer
- [ ] ScrGetUserInfoMessageComposer
- [ ] SelectFavouriteHabboGroupMessageComposer
- [ ] UnblockGroupMemberMessageComposer
- [ ] UnblockUserMessageComposer
- [ ] UnignoreUserMessageComposer
- [ ] UpdateGuildBadgeMessageComposer
- [ ] UpdateGuildColorsMessageComposer
- [ ] UpdateGuildIdentityMessageComposer
- [ ] UpdateGuildSettingsMessageComposer

### variablesmanagement (x3)
- [ ] WiredGetUserPermanentVariablesComposer
- [ ] WiredGetVariableOwnersPageComposer
- [ ] WiredSetUserPermanentVariableComposer

### vault (x2)
- [ ] IncomeRewardClaimMessageComposer
- [ ] WithdrawCreditVaultMessageComposer

### votes (x1)
- [ ] CommunityGoalVoteMessageComposer

### wiredmenu (x13)
- [ ] WiredClearErrorLogsMessageComposer
- [ ] WiredGetAllVariableHoldersMessageComposer
- [ ] WiredGetAllVariablesDiffsMessageComposer
- [ ] WiredGetAllVariablesHashMessageComposer
- [ ] WiredGetErrorLogsMessageComposer
- [ ] WiredGetRoomLogsComposer
- [ ] WiredGetRoomSettingsMessageComposer
- [ ] WiredGetRoomStatsMessageComposer
- [ ] WiredGetVariablesForObjectMessageComposer
- [ ] WiredSetObjectVariableValueMessageComposer
- [ ] WiredSetPreferencesMessageComposer
- [ ] WiredSetRoomSettingsMessageComposer
- [ ] WiredUpdateRoomComposer

## Notes
- Names are compared with suffix normalization: MessageEvent / MessageComposer / Parser / Serializer.
- AS3 parser class_*.as files are not covered.
- This doc is made for execution; manually verify false positives afterward.

