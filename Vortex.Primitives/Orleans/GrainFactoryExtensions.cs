using Orleans;
using Vortex.Primitives.Catalog.Grains;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Groups.Grains;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Marketplace.Grains;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Quests.Grains;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Server.Grains;

namespace Vortex.Primitives.Orleans;

public static class GrainFactoryExtensions
{
    public static IRoomGrain GetRoomGrain(this IGrainFactory factory, RoomId roomId) =>
        factory.GetGrain<IRoomGrain>((long)roomId.Value);

    public static IRoomPersistenceGrain GetRoomPersistenceGrain(
        this IGrainFactory factory,
        RoomId roomId
    ) => factory.GetGrain<IRoomPersistenceGrain>((long)roomId.Value);

    public static IRoomDirectoryGrain GetRoomDirectoryGrain(this IGrainFactory factory) =>
        factory.GetGrain<IRoomDirectoryGrain>(SingletonGrainId.GLOBAL);

    public static IPlayerGrain GetPlayerGrain(this IGrainFactory factory, PlayerId playerId) =>
        factory.GetGrain<IPlayerGrain>((long)playerId.Value);

    public static IPlayerPresenceGrain GetPlayerPresenceGrain(
        this IGrainFactory factory,
        PlayerId playerId
    ) => factory.GetGrain<IPlayerPresenceGrain>(playerId.Value);

    public static IPlayerPresenceGrain GetPlayerPresenceGrain(
        this IGrainFactory factory,
        long playerId
    ) => factory.GetGrain<IPlayerPresenceGrain>(playerId);

    public static IPlayerDirectoryGrain GetPlayerDirectoryGrain(this IGrainFactory factory) =>
        factory.GetGrain<IPlayerDirectoryGrain>(SingletonGrainId.GLOBAL);

    public static IPlayerWalletGrain GetPlayerWalletGrain(
        this IGrainFactory factory,
        PlayerId playerId
    ) => factory.GetGrain<IPlayerWalletGrain>(playerId.Value);

    public static IPlayerWalletGrain GetPlayerWalletGrain(
        this IGrainFactory factory,
        long playerId
    ) => factory.GetGrain<IPlayerWalletGrain>(playerId);

    public static IInventoryGrain GetInventoryGrain(
        this IGrainFactory factory,
        PlayerId playerId
    ) => factory.GetGrain<IInventoryGrain>(playerId.Value);

    public static IInventoryGrain GetInventoryGrain(this IGrainFactory factory, long playerId) =>
        factory.GetGrain<IInventoryGrain>(playerId);

    public static ICatalogPurchaseGrain GetCatalogPurchaseGrain(
        this IGrainFactory factory,
        PlayerId playerId
    ) => factory.GetGrain<ICatalogPurchaseGrain>(playerId.Value);

    public static ICatalogPurchaseGrain GetCatalogPurchaseGrain(
        this IGrainFactory factory,
        long playerId
    ) => factory.GetGrain<ICatalogPurchaseGrain>(playerId);

    public static IPlayerNavigatorGrain GetPlayerNavigatorGrain(
        this IGrainFactory factory,
        PlayerId playerId
    ) => factory.GetGrain<IPlayerNavigatorGrain>(playerId.Value);

    public static IPlayerNavigatorGrain GetPlayerNavigatorGrain(
        this IGrainFactory factory,
        long playerId
    ) => factory.GetGrain<IPlayerNavigatorGrain>(playerId);

    public static IMarketplacePurchaseGrain GetMarketplacePurchaseGrain(
        this IGrainFactory factory,
        PlayerId playerId
    ) => factory.GetGrain<IMarketplacePurchaseGrain>(playerId.Value);

    public static IMarketplacePurchaseGrain GetMarketplacePurchaseGrain(
        this IGrainFactory factory,
        long playerId
    ) => factory.GetGrain<IMarketplacePurchaseGrain>(playerId);

    public static IMarketplaceSearchGrain GetMarketplaceSearchGrain(this IGrainFactory factory) =>
        factory.GetGrain<IMarketplaceSearchGrain>(SingletonGrainId.GLOBAL);

