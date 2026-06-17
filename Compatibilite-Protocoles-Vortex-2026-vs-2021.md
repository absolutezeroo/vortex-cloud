# Audit protocolaire WIN63-2026 vs stack Vortex (2021)

Date: 2026-06-16 15:56:36
Source: WIN63-202601121721-391685409-source-master

## Resume de couverture
- Source incoming AS3: 349
- Source outgoing AS3: 519
- Source parsers AS3: 532
- Headers MessageEvent en revision: 519
- Headers MessageComposer en revision: 533
- Parsers C# en revision: 501
- Serializers C# en revision: 543
- Incoming C# (Primitives): 501
- Outgoing C# (Primitives): 513
- Gap incoming (source -> c#): 349
- Gap outgoing (source -> c#): 499

## P0 - Critique
- Missing incoming dans emu: 349
- Missing outgoing dans emu: 499
- Missing revision Event headers: 336
- Missing revision Composer headers: 497
- Missing revision parsers: 522
- Missing revision serializers: 500

### Missing incoming AS3 (source) absent de Turbo.Primitives.Messages.Incoming
#### Domain action
- [ ] AvatarEffectMessageEvent
- [ ] CarryObjectMessageEvent
- [ ] DanceMessageEvent
- [ ] ExpressionMessageEvent
- [ ] SleepMessageEvent
- [ ] UseObjectMessageEvent

#### Domain advertisement
- [ ] InterstitialMessageEvent

#### Domain arena
- [ ] Game2ArenaEnteredMessageEvent
- [ ] Game2EnterArenaMessageEvent
- [ ] Game2EnterArenaFailedMessageEvent
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

#### Domain availability
- [ ] AvailabilityStatusMessageEvent
- [ ] InfoHotelClosedMessageEvent
- [ ] InfoHotelClosingMessageEvent
- [ ] LoginFailedHotelClosedMessageEvent
- [ ] MaintenanceStatusMessageEvent

#### Domain avatar
- [ ] ChangeUserNameResultMessageEvent
- [ ] CheckUserNameResultMessageEvent
- [ ] WardrobeMessageEvent

#### Domain avatareffect
- [ ] AvatarEffectActivatedMessageEvent
- [ ] AvatarEffectAddedMessageEvent
- [ ] AvatarEffectExpiredMessageEvent
- [ ] AvatarEffectsMessageEvent
- [ ] AvatarEffectSelectedMessageEvent

#### Domain callforhelp
- [ ] CfhSanctionMessageEvent
- [ ] CfhTopicsInitMessageEvent
- [ ] MyCfhReportStatusMessageEvent

#### Domain camera
- [ ] CameraPublishStatusMessageEvent
- [ ] CameraPurchaseOKMessageEvent
- [ ] CameraStorageUrlMessageEvent
- [ ] CompetitionStatusMessageEvent
- [ ] InitCameraMessageEvent
- [ ] ThumbnailStatusMessageEvent

#### Domain campaign
- [ ] CampaignCalendarDataMessageEvent
- [ ] CampaignCalendarDoorOpenedMessageEvent

#### Domain catalog
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

#### Domain chat
- [ ] ChatMessageEvent
- [ ] FloodControlMessageEvent
- [ ] RoomChatSettingsMessageEvent
- [ ] RoomFilterSettingsMessageEvent
- [ ] ShoutMessageEvent
- [ ] UserTypingMessageEvent
- [ ] WhisperMessageEvent

#### Domain collectibles
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

#### Domain competition
- [ ] CompetitionEntrySubmitResultMessageEvent
- [ ] CompetitionVotingInfoMessageEvent
- [ ] CurrentTimingCodeMessageEvent
- [ ] IsUserPartOfCompetitionMessageEvent
- [ ] NoOwnedRoomsAlertMessageEvent
- [ ] SecondsUntilMessageEvent

#### Domain crafting
- [ ] CraftableProductsMessageEvent
- [ ] CraftingRecipeMessageEvent
- [ ] CraftingRecipesAvailableMessageEvent
- [ ] CraftingResultMessageEvent

#### Domain customfilter
- [ ] GetCustomFilterResultMessageEvent
- [ ] ModifyCustomFilterResultMessageEvent

#### Domain directory
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

#### Domain engine
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
- [ ] ObjectRemoveMessageEvent
- [ ] ObjectRemoveConfirmMessageEvent
- [ ] ObjectRemoveMultipleMessageEvent
- [ ] ObjectsMessageEvent
- [ ] ObjectsDataUpdateMessageEvent
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

#### Domain friendfurni
- [ ] FriendFurniCancelLockMessageEvent
- [ ] FriendFurniOtherLockConfirmedMessageEvent
- [ ] FriendFurniStartConfirmationMessageEvent

#### Domain friendlist
- [ ] FriendListFragmentMessageEvent
- [ ] MiniMailNewMessageEvent
- [ ] NewConsoleMessageEvent

#### Domain furniture
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
- [ ] RentableSpaceStatusMessageEvent
- [ ] RequestSpamWallPostItMessageEvent
- [ ] RoomDimmerPresetsMessageEvent
- [ ] RoomMessageNotificationMessageEvent
- [ ] YoutubeControlVideoMessageEvent
- [ ] YoutubeDisplayPlaylistsMessageEvent
- [ ] YoutubeDisplayVideoMessageEvent

#### Domain gifts
- [ ] PhoneCollectionStateMessageEvent
- [ ] TryPhoneNumberResultMessageEvent
- [ ] TryVerificationCodeResultMessageEvent

#### Domain groupforums
- [ ] ForumDataMessageEvent
- [ ] ForumsListMessageEvent
- [ ] ForumThreadsMessageEvent
- [ ] PostMessageMessageEvent
- [ ] PostThreadMessageEvent
- [ ] ThreadMessagesMessageEvent
- [ ] UnreadForumsCountMessageEvent
- [ ] UpdateMessageMessageEvent
- [ ] UpdateThreadMessageEvent

#### Domain handshake
- [ ] AuthenticationOKMessageEvent
- [ ] NoobnessLevelMessageEvent
- [ ] PingMessageEvent
- [ ] UserRightsMessageEvent

#### Domain help
- [ ] CallForHelpDisabledNotifyMessageEvent
- [ ] CallForHelpPendingCallsMessageEvent
- [ ] CallForHelpPendingCallsDeletedMessageEvent
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
- [ ] GuideSessionMessageMessageEvent
- [ ] GuideSessionPartnerIsTypingMessageEvent
- [ ] GuideSessionRequesterRoomMessageEvent
- [ ] GuideSessionStartedMessageEvent
- [ ] GuideTicketCreationResultMessageEvent
- [ ] GuideTicketResolutionMessageEvent
- [ ] IssueCloseNotificationMessageEvent
- [ ] QuizDataMessageEvent
- [ ] QuizResultsMessageEvent

#### Domain hotlooks
- [ ] HotLooksMessageEvent

#### Domain ingame
- [ ] Game2FullGameStatusMessageEvent
- [ ] Game2GameStatusMessageEvent

#### Domain landingview
- [ ] PromoArticlesMessageEvent

#### Domain layout
- [ ] RoomEntryTileMessageEvent
- [ ] RoomOccupiedTilesMessageEvent

#### Domain lobby
- [ ] AchievementResolutionCompletedMessageEvent
- [ ] AchievementResolutionProgressMessageEvent
- [ ] AchievementResolutionsMessageEvent
- [ ] UserGameAchievementsMessageEvent

#### Domain moderation
- [ ] BanInfoMessageEvent
- [ ] IssueDeletedMessageEvent
- [ ] IssueInfoMessageEvent
- [ ] IssuePickFailedMessageEvent
- [ ] ModeratorMessageEvent
- [ ] ModeratorActionResultMessageEvent
- [ ] ModeratorInitMessageEvent
- [ ] UserBannedMessageEvent

#### Domain mysterybox
- [ ] CancelMysteryBoxWaitMessageEvent
- [ ] GotMysteryBoxPrizeMessageEvent
- [ ] MysteryBoxKeysMessageEvent
- [ ] ShowMysteryBoxWaitMessageEvent

#### Domain navigator
- [ ] CompetitionRoomsDataMessageEvent
- [ ] DoorbellMessageEvent
- [ ] FlatAccessDeniedMessageEvent
- [ ] NavigatorCollapsedCategoriesMessageEvent

#### Domain nft
- [ ] NftEmeraldConvertResultMessageEvent
- [ ] TradeNftAssetInventoryMessageEvent
- [ ] TradeNftAssetsMessageEvent
- [ ] UserNftChatStylesMessageEvent
- [ ] UserNftWardrobeMessageEvent
- [ ] UserNftWardrobeSelectionMessageEvent
- [ ] UserPurchasableChatStyleChangedMessageEvent
- [ ] UserPurchasableChatStylesMessageEvent

#### Domain notifications
- [ ] ActivityPointsMessageEvent
- [ ] ElementPointerMessageEvent
- [ ] HabboAchievementNotificationMessageEvent
- [ ] HabboActivityPointNotificationMessageEvent
- [ ] HabboBroadcastMessageEvent
- [ ] InfoFeedEnableMessageEvent
- [ ] NotificationDialogMessageEvent
- [ ] OfferRewardDeliveredMessageEvent
- [ ] RestoreClientMessageEvent

#### Domain perk
- [ ] PerkAllowancesMessageEvent

#### Domain permissions
- [ ] YouAreControllerMessageEvent
- [ ] YouAreNotControllerMessageEvent
- [ ] YouAreOwnerMessageEvent

#### Domain pets
- [ ] PetCommandsMessageEvent
- [ ] PetInfoMessageEvent
- [ ] PetReceivedMessageEvent

#### Domain quest
- [ ] CommunityGoalHallOfFameMessageEvent
- [ ] CommunityGoalProgressMessageEvent
- [ ] ConcurrentUsersGoalProgressMessageEvent
- [ ] DailyTasksActiveListMessageEvent
- [ ] DailyTasksTasksAddedMessageEvent
- [ ] DailyTasksTaskUpdateMessageEvent
- [ ] EpicPopupMessageEvent
- [ ] QuestMessageEvent
- [ ] QuestCancelledMessageEvent
- [ ] QuestCompletedMessageEvent
- [ ] QuestDailyMessageEvent
- [ ] QuestsMessageEvent
- [ ] SeasonalQuestsMessageEvent

#### Domain session
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

#### Domain sound
- [ ] JukeboxPlayListFullMessageEvent
- [ ] JukeboxSongDisksMessageEvent
- [ ] NowPlayingMessageEvent
- [ ] OfficialSongIdMessageEvent
- [ ] PlayListMessageEvent
- [ ] PlayListSongAddedMessageEvent
- [ ] TraxSongInfoMessageEvent
- [ ] UserSongDisksInventoryMessageEvent

#### Domain talent
- [ ] TalentLevelUpMessageEvent
- [ ] TalentTrackMessageEvent
- [ ] TalentTrackLevelMessageEvent

#### Domain tracking
- [ ] LatencyPingResponseMessageEvent

#### Domain trading
- [ ] TradeSilverFeeMessageEvent
- [ ] TradeSilverSetMessageEvent

#### Domain treasurehunt
- [ ] TreasureHuntFailMessageEvent
- [ ] TreasureHuntFirstWinnerMessageEvent
- [ ] TreasureHuntUpdateMessageEvent

#### Domain userclassification
- [ ] UserClassificationMessageEvent

#### Domain userdefinedroomevents
- [ ] WiredEnvironmentMessageEvent
- [ ] WiredRewardResultMessageEvent

#### Domain users
- [ ] AccountSafetyLockStatusChangeMessageEvent
- [ ] ApproveNameMessageEvent
- [ ] BlockListMessageEvent
- [ ] BlockUserUpdateMessageEvent
- [ ] ExtendedProfileMessageEvent
- [ ] ExtendedProfileChangedMessageEvent
- [ ] GroupDetailsChangedMessageEvent
- [ ] GroupMembershipRequestedMessageEvent
- [ ] GuildCreatedMessageEvent
- [ ] GuildCreationInfoMessageEvent
- [ ] GuildEditFailedMessageEvent
- [ ] GuildEditInfoMessageEvent
- [ ] GuildEditorDataMessageEvent
- [ ] GuildMemberFurniCountInHQMessageEvent
- [ ] GuildMemberMgmtFailedMessageEvent
- [ ] GuildMembersMessageEvent
- [ ] GuildMembershipRejectedMessageEvent
- [ ] GuildMembershipsMessageEvent
- [ ] GuildMembershipUpdatedMessageEvent
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

#### Domain vault
- [ ] IncomeRewardClaimResponseMessageEvent
- [ ] IncomeRewardNotificationMessageEvent
- [ ] IncomeRewardStatusMessageEvent

### Missing outgoing AS3 (source) absent de Turbo.Primitives.Messages.Outgoing
#### Domain achievements
- [ ] GetAchievementsComposer

#### Domain action
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

#### Domain advertisement
- [ ] GetInterstitialMessageComposer
- [ ] InterstitialShownMessageComposer

#### Domain arena
- [ ] Game2ExitGameMessageComposer
- [ ] Game2GameChatMessageComposer
- [ ] Game2LoadStageReadyMessageComposer
- [ ] Game2PlayAgainMessageComposer

#### Domain avatar
- [ ] AvatarExpressionMessageComposer
- [ ] ChangeMottoMessageComposer
- [ ] ChangePostureMessageComposer
- [ ] ChangeUserNameMessageComposer
- [ ] ChangeUserNameInRoomMessageComposer
- [ ] CheckUserNameMessageComposer
- [ ] CustomizeAvatarWithFurniMessageComposer
- [ ] DropCarryItemMessageComposer
- [ ] GetWardrobeMessageComposer
- [ ] LookToMessageComposer
- [ ] PassCarryItemMessageComposer
- [ ] PassCarryItemToPetMessageComposer
- [ ] SaveWardrobeOutfitMessageComposer
- [ ] SignMessageComposer

#### Domain badges
- [ ] GetBadgePointLimitsComposer
- [ ] GetBadgesComposer
- [ ] GetIsBadgeRequestFulfilledComposer
- [ ] RequestABadgeComposer
- [ ] SetActivatedBadgesComposer

#### Domain bots
- [ ] CommandBotComposer
- [ ] GetBotCommandConfigurationDataComposer
- [ ] GetBotInventoryComposer

#### Domain camera
- [ ] PhotoCompetitionMessageComposer
- [ ] PublishPhotoMessageComposer
- [ ] PurchasePhotoMessageComposer
- [ ] RenderRoomMessageComposer
- [ ] RenderRoomThumbnailMessageComposer
- [ ] RequestCameraConfigurationMessageComposer

#### Domain campaign
- [ ] OpenCampaignCalendarDoorComposer
- [ ] OpenCampaignCalendarDoorAsStaffComposer

#### Domain catalog
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
- [ ] PurchaseFromCatalogComposer
- [ ] PurchaseFromCatalogAsGiftComposer
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

#### Domain chat
- [ ] CancelTypingMessageComposer
- [ ] StartTypingMessageComposer

#### Domain collectibles
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

#### Domain competition
- [ ] ForwardToACompetitionRoomMessageComposer
- [ ] ForwardToASubmittableRoomMessageComposer
- [ ] ForwardToRandomCompetitionRoomMessageComposer
- [ ] GetCurrentTimingCodeMessageComposer
- [ ] GetIsUserPartOfCompetitionMessageComposer
- [ ] GetSecondsUntilMessageComposer
- [ ] RoomCompetitionInitMessageComposer
- [ ] SubmitRoomToCompetitionMessageComposer
- [ ] VoteForRoomMessageComposer

#### Domain crafting
- [ ] CraftComposer
- [ ] CraftSecretComposer
- [ ] GetCraftableProductsComposer
- [ ] GetCraftingRecipeComposer
- [ ] GetCraftingRecipesAvailableComposer

#### Domain customfilter
- [ ] AddToCustomFilterMessageComposer
- [ ] GetCustomFilterMessageComposer
- [ ] RemoveFromCustomFilterMessageComposer

#### Domain directory
- [ ] Game2CheckGameDirectoryStatusMessageComposer
- [ ] Game2GetAccountGameStatusMessageComposer
- [ ] Game2LeaveGameMessageComposer
- [ ] Game2QuickJoinGameMessageComposer
- [ ] Game2StartSnowWarMessageComposer

#### Domain engine
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

#### Domain friendfurni
- [ ] FriendFurniConfirmLockMessageComposer

#### Domain friendlist
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

#### Domain furni
- [ ] RequestFurniInventoryComposer
- [ ] RequestFurniInventoryWhenNotInRoomComposer

#### Domain furniture
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

#### Domain gifts
- [ ] ResetPhoneNumberStateMessageComposer
- [ ] SetPhoneNumberVerificationStatusMessageComposer
- [ ] TryPhoneNumberMessageComposer
- [ ] VerifyCodeMessageComposer

#### Domain groupforums
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

#### Domain handshake
- [ ] ClientHelloMessageComposer
- [ ] CompleteDiffieHandshakeMessageComposer
- [ ] DisconnectMessageComposer
- [ ] InfoRetrieveMessageComposer
- [ ] PongMessageComposer
- [ ] SSOTicketMessageComposer
- [ ] UniqueIDMessageComposer
- [ ] VersionCheckMessageComposer

#### Domain help
- [ ] AppealCfhMessageComposer
- [ ] CallForHelpMessageComposer
- [ ] CallForHelpFromForumMessageMessageComposer
- [ ] CallForHelpFromForumThreadMessageComposer
- [ ] CallForHelpFromIMMessageComposer
- [ ] CallForHelpFromPhotoMessageComposer
- [ ] CallForHelpFromSelfieMessageComposer
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

#### Domain hotlooks
- [ ] GetHotLooksMessageComposer

#### Domain ingame
- [ ] Game2MakeSnowballMessageComposer
- [ ] Game2RequestFullStatusUpdateMessageComposer
- [ ] Game2SetUserMoveTargetMessageComposer
- [ ] Game2ThrowSnowballAtHumanMessageComposer
- [ ] Game2ThrowSnowballAtPositionMessageComposer

#### Domain landingview
- [ ] GetPromoArticlesMessageComposer

#### Domain layout
- [ ] GetOccupiedTilesMessageComposer
- [ ] GetRoomEntryTileMessageComposer
- [ ] UpdateFloorPropertiesMessageComposer

#### Domain lobby
- [ ] GetResolutionAchievementsMessageComposer
- [ ] GetUserGameAchievementsMessageComposer

#### Domain marketplace
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

#### Domain moderator
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

#### Domain mysterybox
- [ ] MysteryBoxWaitingCanceledMessageComposer

#### Domain navigator
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

#### Domain newnavigator
- [ ] NavigatorAddCollapsedCategoryMessageComposer
- [ ] NavigatorAddSavedSearchComposer
- [ ] NavigatorDeleteSavedSearchComposer
- [ ] NavigatorRemoveCollapsedCategoryMessageComposer
- [ ] NavigatorSetSearchCodeViewModeMessageComposer
- [ ] NewNavigatorInitComposer
- [ ] NewNavigatorSearchComposer

#### Domain nft
- [ ] AddNftToTradeComposer
- [ ] GetNftCreditsMessageComposer
- [ ] GetNftTradeInventoryComposer
- [ ] GetSelectedNftWardrobeOutfitMessageComposer
- [ ] GetSilverMessageComposer
- [ ] GetUserNftWardrobeMessageComposer
- [ ] RemoveNftFromTradeComposer
- [ ] SaveUserNftWardrobeMessageComposer

#### Domain notifications
- [ ] ResetUnseenItemIdsComposer
- [ ] ResetUnseenItemsComposer

#### Domain nux
- [ ] NewUserExperienceGetGiftsMessageComposer
- [ ] NewUserExperienceScriptProceedComposer

#### Domain pets
- [ ] BreedPetsMessageComposer
- [ ] CancelPetBreedingComposer
- [ ] ConfirmPetBreedingComposer
- [ ] CustomizePetWithFurniComposer
- [ ] GetPetInfoMessageComposer
- [ ] GetPetInventoryComposer
- [ ] PetSelectedMessageComposer
- [ ] RespectPetMessageComposer

#### Domain poll
- [ ] PollAnswerComposer
- [ ] PollRejectComposer
- [ ] PollStartComposer

#### Domain preferences
- [ ] SetChatPreferencesMessageComposer
- [ ] SetChatStylePreferenceComposer
- [ ] SetIgnoreRoomInvitesMessageComposer
- [ ] SetNewNavigatorWindowPreferencesMessageComposer
- [ ] SetRoomCameraPreferencesMessageComposer
- [ ] SetSoundSettingsComposer
- [ ] SetUIFlagsMessageComposer

#### Domain purse
- [ ] GetCreditsInfoComposer

#### Domain quest
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

#### Domain register
- [ ] UpdateFigureDataMessageComposer

#### Domain roomdirectory
- [ ] RoomNetworkOpenConnectionMessageComposer

#### Domain roomsettings
- [ ] DeleteRoomMessageComposer
- [ ] GetBannedUsersFromRoomMessageComposer
- [ ] GetCustomRoomFilterMessageComposer
- [ ] GetFlatControllersMessageComposer
- [ ] GetRoomSettingsMessageComposer
- [ ] SaveRoomSettingsMessageComposer
- [ ] UpdateRoomCategoryAndTradeSettingsComposer
- [ ] UpdateRoomFilterMessageComposer

#### Domain score
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

#### Domain session
- [ ] ChangeQueueMessageComposer
- [ ] OpenFlatConnectionMessageComposer
- [ ] QuitMessageComposer

#### Domain sound
- [ ] AddJukeboxDiskComposer
- [ ] GetJukeboxPlayListMessageComposer
- [ ] GetNowPlayingMessageComposer
- [ ] GetOfficialSongIdMessageComposer
- [ ] GetSongInfoMessageComposer
- [ ] GetSoundMachinePlayListMessageComposer
- [ ] GetSoundSettingsComposer
- [ ] GetUserSongDisksMessageComposer
- [ ] RemoveJukeboxDiskComposer

#### Domain talent
- [ ] GetTalentTrackMessageComposer
- [ ] GetTalentTrackLevelMessageComposer
- [ ] GuideAdvertisementReadMessageComposer

#### Domain tracking
- [ ] EventLogMessageComposer
- [ ] LagWarningReportMessageComposer
- [ ] LatencyPingReportMessageComposer
- [ ] LatencyPingRequestMessageComposer
- [ ] PerformanceLogMessageComposer

#### Domain trading
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

#### Domain treasurehunt
- [ ] ProgressTreasureHuntMessageComposer

#### Domain userclassification
- [ ] PeerUsersClassificationMessageComposer
- [ ] RoomUsersClassificationMessageComposer

#### Domain userdefinedroomevents
- [ ] ApplySnapshotMessageComposer
- [ ] UpdateActionMessageComposer
- [ ] UpdateAddonMessageComposer
- [ ] UpdateConditionMessageComposer
- [ ] UpdateSelectorMessageComposer
- [ ] UpdateTriggerMessageComposer
- [ ] UpdateVariableMessageComposer
- [ ] WiredClickUserMessageComposer

#### Domain users
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
- [ ] GetExtendedProfileMessageComposer
- [ ] GetExtendedProfileByNameMessageComposer
- [ ] GetGuildCreationInfoMessageComposer
- [ ] GetGuildEditInfoMessageComposer
- [ ] GetGuildEditorDataMessageComposer
- [ ] GetGuildMembersMessageComposer
- [ ] GetGuildMembershipsMessageComposer
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

#### Domain variablesmanagement
- [ ] WiredGetUserPermanentVariablesComposer
- [ ] WiredGetVariableOwnersPageComposer
- [ ] WiredSetUserPermanentVariableComposer

#### Domain vault
- [ ] IncomeRewardClaimMessageComposer
- [ ] WithdrawCreditVaultMessageComposer

#### Domain votes
- [ ] CommunityGoalVoteMessageComposer

#### Domain wiredmenu
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

## P1 - Mapping Revision20260112
### Headers Event manquants
#### Domain action
- [ ] AvatarEffectMessageEvent
- [ ] CarryObjectMessageEvent
- [ ] ExpressionMessageEvent
- [ ] SleepMessageEvent
- [ ] UseObjectMessageEvent

#### Domain advertisement
- [ ] InterstitialMessageEvent

#### Domain arena
- [ ] Game2ArenaEnteredMessageEvent
- [ ] Game2EnterArenaMessageEvent
- [ ] Game2EnterArenaFailedMessageEvent
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

#### Domain availability
- [ ] AvailabilityStatusMessageEvent
- [ ] InfoHotelClosedMessageEvent
- [ ] InfoHotelClosingMessageEvent
- [ ] LoginFailedHotelClosedMessageEvent
- [ ] MaintenanceStatusMessageEvent

#### Domain avatar
- [ ] ChangeUserNameResultMessageEvent
- [ ] CheckUserNameResultMessageEvent
- [ ] WardrobeMessageEvent

#### Domain avatareffect
- [ ] AvatarEffectAddedMessageEvent
- [ ] AvatarEffectExpiredMessageEvent
- [ ] AvatarEffectsMessageEvent

#### Domain callforhelp
- [ ] CfhSanctionMessageEvent
- [ ] CfhTopicsInitMessageEvent
- [ ] MyCfhReportStatusMessageEvent

#### Domain camera
- [ ] CameraPublishStatusMessageEvent
- [ ] CameraPurchaseOKMessageEvent
- [ ] CameraStorageUrlMessageEvent
- [ ] CompetitionStatusMessageEvent
- [ ] InitCameraMessageEvent
- [ ] ThumbnailStatusMessageEvent

#### Domain campaign
- [ ] CampaignCalendarDataMessageEvent
- [ ] CampaignCalendarDoorOpenedMessageEvent

#### Domain catalog
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

#### Domain chat
- [ ] FloodControlMessageEvent
- [ ] RoomChatSettingsMessageEvent
- [ ] RoomFilterSettingsMessageEvent
- [ ] UserTypingMessageEvent

#### Domain collectibles
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

#### Domain competition
- [ ] CompetitionEntrySubmitResultMessageEvent
- [ ] CompetitionVotingInfoMessageEvent
- [ ] CurrentTimingCodeMessageEvent
- [ ] IsUserPartOfCompetitionMessageEvent
- [ ] NoOwnedRoomsAlertMessageEvent
- [ ] SecondsUntilMessageEvent

#### Domain crafting
- [ ] CraftableProductsMessageEvent
- [ ] CraftingRecipeMessageEvent
- [ ] CraftingRecipesAvailableMessageEvent
- [ ] CraftingResultMessageEvent

#### Domain customfilter
- [ ] GetCustomFilterResultMessageEvent
- [ ] ModifyCustomFilterResultMessageEvent

#### Domain directory
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

#### Domain engine
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
- [ ] ObjectRemoveMessageEvent
- [ ] ObjectRemoveConfirmMessageEvent
- [ ] ObjectRemoveMultipleMessageEvent
- [ ] ObjectsMessageEvent
- [ ] ObjectsDataUpdateMessageEvent
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

#### Domain friendfurni
- [ ] FriendFurniCancelLockMessageEvent
- [ ] FriendFurniOtherLockConfirmedMessageEvent
- [ ] FriendFurniStartConfirmationMessageEvent

#### Domain friendlist
- [ ] FriendListFragmentMessageEvent
- [ ] MiniMailNewMessageEvent
- [ ] NewConsoleMessageEvent

#### Domain furniture
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

#### Domain gifts
- [ ] PhoneCollectionStateMessageEvent
- [ ] TryPhoneNumberResultMessageEvent
- [ ] TryVerificationCodeResultMessageEvent

#### Domain groupforums
- [ ] ForumDataMessageEvent
- [ ] ForumsListMessageEvent
- [ ] ForumThreadsMessageEvent
- [ ] PostThreadMessageEvent
- [ ] ThreadMessagesMessageEvent
- [ ] UnreadForumsCountMessageEvent
- [ ] UpdateMessageMessageEvent

#### Domain handshake
- [ ] AuthenticationOKMessageEvent
- [ ] NoobnessLevelMessageEvent
- [ ] PingMessageEvent
- [ ] UserRightsMessageEvent

#### Domain help
- [ ] CallForHelpDisabledNotifyMessageEvent
- [ ] CallForHelpPendingCallsMessageEvent
- [ ] CallForHelpPendingCallsDeletedMessageEvent
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

#### Domain hotlooks
- [ ] HotLooksMessageEvent

#### Domain ingame
- [ ] Game2FullGameStatusMessageEvent
- [ ] Game2GameStatusMessageEvent

#### Domain landingview
- [ ] PromoArticlesMessageEvent

#### Domain layout
- [ ] RoomEntryTileMessageEvent
- [ ] RoomOccupiedTilesMessageEvent

#### Domain lobby
- [ ] AchievementResolutionCompletedMessageEvent
- [ ] AchievementResolutionProgressMessageEvent
- [ ] AchievementResolutionsMessageEvent
- [ ] UserGameAchievementsMessageEvent

#### Domain moderation
- [ ] BanInfoMessageEvent
- [ ] IssueDeletedMessageEvent
- [ ] IssueInfoMessageEvent
- [ ] IssuePickFailedMessageEvent
- [ ] ModeratorMessageEvent
- [ ] ModeratorActionResultMessageEvent
- [ ] ModeratorInitMessageEvent
- [ ] UserBannedMessageEvent

#### Domain mysterybox
- [ ] CancelMysteryBoxWaitMessageEvent
- [ ] GotMysteryBoxPrizeMessageEvent
- [ ] MysteryBoxKeysMessageEvent
- [ ] ShowMysteryBoxWaitMessageEvent

#### Domain navigator
- [ ] CompetitionRoomsDataMessageEvent
- [ ] DoorbellMessageEvent
- [ ] FlatAccessDeniedMessageEvent
- [ ] NavigatorCollapsedCategoriesMessageEvent

#### Domain nft
- [ ] NftEmeraldConvertResultMessageEvent
- [ ] TradeNftAssetInventoryMessageEvent
- [ ] TradeNftAssetsMessageEvent
- [ ] UserNftChatStylesMessageEvent
- [ ] UserNftWardrobeMessageEvent
- [ ] UserNftWardrobeSelectionMessageEvent
- [ ] UserPurchasableChatStyleChangedMessageEvent
- [ ] UserPurchasableChatStylesMessageEvent

#### Domain notifications
- [ ] ActivityPointsMessageEvent
- [ ] ElementPointerMessageEvent
- [ ] HabboAchievementNotificationMessageEvent
- [ ] HabboActivityPointNotificationMessageEvent
- [ ] HabboBroadcastMessageEvent
- [ ] InfoFeedEnableMessageEvent
- [ ] NotificationDialogMessageEvent
- [ ] OfferRewardDeliveredMessageEvent
- [ ] RestoreClientMessageEvent

#### Domain perk
- [ ] PerkAllowancesMessageEvent

#### Domain permissions
- [ ] YouAreControllerMessageEvent
- [ ] YouAreNotControllerMessageEvent
- [ ] YouAreOwnerMessageEvent

#### Domain pets
- [ ] PetCommandsMessageEvent
- [ ] PetInfoMessageEvent
- [ ] PetReceivedMessageEvent

#### Domain quest
- [ ] CommunityGoalHallOfFameMessageEvent
- [ ] CommunityGoalProgressMessageEvent
- [ ] ConcurrentUsersGoalProgressMessageEvent
- [ ] DailyTasksActiveListMessageEvent
- [ ] DailyTasksTasksAddedMessageEvent
- [ ] DailyTasksTaskUpdateMessageEvent
- [ ] EpicPopupMessageEvent
- [ ] QuestMessageEvent
- [ ] QuestCancelledMessageEvent
- [ ] QuestCompletedMessageEvent
- [ ] QuestDailyMessageEvent
- [ ] QuestsMessageEvent
- [ ] SeasonalQuestsMessageEvent

#### Domain session
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

#### Domain sound
- [ ] JukeboxPlayListFullMessageEvent
- [ ] JukeboxSongDisksMessageEvent
- [ ] NowPlayingMessageEvent
- [ ] OfficialSongIdMessageEvent
- [ ] PlayListMessageEvent
- [ ] PlayListSongAddedMessageEvent
- [ ] TraxSongInfoMessageEvent
- [ ] UserSongDisksInventoryMessageEvent

#### Domain talent
- [ ] TalentLevelUpMessageEvent
- [ ] TalentTrackMessageEvent
- [ ] TalentTrackLevelMessageEvent

#### Domain tracking
- [ ] LatencyPingResponseMessageEvent

#### Domain trading
- [ ] TradeSilverFeeMessageEvent
- [ ] TradeSilverSetMessageEvent

#### Domain treasurehunt
- [ ] TreasureHuntFailMessageEvent
- [ ] TreasureHuntFirstWinnerMessageEvent
- [ ] TreasureHuntUpdateMessageEvent

#### Domain userclassification
- [ ] UserClassificationMessageEvent

#### Domain userdefinedroomevents
- [ ] WiredEnvironmentMessageEvent
- [ ] WiredRewardResultMessageEvent

#### Domain users
- [ ] AccountSafetyLockStatusChangeMessageEvent
- [ ] BlockListMessageEvent
- [ ] BlockUserUpdateMessageEvent
- [ ] ExtendedProfileMessageEvent
- [ ] ExtendedProfileChangedMessageEvent
- [ ] GroupDetailsChangedMessageEvent
- [ ] GroupMembershipRequestedMessageEvent
- [ ] GuildCreatedMessageEvent
- [ ] GuildCreationInfoMessageEvent
- [ ] GuildEditFailedMessageEvent
- [ ] GuildEditInfoMessageEvent
- [ ] GuildEditorDataMessageEvent
- [ ] GuildMemberFurniCountInHQMessageEvent
- [ ] GuildMemberMgmtFailedMessageEvent
- [ ] GuildMembersMessageEvent
- [ ] GuildMembershipRejectedMessageEvent
- [ ] GuildMembershipsMessageEvent
- [ ] GuildMembershipUpdatedMessageEvent
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

#### Domain vault
- [ ] IncomeRewardClaimResponseMessageEvent
- [ ] IncomeRewardNotificationMessageEvent

### Headers Composer manquants
#### Domain achievements
- [ ] GetAchievementsComposer

#### Domain action
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

#### Domain advertisement
- [ ] GetInterstitialMessageComposer
- [ ] InterstitialShownMessageComposer

#### Domain arena
- [ ] Game2ExitGameMessageComposer
- [ ] Game2GameChatMessageComposer
- [ ] Game2LoadStageReadyMessageComposer
- [ ] Game2PlayAgainMessageComposer

#### Domain avatar
- [ ] AvatarExpressionMessageComposer
- [ ] ChangeMottoMessageComposer
- [ ] ChangePostureMessageComposer
- [ ] ChangeUserNameMessageComposer
- [ ] ChangeUserNameInRoomMessageComposer
- [ ] CheckUserNameMessageComposer
- [ ] CustomizeAvatarWithFurniMessageComposer
- [ ] DropCarryItemMessageComposer
- [ ] GetWardrobeMessageComposer
- [ ] LookToMessageComposer
- [ ] PassCarryItemMessageComposer
- [ ] PassCarryItemToPetMessageComposer
- [ ] SaveWardrobeOutfitMessageComposer
- [ ] SignMessageComposer

#### Domain badges
- [ ] GetBadgePointLimitsComposer
- [ ] GetBadgesComposer
- [ ] GetIsBadgeRequestFulfilledComposer
- [ ] RequestABadgeComposer
- [ ] SetActivatedBadgesComposer

#### Domain bots
- [ ] CommandBotComposer
- [ ] GetBotCommandConfigurationDataComposer
- [ ] GetBotInventoryComposer

#### Domain camera
- [ ] PhotoCompetitionMessageComposer
- [ ] PublishPhotoMessageComposer
- [ ] PurchasePhotoMessageComposer
- [ ] RenderRoomMessageComposer
- [ ] RenderRoomThumbnailMessageComposer
- [ ] RequestCameraConfigurationMessageComposer

#### Domain campaign
- [ ] OpenCampaignCalendarDoorComposer
- [ ] OpenCampaignCalendarDoorAsStaffComposer

#### Domain catalog
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
- [ ] PurchaseFromCatalogComposer
- [ ] PurchaseFromCatalogAsGiftComposer
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

#### Domain chat
- [ ] CancelTypingMessageComposer
- [ ] StartTypingMessageComposer

#### Domain collectibles
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

#### Domain competition
- [ ] ForwardToACompetitionRoomMessageComposer
- [ ] ForwardToASubmittableRoomMessageComposer
- [ ] ForwardToRandomCompetitionRoomMessageComposer
- [ ] GetCurrentTimingCodeMessageComposer
- [ ] GetIsUserPartOfCompetitionMessageComposer
- [ ] GetSecondsUntilMessageComposer
- [ ] RoomCompetitionInitMessageComposer
- [ ] SubmitRoomToCompetitionMessageComposer
- [ ] VoteForRoomMessageComposer

#### Domain crafting
- [ ] CraftComposer
- [ ] CraftSecretComposer
- [ ] GetCraftableProductsComposer
- [ ] GetCraftingRecipeComposer
- [ ] GetCraftingRecipesAvailableComposer

#### Domain customfilter
- [ ] AddToCustomFilterMessageComposer
- [ ] GetCustomFilterMessageComposer
- [ ] RemoveFromCustomFilterMessageComposer

#### Domain directory
- [ ] Game2CheckGameDirectoryStatusMessageComposer
- [ ] Game2GetAccountGameStatusMessageComposer
- [ ] Game2LeaveGameMessageComposer
- [ ] Game2QuickJoinGameMessageComposer
- [ ] Game2StartSnowWarMessageComposer

#### Domain engine
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

#### Domain friendfurni
- [ ] FriendFurniConfirmLockMessageComposer

#### Domain friendlist
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

#### Domain furni
- [ ] RequestFurniInventoryComposer
- [ ] RequestFurniInventoryWhenNotInRoomComposer

#### Domain furniture
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

#### Domain gifts
- [ ] ResetPhoneNumberStateMessageComposer
- [ ] SetPhoneNumberVerificationStatusMessageComposer
- [ ] TryPhoneNumberMessageComposer
- [ ] VerifyCodeMessageComposer

#### Domain groupforums
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

#### Domain handshake
- [ ] ClientHelloMessageComposer
- [ ] DisconnectMessageComposer
- [ ] InfoRetrieveMessageComposer
- [ ] PongMessageComposer
- [ ] SSOTicketMessageComposer
- [ ] UniqueIDMessageComposer
- [ ] VersionCheckMessageComposer

#### Domain help
- [ ] AppealCfhMessageComposer
- [ ] CallForHelpMessageComposer
- [ ] CallForHelpFromForumMessageMessageComposer
- [ ] CallForHelpFromForumThreadMessageComposer
- [ ] CallForHelpFromIMMessageComposer
- [ ] CallForHelpFromPhotoMessageComposer
- [ ] CallForHelpFromSelfieMessageComposer
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

#### Domain hotlooks
- [ ] GetHotLooksMessageComposer

#### Domain ingame
- [ ] Game2MakeSnowballMessageComposer
- [ ] Game2RequestFullStatusUpdateMessageComposer
- [ ] Game2SetUserMoveTargetMessageComposer
- [ ] Game2ThrowSnowballAtHumanMessageComposer
- [ ] Game2ThrowSnowballAtPositionMessageComposer

#### Domain landingview
- [ ] GetPromoArticlesMessageComposer

#### Domain layout
- [ ] GetOccupiedTilesMessageComposer
- [ ] GetRoomEntryTileMessageComposer
- [ ] UpdateFloorPropertiesMessageComposer

#### Domain lobby
- [ ] GetResolutionAchievementsMessageComposer
- [ ] GetUserGameAchievementsMessageComposer

#### Domain marketplace
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

#### Domain moderator
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

#### Domain mysterybox
- [ ] MysteryBoxWaitingCanceledMessageComposer

#### Domain navigator
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

#### Domain newnavigator
- [ ] NavigatorAddCollapsedCategoryMessageComposer
- [ ] NavigatorAddSavedSearchComposer
- [ ] NavigatorDeleteSavedSearchComposer
- [ ] NavigatorRemoveCollapsedCategoryMessageComposer
- [ ] NavigatorSetSearchCodeViewModeMessageComposer
- [ ] NewNavigatorInitComposer
- [ ] NewNavigatorSearchComposer

#### Domain nft
- [ ] AddNftToTradeComposer
- [ ] GetNftCreditsMessageComposer
- [ ] GetNftTradeInventoryComposer
- [ ] GetSelectedNftWardrobeOutfitMessageComposer
- [ ] GetSilverMessageComposer
- [ ] GetUserNftWardrobeMessageComposer
- [ ] RemoveNftFromTradeComposer
- [ ] SaveUserNftWardrobeMessageComposer

#### Domain notifications
- [ ] ResetUnseenItemIdsComposer
- [ ] ResetUnseenItemsComposer

#### Domain nux
- [ ] NewUserExperienceGetGiftsMessageComposer
- [ ] NewUserExperienceScriptProceedComposer

#### Domain pets
- [ ] BreedPetsMessageComposer
- [ ] CancelPetBreedingComposer
- [ ] ConfirmPetBreedingComposer
- [ ] CustomizePetWithFurniComposer
- [ ] GetPetInfoMessageComposer
- [ ] GetPetInventoryComposer
- [ ] PetSelectedMessageComposer
- [ ] RespectPetMessageComposer

#### Domain poll
- [ ] PollAnswerComposer
- [ ] PollRejectComposer
- [ ] PollStartComposer

#### Domain preferences
- [ ] SetChatPreferencesMessageComposer
- [ ] SetChatStylePreferenceComposer
- [ ] SetIgnoreRoomInvitesMessageComposer
- [ ] SetNewNavigatorWindowPreferencesMessageComposer
- [ ] SetRoomCameraPreferencesMessageComposer
- [ ] SetSoundSettingsComposer
- [ ] SetUIFlagsMessageComposer

#### Domain purse
- [ ] GetCreditsInfoComposer

#### Domain quest
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

#### Domain register
- [ ] UpdateFigureDataMessageComposer

#### Domain roomdirectory
- [ ] RoomNetworkOpenConnectionMessageComposer

#### Domain roomsettings
- [ ] DeleteRoomMessageComposer
- [ ] GetBannedUsersFromRoomMessageComposer
- [ ] GetCustomRoomFilterMessageComposer
- [ ] GetFlatControllersMessageComposer
- [ ] GetRoomSettingsMessageComposer
- [ ] SaveRoomSettingsMessageComposer
- [ ] UpdateRoomCategoryAndTradeSettingsComposer
- [ ] UpdateRoomFilterMessageComposer

#### Domain score
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

#### Domain session
- [ ] ChangeQueueMessageComposer
- [ ] OpenFlatConnectionMessageComposer
- [ ] QuitMessageComposer

#### Domain sound
- [ ] AddJukeboxDiskComposer
- [ ] GetJukeboxPlayListMessageComposer
- [ ] GetNowPlayingMessageComposer
- [ ] GetOfficialSongIdMessageComposer
- [ ] GetSongInfoMessageComposer
- [ ] GetSoundMachinePlayListMessageComposer
- [ ] GetSoundSettingsComposer
- [ ] GetUserSongDisksMessageComposer
- [ ] RemoveJukeboxDiskComposer

#### Domain talent
- [ ] GetTalentTrackMessageComposer
- [ ] GetTalentTrackLevelMessageComposer
- [ ] GuideAdvertisementReadMessageComposer

#### Domain tracking
- [ ] EventLogMessageComposer
- [ ] LagWarningReportMessageComposer
- [ ] LatencyPingReportMessageComposer
- [ ] LatencyPingRequestMessageComposer
- [ ] PerformanceLogMessageComposer

#### Domain trading
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

#### Domain treasurehunt
- [ ] ProgressTreasureHuntMessageComposer

#### Domain userclassification
- [ ] PeerUsersClassificationMessageComposer
- [ ] RoomUsersClassificationMessageComposer

#### Domain userdefinedroomevents
- [ ] ApplySnapshotMessageComposer
- [ ] UpdateActionMessageComposer
- [ ] UpdateAddonMessageComposer
- [ ] UpdateConditionMessageComposer
- [ ] UpdateSelectorMessageComposer
- [ ] UpdateTriggerMessageComposer
- [ ] UpdateVariableMessageComposer
- [ ] WiredClickUserMessageComposer

#### Domain users
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
- [ ] GetExtendedProfileMessageComposer
- [ ] GetExtendedProfileByNameMessageComposer
- [ ] GetGuildCreationInfoMessageComposer
- [ ] GetGuildEditInfoMessageComposer
- [ ] GetGuildEditorDataMessageComposer
- [ ] GetGuildMembersMessageComposer
- [ ] GetGuildMembershipsMessageComposer
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

#### Domain variablesmanagement
- [ ] WiredGetUserPermanentVariablesComposer
- [ ] WiredGetVariableOwnersPageComposer
- [ ] WiredSetUserPermanentVariableComposer

#### Domain vault
- [ ] IncomeRewardClaimMessageComposer
- [ ] WithdrawCreditVaultMessageComposer

#### Domain votes
- [ ] CommunityGoalVoteMessageComposer

#### Domain wiredmenu
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

### Parsers manquants (Revision)
#### Domain achievements
- [ ] AchievementEventParser
- [ ] AchievementsEventParser
- [ ] AchievementsScoreEventParser

#### Domain action
- [ ] AvatarEffectMessageEventParser
- [ ] CarryObjectMessageEventParser
- [ ] ExpressionMessageEventParser
- [ ] SleepMessageEventParser
- [ ] UseObjectMessageEventParser

#### Domain advertisement
- [ ] InterstitialMessageEventParser
- [ ] RoomAdErrorEventParser

#### Domain arena
- [ ] Game2ArenaEnteredMessageEventParser
- [ ] Game2EnterArenaMessageEventParser
- [ ] Game2EnterArenaFailedMessageEventParser
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

#### Domain availability
- [ ] AvailabilityStatusMessageEventParser
- [ ] InfoHotelClosedMessageEventParser
- [ ] InfoHotelClosingMessageEventParser
- [ ] LoginFailedHotelClosedMessageEventParser
- [ ] MaintenanceStatusMessageEventParser

#### Domain avatar
- [ ] ChangeUserNameResultMessageEventParser
- [ ] CheckUserNameResultMessageEventParser
- [ ] FigureUpdateEventParser
- [ ] WardrobeMessageEventParser

#### Domain avatareffect
- [ ] AvatarEffectAddedMessageEventParser
- [ ] AvatarEffectExpiredMessageEventParser
- [ ] AvatarEffectsMessageEventParser

#### Domain badges
- [ ] BadgePointLimitsEventParser
- [ ] BadgeReceivedEventParser
- [ ] BadgesEventParser
- [ ] IsBadgeRequestFulfilledEventParser

#### Domain bots
- [ ] BotAddedToInventoryEventParser
- [ ] BotCommandConfigurationEventParser
- [ ] BotErrorEventParser
- [ ] BotForceOpenContextMenuEventParser
- [ ] BotInventoryEventParser
- [ ] BotRemovedFromInventoryEventParser
- [ ] BotSkillListUpdateEventParser

#### Domain callforhelp
- [ ] CfhSanctionMessageEventParser
- [ ] CfhTopicsInitMessageEventParser
- [ ] MyCfhReportStatusMessageEventParser
- [ ] SanctionStatusEventParser

#### Domain camera
- [ ] CameraPublishStatusMessageEventParser
- [ ] CameraPurchaseOKMessageEventParser
- [ ] CameraStorageUrlMessageEventParser
- [ ] CompetitionStatusMessageEventParser
- [ ] InitCameraMessageEventParser
- [ ] ThumbnailStatusMessageEventParser

#### Domain campaign
- [ ] CampaignCalendarDataMessageEventParser
- [ ] CampaignCalendarDoorOpenedMessageEventParser

#### Domain catalog
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

#### Domain chat
- [ ] FloodControlMessageEventParser
- [ ] RemainingMutePeriodEventParser
- [ ] RoomChatSettingsMessageEventParser
- [ ] RoomFilterSettingsMessageEventParser
- [ ] UserTypingMessageEventParser

#### Domain clothing
- [ ] FigureSetIdsEventParser

#### Domain collectibles
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

#### Domain competition
- [ ] CompetitionEntrySubmitResultMessageEventParser
- [ ] CompetitionVotingInfoMessageEventParser
- [ ] CurrentTimingCodeMessageEventParser
- [ ] IsUserPartOfCompetitionMessageEventParser
- [ ] NoOwnedRoomsAlertMessageEventParser
- [ ] SecondsUntilMessageEventParser

#### Domain crafting
- [ ] CraftableProductsMessageEventParser
- [ ] CraftingRecipeMessageEventParser
- [ ] CraftingRecipesAvailableMessageEventParser
- [ ] CraftingResultMessageEventParser

#### Domain customfilter
- [ ] GetCustomFilterResultMessageEventParser
- [ ] ModifyCustomFilterResultMessageEventParser

#### Domain directory
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

#### Domain engine
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
- [ ] ObjectRemoveMessageEventParser
- [ ] ObjectRemoveConfirmMessageEventParser
- [ ] ObjectRemoveMultipleMessageEventParser
- [ ] ObjectsMessageEventParser
- [ ] ObjectsDataUpdateMessageEventParser
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

#### Domain error
- [ ] ErrorReportEventParser

#### Domain friendfurni
- [ ] FriendFurniCancelLockMessageEventParser
- [ ] FriendFurniOtherLockConfirmedMessageEventParser
- [ ] FriendFurniStartConfirmationMessageEventParser

#### Domain friendlist
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

#### Domain furni
- [ ] FurniListAddOrUpdateEventParser
- [ ] FurniListEventParser
- [ ] FurniListInvalidateEventParser
- [ ] FurniListRemoveEventParser
- [ ] FurniListRemoveMultipleEventParser
- [ ] PostItPlacedEventParser

#### Domain furniture
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

#### Domain gifts
- [ ] PhoneCollectionStateMessageEventParser
- [ ] TryPhoneNumberResultMessageEventParser
- [ ] TryVerificationCodeResultMessageEventParser

#### Domain groupforums
- [ ] ForumDataMessageEventParser
- [ ] ForumsListMessageEventParser
- [ ] ForumThreadsMessageEventParser
- [ ] PostThreadMessageEventParser
- [ ] ThreadMessagesMessageEventParser
- [ ] UnreadForumsCountMessageEventParser
- [ ] UpdateMessageMessageEventParser

#### Domain handshake
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

#### Domain help
- [ ] CallForHelpDisabledNotifyMessageEventParser
- [ ] CallForHelpPendingCallsMessageEventParser
- [ ] CallForHelpPendingCallsDeletedMessageEventParser
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

#### Domain hotlooks
- [ ] HotLooksMessageEventParser

#### Domain ingame
- [ ] Game2FullGameStatusMessageEventParser
- [ ] Game2GameStatusMessageEventParser

#### Domain landingview
- [ ] PromoArticlesMessageEventParser

#### Domain layout
- [ ] RoomEntryTileMessageEventParser
- [ ] RoomOccupiedTilesMessageEventParser

#### Domain lobby
- [ ] AchievementResolutionCompletedMessageEventParser
- [ ] AchievementResolutionProgressMessageEventParser
- [ ] AchievementResolutionsMessageEventParser
- [ ] UserGameAchievementsMessageParser

#### Domain marketplace
- [ ] MarketplaceBuyOfferResultEventParser
- [ ] MarketplaceCancelOfferResultEventParser
- [ ] MarketplaceCanMakeOfferResultParser
- [ ] MarketplaceConfigurationEventParser
- [ ] MarketplaceItemStatsEventParser
- [ ] MarketplaceMakeOfferResultParser
- [ ] MarketPlaceOffersEventParser
- [ ] MarketPlaceOwnOffersEventParser

#### Domain moderation
- [ ] BanInfoMessageEventParser
- [ ] CfhChatlogEventParser
- [ ] IssueDeletedMessageEventParser
- [ ] IssueInfoMessageEventParser
- [ ] IssuePickFailedMessageEventParser
- [ ] ModeratorMessageEventParser
- [ ] ModeratorActionResultMessageEventParser
- [ ] ModeratorCautionEventParser
- [ ] ModeratorInitMessageEventParser
- [ ] ModeratorRoomInfoEventParser
- [ ] ModeratorToolPreferencesEventParser
- [ ] ModeratorUserInfoEventParser
- [ ] RoomChatlogEventParser
- [ ] RoomVisitsEventParser
- [ ] UserBannedMessageEventParser
- [ ] UserChatlogEventParser

#### Domain mysterybox
- [ ] CancelMysteryBoxWaitMessageEventParser
- [ ] GotMysteryBoxPrizeMessageEventParser
- [ ] MysteryBoxKeysMessageEventParser
- [ ] ShowMysteryBoxWaitMessageEventParser

#### Domain navigator
- [ ] CanCreateRoomEventParser
- [ ] CanCreateRoomEventEventParser
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

#### Domain nft
- [ ] NftEmeraldConvertResultMessageEventParser
- [ ] TradeNftAssetInventoryMessageEventParser
- [ ] TradeNftAssetsMessageEventParser
- [ ] UserNftChatStylesMessageEventParser
- [ ] UserNftWardrobeMessageEventParser
- [ ] UserNftWardrobeSelectionMessageEventParser
- [ ] UserPurchasableChatStyleChangedMessageEventParser
- [ ] UserPurchasableChatStylesMessageEventParser

#### Domain notifications
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

#### Domain nux
- [ ] NewUserExperienceGiftOfferEventParser
- [ ] NewUserExperienceNotCompleteEventParser
- [ ] SelectInitialRoomEventParser

#### Domain perk
- [ ] CitizenshipVipOfferPromoEnabledEventParser
- [ ] PerkAllowancesMessageEventParser

#### Domain permissions
- [ ] YouAreControllerMessageEventParser
- [ ] YouAreNotControllerMessageEventParser
- [ ] YouAreOwnerMessageEventParser

#### Domain pets
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

#### Domain poll
- [ ] PollContentsEventParser
- [ ] PollErrorEventParser
- [ ] PollOfferEventParser
- [ ] QuestionAnsweredEventParser
- [ ] QuestionEventParser
- [ ] QuestionFinishedEventParser

#### Domain preferences
- [ ] AccountPreferencesEventParser

#### Domain purse
- [ ] CreditBalanceEventParser

#### Domain quest
- [ ] CommunityGoalHallOfFameMessageEventParser
- [ ] CommunityGoalProgressMessageEventParser
- [ ] ConcurrentUsersGoalProgressMessageEventParser
- [ ] DailyTasksActiveListMessageEventParser
- [ ] DailyTasksTasksAddedMessageEventParser
- [ ] DailyTasksTaskUpdateMessageEventParser
- [ ] EpicPopupMessageEventParser
- [ ] QuestMessageEventParser
- [ ] QuestCancelledMessageEventParser
- [ ] QuestCompletedMessageEventParser
- [ ] QuestDailyMessageEventParser
- [ ] QuestsMessageEventParser
- [ ] SeasonalQuestsMessageEventParser

#### Domain roomsettings
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

#### Domain score
- [ ] Game2GroupLeaderboardParser
- [ ] Game2LeaderboardParser
- [ ] Game2WeeklyGroupLeaderboardParser
- [ ] Game2WeeklyLeaderboardParser

#### Domain session
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

#### Domain sound
- [ ] JukeboxPlayListFullMessageEventParser
- [ ] JukeboxSongDisksMessageEventParser
- [ ] NowPlayingMessageEventParser
- [ ] OfficialSongIdMessageEventParser
- [ ] PlayListMessageEventParser
- [ ] PlayListSongAddedMessageEventParser
- [ ] TraxSongInfoMessageEventParser
- [ ] UserSongDisksInventoryMessageEventParser

#### Domain talent
- [ ] TalentLevelUpMessageEventParser
- [ ] TalentTrackMessageEventParser
- [ ] TalentTrackLevelMessageEventParser

#### Domain tracking
- [ ] LatencyPingResponseMessageEventParser

#### Domain trading
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

#### Domain treasurehunt
- [ ] TreasureHuntFailMessageEventParser
- [ ] TreasureHuntFirstWinnerMessageEventParser
- [ ] TreasureHuntUpdateMessageEventParser

#### Domain userclassification
- [ ] UserClassificationMessageEventParser

#### Domain userdefinedroomevents
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

#### Domain users
- [ ] AccountSafetyLockStatusChangeMessageEventParser
- [ ] BlockListMessageEventParser
- [ ] BlockUserUpdateMessageEventParser
- [ ] ChangeEmailResultEventParser
- [ ] EmailStatusResultEventParser
- [ ] ExtendedProfileMessageEventParser
- [ ] ExtendedProfileChangedMessageEventParser
- [ ] GroupDetailsChangedMessageEventParser
- [ ] GroupMembershipRequestedMessageEventParser
- [ ] GuildCreatedMessageEventParser
- [ ] GuildCreationInfoMessageEventParser
- [ ] GuildEditFailedMessageEventParser
- [ ] GuildEditInfoMessageEventParser
- [ ] GuildEditorDataMessageEventParser
- [ ] GuildMemberFurniCountInHQMessageEventParser
- [ ] GuildMemberMgmtFailedMessageEventParser
- [ ] GuildMembersMessageEventParser
- [ ] GuildMembershipRejectedMessageEventParser
- [ ] GuildMembershipsMessageEventParser
- [ ] GuildMembershipUpdatedMessageEventParser
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

#### Domain variablesmanagement
- [ ] WiredSetUserPermanentVariableResultEventParser
- [ ] WiredUserPermanentVariablesEventParser
- [ ] WiredUserVariablesListEventParser

#### Domain vault
- [ ] IncomeRewardClaimResponseMessageEventParser
- [ ] IncomeRewardNotificationMessageEventParser

#### Domain votes
- [ ] CommunityVoteReceivedEventParser

#### Domain wiredmenu
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

### Serializers manquants (Revision)
#### Domain achievements
- [ ] GetAchievementsComposer

#### Domain action
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

#### Domain advertisement
- [ ] GetInterstitialMessageComposer
- [ ] InterstitialShownMessageComposer

#### Domain arena
- [ ] Game2ExitGameMessageComposer
- [ ] Game2GameChatMessageComposer
- [ ] Game2LoadStageReadyMessageComposer
- [ ] Game2PlayAgainMessageComposer

#### Domain avatar
- [ ] AvatarExpressionMessageComposer
- [ ] ChangeMottoMessageComposer
- [ ] ChangePostureMessageComposer
- [ ] ChangeUserNameMessageComposer
- [ ] ChangeUserNameInRoomMessageComposer
- [ ] CheckUserNameMessageComposer
- [ ] CustomizeAvatarWithFurniMessageComposer
- [ ] DropCarryItemMessageComposer
- [ ] GetWardrobeMessageComposer
- [ ] LookToMessageComposer
- [ ] PassCarryItemMessageComposer
- [ ] PassCarryItemToPetMessageComposer
- [ ] SaveWardrobeOutfitMessageComposer
- [ ] SignMessageComposer

#### Domain badges
- [ ] GetBadgePointLimitsComposer
- [ ] GetBadgesComposer
- [ ] GetIsBadgeRequestFulfilledComposer
- [ ] RequestABadgeComposer
- [ ] SetActivatedBadgesComposer

#### Domain bots
- [ ] CommandBotComposer
- [ ] GetBotCommandConfigurationDataComposer
- [ ] GetBotInventoryComposer

#### Domain camera
- [ ] PhotoCompetitionMessageComposer
- [ ] PublishPhotoMessageComposer
- [ ] PurchasePhotoMessageComposer
- [ ] RenderRoomMessageComposer
- [ ] RenderRoomThumbnailMessageComposer
- [ ] RequestCameraConfigurationMessageComposer

#### Domain campaign
- [ ] OpenCampaignCalendarDoorComposer
- [ ] OpenCampaignCalendarDoorAsStaffComposer

#### Domain catalog
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
- [ ] PurchaseFromCatalogComposer
- [ ] PurchaseFromCatalogAsGiftComposer
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

#### Domain chat
- [ ] CancelTypingMessageComposer
- [ ] StartTypingMessageComposer

#### Domain collectibles
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

#### Domain competition
- [ ] ForwardToACompetitionRoomMessageComposer
- [ ] ForwardToASubmittableRoomMessageComposer
- [ ] ForwardToRandomCompetitionRoomMessageComposer
- [ ] GetCurrentTimingCodeMessageComposer
- [ ] GetIsUserPartOfCompetitionMessageComposer
- [ ] GetSecondsUntilMessageComposer
- [ ] RoomCompetitionInitMessageComposer
- [ ] SubmitRoomToCompetitionMessageComposer
- [ ] VoteForRoomMessageComposer

#### Domain crafting
- [ ] CraftComposer
- [ ] CraftSecretComposer
- [ ] GetCraftableProductsComposer
- [ ] GetCraftingRecipeComposer
- [ ] GetCraftingRecipesAvailableComposer

#### Domain customfilter
- [ ] AddToCustomFilterMessageComposer
- [ ] GetCustomFilterMessageComposer
- [ ] RemoveFromCustomFilterMessageComposer

#### Domain directory
- [ ] Game2CheckGameDirectoryStatusMessageComposer
- [ ] Game2GetAccountGameStatusMessageComposer
- [ ] Game2LeaveGameMessageComposer
- [ ] Game2QuickJoinGameMessageComposer
- [ ] Game2StartSnowWarMessageComposer

#### Domain engine
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

#### Domain friendfurni
- [ ] FriendFurniConfirmLockMessageComposer

#### Domain friendlist
- [ ] AcceptFriendMessageComposer
- [ ] DeclineFriendMessageComposer
- [ ] FindNewFriendsMessageComposer
- [ ] FollowFriendMessageComposer
- [ ] FriendListUpdateMessageComposer
- [ ] GetFriendRequestsMessageComposer
- [ ] GetMessengerHistoryComposer
- [ ] HabboSearchMessageComposer
- [ ] RemoveFriendMessageComposer
- [ ] RequestFriendMessageComposer
- [ ] SendMsgMessageComposer
- [ ] SendRoomInviteMessageComposer
- [ ] SetRelationshipStatusMessageComposer
- [ ] VisitUserMessageComposer

#### Domain furni
- [ ] RequestFurniInventoryComposer
- [ ] RequestFurniInventoryWhenNotInRoomComposer

#### Domain furniture
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

#### Domain gifts
- [ ] ResetPhoneNumberStateMessageComposer
- [ ] SetPhoneNumberVerificationStatusMessageComposer
- [ ] TryPhoneNumberMessageComposer
- [ ] VerifyCodeMessageComposer

#### Domain groupforums
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

#### Domain handshake
- [ ] ClientHelloMessageComposer
- [ ] CompleteDiffieHandshakeMessageComposer
- [ ] DisconnectMessageComposer
- [ ] InfoRetrieveMessageComposer
- [ ] PongMessageComposer
- [ ] SSOTicketMessageComposer
- [ ] UniqueIDMessageComposer
- [ ] VersionCheckMessageComposer

#### Domain help
- [ ] AppealCfhMessageComposer
- [ ] CallForHelpMessageComposer
- [ ] CallForHelpFromForumMessageMessageComposer
- [ ] CallForHelpFromForumThreadMessageComposer
- [ ] CallForHelpFromIMMessageComposer
- [ ] CallForHelpFromPhotoMessageComposer
- [ ] CallForHelpFromSelfieMessageComposer
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

#### Domain hotlooks
- [ ] GetHotLooksMessageComposer

#### Domain ingame
- [ ] Game2MakeSnowballMessageComposer
- [ ] Game2RequestFullStatusUpdateMessageComposer
- [ ] Game2SetUserMoveTargetMessageComposer
- [ ] Game2ThrowSnowballAtHumanMessageComposer
- [ ] Game2ThrowSnowballAtPositionMessageComposer

#### Domain landingview
- [ ] GetPromoArticlesMessageComposer

#### Domain layout
- [ ] GetOccupiedTilesMessageComposer
- [ ] GetRoomEntryTileMessageComposer
- [ ] UpdateFloorPropertiesMessageComposer

#### Domain lobby
- [ ] GetResolutionAchievementsMessageComposer
- [ ] GetUserGameAchievementsMessageComposer

#### Domain marketplace
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

#### Domain moderator
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

#### Domain mysterybox
- [ ] MysteryBoxWaitingCanceledMessageComposer

#### Domain navigator
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

#### Domain newnavigator
- [ ] NavigatorAddCollapsedCategoryMessageComposer
- [ ] NavigatorAddSavedSearchComposer
- [ ] NavigatorDeleteSavedSearchComposer
- [ ] NavigatorRemoveCollapsedCategoryMessageComposer
- [ ] NavigatorSetSearchCodeViewModeMessageComposer
- [ ] NewNavigatorInitComposer
- [ ] NewNavigatorSearchComposer

#### Domain nft
- [ ] AddNftToTradeComposer
- [ ] GetNftCreditsMessageComposer
- [ ] GetNftTradeInventoryComposer
- [ ] GetSelectedNftWardrobeOutfitMessageComposer
- [ ] GetSilverMessageComposer
- [ ] GetUserNftWardrobeMessageComposer
- [ ] RemoveNftFromTradeComposer
- [ ] SaveUserNftWardrobeMessageComposer

#### Domain notifications
- [ ] ResetUnseenItemIdsComposer
- [ ] ResetUnseenItemsComposer

#### Domain nux
- [ ] NewUserExperienceGetGiftsMessageComposer
- [ ] NewUserExperienceScriptProceedComposer

#### Domain pets
- [ ] BreedPetsMessageComposer
- [ ] CancelPetBreedingComposer
- [ ] ConfirmPetBreedingComposer
- [ ] CustomizePetWithFurniComposer
- [ ] GetPetInfoMessageComposer
- [ ] GetPetInventoryComposer
- [ ] PetSelectedMessageComposer
- [ ] RespectPetMessageComposer

#### Domain poll
- [ ] PollAnswerComposer
- [ ] PollRejectComposer
- [ ] PollStartComposer

#### Domain preferences
- [ ] SetChatPreferencesMessageComposer
- [ ] SetChatStylePreferenceComposer
- [ ] SetIgnoreRoomInvitesMessageComposer
- [ ] SetNewNavigatorWindowPreferencesMessageComposer
- [ ] SetRoomCameraPreferencesMessageComposer
- [ ] SetSoundSettingsComposer
- [ ] SetUIFlagsMessageComposer

#### Domain purse
- [ ] GetCreditsInfoComposer

#### Domain quest
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

#### Domain register
- [ ] UpdateFigureDataMessageComposer

#### Domain roomdirectory
- [ ] RoomNetworkOpenConnectionMessageComposer

#### Domain roomsettings
- [ ] DeleteRoomMessageComposer
- [ ] GetBannedUsersFromRoomMessageComposer
- [ ] GetCustomRoomFilterMessageComposer
- [ ] GetFlatControllersMessageComposer
- [ ] GetRoomSettingsMessageComposer
- [ ] SaveRoomSettingsMessageComposer
- [ ] UpdateRoomCategoryAndTradeSettingsComposer
- [ ] UpdateRoomFilterMessageComposer

#### Domain score
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

#### Domain session
- [ ] ChangeQueueMessageComposer
- [ ] OpenFlatConnectionMessageComposer
- [ ] QuitMessageComposer

#### Domain sound
- [ ] AddJukeboxDiskComposer
- [ ] GetJukeboxPlayListMessageComposer
- [ ] GetNowPlayingMessageComposer
- [ ] GetOfficialSongIdMessageComposer
- [ ] GetSongInfoMessageComposer
- [ ] GetSoundMachinePlayListMessageComposer
- [ ] GetSoundSettingsComposer
- [ ] GetUserSongDisksMessageComposer
- [ ] RemoveJukeboxDiskComposer

#### Domain talent
- [ ] GetTalentTrackMessageComposer
- [ ] GetTalentTrackLevelMessageComposer
- [ ] GuideAdvertisementReadMessageComposer

#### Domain tracking
- [ ] EventLogMessageComposer
- [ ] LagWarningReportMessageComposer
- [ ] LatencyPingReportMessageComposer
- [ ] LatencyPingRequestMessageComposer
- [ ] PerformanceLogMessageComposer

#### Domain trading
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

#### Domain treasurehunt
- [ ] ProgressTreasureHuntMessageComposer

#### Domain userclassification
- [ ] PeerUsersClassificationMessageComposer
- [ ] RoomUsersClassificationMessageComposer

#### Domain userdefinedroomevents
- [ ] ApplySnapshotMessageComposer
- [ ] UpdateActionMessageComposer
- [ ] UpdateAddonMessageComposer
- [ ] UpdateConditionMessageComposer
- [ ] UpdateSelectorMessageComposer
- [ ] UpdateTriggerMessageComposer
- [ ] UpdateVariableMessageComposer
- [ ] WiredClickUserMessageComposer

#### Domain users
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
- [ ] GetExtendedProfileMessageComposer
- [ ] GetExtendedProfileByNameMessageComposer
- [ ] GetGuildCreationInfoMessageComposer
- [ ] GetGuildEditInfoMessageComposer
- [ ] GetGuildEditorDataMessageComposer
- [ ] GetGuildMembersMessageComposer
- [ ] GetGuildMembershipsMessageComposer
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

#### Domain variablesmanagement
- [ ] WiredGetUserPermanentVariablesComposer
- [ ] WiredGetVariableOwnersPageComposer
- [ ] WiredSetUserPermanentVariableComposer

#### Domain vault
- [ ] IncomeRewardClaimMessageComposer
- [ ] WithdrawCreditVaultMessageComposer

#### Domain votes
- [ ] CommunityGoalVoteMessageComposer

#### Domain wiredmenu
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

## P2 - Divergences emu seulement
incoming emu sans source AS3: 501
outgoing emu sans source AS3: 492

Exemples incoming emu:
- GetAchievementsMessage
- AmbassadorAlertMessage
- AssignRightsMessage
- BanUserWithDurationMessage
- KickUserMessage
- LetUserInMessage
- MuteAllInRoomMessage
- MuteUserMessage
- RemoveAllRightsMessage
- RemoveRightsMessage
- UnbanUserFromRoomMessage
- UnmuteUserMessage
- GetInterstitialMessage
- InterstitialShownMessage
- Game2ExitGameMessage
- Game2GameChatMessage
- Game2LoadStageReadyMessage
- Game2PlayAgainMessage
- AvatarExpressionMessage
- ChangeMottoMessage
- ChangePostureMessage
- ChangeUserNameInRoomMessage
- ChangeUserNameMessage
- CheckUserNameMessage
- CustomizeAvatarWithFurniMessage
- DanceMessage
- DropCarryItemMessage
- GetWardrobeMessage
- LookToMessage
- PassCarryItemMessage
- PassCarryItemToPetMessage
- SaveWardrobeOutfitMessage
- SignMessage
- AvatarEffectActivatedMessage
- AvatarEffectSelectedMessage
- GetBadgePointLimitsMessage
- GetBadgesMessage
- GetIsBadgeRequestFulfilledMessage
- RequestABadgeMessage
- SetActivatedBadgesMessage
- CommandBotMessage
- GetBotCommandConfigurationDataMessage
- GetBotInventoryMessage
- PhotoCompetitionMessage
- PublishPhotoMessage
- PurchasePhotoMessage
- RenderRoomMessage
- RequestCameraConfigurationMessage
- OpenCampaignCalendarDoorAsStaffMessage
- OpenCampaignCalendarDoorMessage
- BuildersClubPlaceRoomItemMessage
- BuildersClubPlaceWallItemMessage
- BuildersClubQueryFurniCountMessage
- ChargeFireworkMessage
- GetBonusRareInfoMessage
- GetBundleDiscountRulesetMessage
- GetCatalogIndexMessage
- GetCatalogPageMessage
- GetCatalogPageWithEarliestExpiryMessage
- GetClubGiftInfoMessage
- GetClubOffersMessage
- GetGiftWrappingConfigurationMessage
- GetHabboClubExtendOfferMessage
- GetIsOfferGiftableMessage
- GetLimitedOfferAppearingNextMessage
- GetNextTargetedOfferMessage
- GetProductOfferMessage
- GetRoomAdPurchaseInfoMessage
- GetSeasonalCalendarDailyOfferMessage
- GetSellablePetPalettesMessage
- GetTargetedOfferMessage
- MarkCatalogNewAdditionsPageOpenedMessage
- PurchaseBasicMembershipExtensionMessage
- PurchaseFromCatalogAsGiftMessage
- PurchaseFromCatalogMessage
- PurchaseRoomAdMessageMessage
- PurchaseTargetedOfferMessage
- PurchaseVipMembershipExtensionMessage
- RedeemVoucherMessage
- RoomAdPurchaseInitiatedMessage
- SelectClubGiftMessage
- SetTargetedOfferStateMessage
- ShopTargetedOfferViewedMessage
- CancelTypingMessage
- ChatMessage
- ShoutMessage
- StartTypingMessage
- WhisperMessage
- GetCollectibleMintableItemTypesMessage
- GetCollectibleMintingEnabledMessage
- GetCollectibleMintTokensMessage
- GetCollectibleWalletAddressesMessage
- GetCollectorScoreMessage
- GetMintTokenOffersMessage
- GetNftCollectionsMessage
- GetNftTransferFeeMessage
- MintItemMessage
- NftCollectiblesClaimBonusItemMessage
- NftCollectiblesClaimRewardItemMessage
- NftTransferAssetsMessage
- PurchaseMintTokenMessage
- ForwardToACompetitionRoomMessage
- ForwardToASubmittableRoomMessage
- ForwardToRandomCompetitionRoomMessage
- GetCurrentTimingCodeMessage
- GetIsUserPartOfCompetitionMessage
- GetSecondsUntilMessage
- RoomCompetitionInitMessage
- SubmitRoomToCompetitionMessage
- VoteForRoomMessage
- CraftMessage
- CraftSecretMessage
- GetCraftableProductsMessage
- GetCraftingRecipeMessage
- GetCraftingRecipesAvailableMessage
- Game2CheckGameDirectoryStatusMessage
- Game2GetAccountGameStatusMessage
- Game2LeaveGameMessage
- Game2QuickJoinGameMessage
- Game2StartSnowWarMessage
Exemples outgoing emu:
- AchievementEventMessageComposer
- AchievementsEventMessageComposer
- AchievementsScoreEventMessageComposer
- AvatarEffectMessageComposer
- CarryObjectMessageComposer
- ExpressionMessageComposer
- SleepMessageComposer
- UseObjectMessageComposer
- InterstitialMessageComposer
- RoomAdErrorEventMessageComposer
- Game2ArenaEnteredMessageComposer
- Game2EnterArenaMessageComposer
- Game2EnterArenaFailedMessageComposer
- Game2GameChatFromPlayerMessageComposer
- Game2GameEndingMessageComposer
- Game2GameRejoinMessageComposer
- Game2PlayerExitedGameArenaMessageComposer
- Game2PlayerRematchesMessageComposer
- Game2StageEndingMessageComposer
- Game2StageLoadMessageComposer
- Game2StageRunningMessageComposer
- Game2StageStartingMessageComposer
- Game2StageStillLoadingMessageComposer
- AvailabilityStatusMessageComposer
- InfoHotelClosedMessageComposer
- InfoHotelClosingMessageComposer
- LoginFailedHotelClosedMessageComposer
- MaintenanceStatusMessageComposer
- ChangeUserNameResultMessageComposer
- CheckUserNameResultMessageComposer
- FigureUpdateEventMessageComposer
- WardrobeMessageComposer
- AvatarEffectAddedMessageComposer
- AvatarEffectExpiredMessageComposer
- AvatarEffectsMessageComposer
- BadgePointLimitsEventMessageComposer
- BadgeReceivedEventMessageComposer
- BadgesEventMessageComposer
- IsBadgeRequestFulfilledEventMessageComposer
- BotAddedToInventoryEventMessageComposer
- BotCommandConfigurationMessageComposer
- BotErrorMessageComposer
- BotForceOpenContextMenuMessageComposer
- BotInventoryEventMessageComposer
- BotRemovedFromInventoryEventMessageComposer
- BotSkillListUpdateMessageComposer
- class_1600MessageComposer
- CfhSanctionMessageComposer
- CfhTopicsInitMessageComposer
- SanctionStatusEventMessageComposer
- CameraPublishStatusMessageComposer
- CameraPurchaseOKMessageComposer
- CameraStorageUrlMessageComposer
- class_1476MessageComposer
- CompetitionStatusMessageComposer
- InitCameraMessageComposer
- ThumbnailStatusMessageComposer
- CampaignCalendarDataMessageComposer
- CampaignCalendarDoorOpenedMessageComposer
- BonusRareInfoMessageComposer
- BuildersClubSubscriptionStatusMessageComposer
- BundleDiscountRulesetMessageComposer
- CatalogIndexMessageComposer
- CatalogPageMessageComposer
- CatalogPageWithEarliestExpiryMessageComposer
- CatalogPublishedMessageComposer
- ClubGiftInfoEventMessageComposer
- ClubGiftSelectedEventMessageComposer
- FigureSetIdsMessage
- GiftReceiverNotFoundEventMessageComposer
- GiftWrappingConfigurationEventMessageComposer
- HabboClubExtendOfferMessageComposer
- HabboClubOffersMessageComposer
- LimitedEditionSoldOutEventMessageComposer
- LimitedOfferAppearingNextMessageComposer
- NotEnoughBalanceMessageComposer
- ProductOfferEventMessageComposer
- PurchaseErrorMessageComposer
- PurchaseNotAllowedMessageComposer
- PurchaseOKMessageComposer
- RoomAdPurchaseInfoEventMessageComposer
- SeasonalCalendarDailyOfferMessageComposer
- SellablePetPalettesMessageComposer
- SnowWarGameTokensMessageMessageComposer
- TargetedOfferEventMessageComposer
- TargetedOfferNotFoundEventMessageComposer
- VoucherRedeemErrorMessageComposer
- VoucherRedeemOkMessageComposer
- FloodControlMessageComposer
- RemainingMutePeriodMessageComposer
- RoomChatSettingsMessageComposer
- RoomFilterSettingsMessageComposer
- UserTypingMessageComposer
- class_1487MessageComposer
- class_1594MessageComposer
- FigureSetIdsEventMessageComposer
- CollectableMintableItemTypesMessageComposer
- CollectibleMintableItemResultMessageComposer
- CollectibleMintingEnabledMessageComposer
- CollectibleMintTokenCountMessageComposer
- CollectibleMintTokenOffersMessageComposer
- CollectibleWalletAddressesMessageComposer
- EmeraldBalanceMessageComposer
- NftBonusItemClaimResultMessageComposer
- NftCollectionsMessageComposer
- NftCollectionsScoreMessageComposer
- NftRewardItemClaimResultMessageComposer
- NftTransferAssetsResultMessageComposer
- NftTransferFeeMessageComposer
- SilverBalanceMessageComposer
- CompetitionEntrySubmitResultMessageComposer
- CompetitionVotingInfoMessageComposer
- CurrentTimingCodeMessageComposer
- IsUserPartOfCompetitionMessageComposer
- NoOwnedRoomsAlertMessageComposer
- SecondsUntilMessageComposer
- CraftableProductsMessageComposer
- CraftingRecipeMessageComposer
- CraftingRecipesAvailableMessageComposer
- CraftingResultMessageComposer

## Notes
- Les noms ont ete normalises en supprimant les suffixes MessageEvent, MessageComposer, Parser, Serializer, EventMessageComposer, EventMessageParser.
- Les classes parser c# sont compares avec les parser AS3 en enlevant les suffixes et les classes class_*.as ont ete ignorees.
