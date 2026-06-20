using Orleans;
using Turbo.Primitives.Catalog.Grains;
using Turbo.Primitives.FriendList.Grains;
using Turbo.Primitives.Groups.Grains;
using Turbo.Primitives.Inventory.Grains;
using Turbo.Primitives.Marketplace.Grains;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.Primitives.Orleans;

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
}