    public static IPlayerVaultGrain GetPlayerVaultGrain(
        this IGrainFactory factory,
        PlayerId playerId
    ) => factory.GetGrain<IPlayerVaultGrain>(playerId.Value);

    public static IPlayerVaultGrain GetPlayerVaultGrain(
        this IGrainFactory factory,
        long playerId
    ) => factory.GetGrain<IPlayerVaultGrain>(playerId);

    public static IMessengerGrain GetMessengerGrain(
        this IGrainFactory factory,
        PlayerId playerId
    ) => factory.GetGrain<IMessengerGrain>(playerId.Value);

    public static IMessengerGrain GetMessengerGrain(this IGrainFactory factory, long playerId) =>
        factory.GetGrain<IMessengerGrain>(playerId);

    public static ILtdRaffleGrain GetLtdRaffleGrain(this IGrainFactory factory, int seriesId) =>
        factory.GetGrain<ILtdRaffleGrain>(seriesId);

    public static IVoucherGrain GetVoucherGrain(this IGrainFactory factory, string code) =>
        factory.GetGrain<IVoucherGrain>(code.Trim().ToUpperInvariant());

    public static IGroupGrain GetGroupGrain(this IGrainFactory factory, int groupId) =>
        factory.GetGrain<IGroupGrain>(groupId);

    public static IGroupDirectoryGrain GetGroupDirectoryGrain(this IGrainFactory factory) =>
        factory.GetGrain<IGroupDirectoryGrain>(SingletonGrainId.GLOBAL);

    public static IGroupForumGrain GetGroupForumGrain(this IGrainFactory factory, int groupId) =>
        factory.GetGrain<IGroupForumGrain>(groupId);

    public static IRentableSpaceGrain GetRentableSpaceGrain(
        this IGrainFactory factory,
        int furnitureId
    ) => factory.GetGrain<IRentableSpaceGrain>(furnitureId);

    public static IPlayerBadgeGrain GetPlayerBadgeGrain(
        this IGrainFactory factory,
        PlayerId playerId
    ) => factory.GetGrain<IPlayerBadgeGrain>(playerId.Value);

    public static IPlayerBadgeGrain GetPlayerBadgeGrain(
        this IGrainFactory factory,
        long playerId
    ) => factory.GetGrain<IPlayerBadgeGrain>(playerId);

    public static IAchievementManagerGrain GetAchievementManagerGrain(this IGrainFactory factory) =>
        factory.GetGrain<IAchievementManagerGrain>(SingletonGrainId.GLOBAL);

    public static IServerConfigGrain GetServerConfigGrain(this IGrainFactory factory) =>
        factory.GetGrain<IServerConfigGrain>(SingletonGrainId.GLOBAL);

    public static IPlayerAchievementGrain GetPlayerAchievementGrain(
        this IGrainFactory factory,
        PlayerId playerId
    ) => factory.GetGrain<IPlayerAchievementGrain>(playerId.Value);

    public static IPlayerAchievementGrain GetPlayerAchievementGrain(
        this IGrainFactory factory,
        long playerId
    ) => factory.GetGrain<IPlayerAchievementGrain>(playerId);

    public static IQuestManagerGrain GetQuestManagerGrain(this IGrainFactory factory) =>
        factory.GetGrain<IQuestManagerGrain>(SingletonGrainId.GLOBAL);

    public static IPlayerQuestGrain GetPlayerQuestGrain(
        this IGrainFactory factory,
        PlayerId playerId
    ) => factory.GetGrain<IPlayerQuestGrain>(playerId.Value);

    public static IPlayerQuestGrain GetPlayerQuestGrain(
        this IGrainFactory factory,
        long playerId
    ) => factory.GetGrain<IPlayerQuestGrain>(playerId);

    public static ITargetedOfferManagerGrain GetTargetedOfferManagerGrain(
        this IGrainFactory factory
    ) => factory.GetGrain<ITargetedOfferManagerGrain>(SingletonGrainId.GLOBAL);

    public static IPlayerTargetedOfferGrain GetPlayerTargetedOfferGrain(
        this IGrainFactory factory,
        long playerId
    ) => factory.GetGrain<IPlayerTargetedOfferGrain>(playerId);
}
