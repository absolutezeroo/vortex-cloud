using Microsoft.Extensions.Options;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Configuration;
using Vortex.Revisions.Revision20260701.Maps;

namespace Vortex.Revisions.Revision20260701;

public sealed class Revision20260701(IOptions<ProtocolLimitsConfig> protocolLimits)
    : RevisionBase(
        new IRevisionMap[]
        {
            new AdvertisementMap(),
            new AvatarMap(),
            new CameraMap(),
            new CampaignMap(),
            new CatalogMap(),
            new CollectiblesMap(protocolLimits.Value),
            new CompetitionMap(),
            new CraftingMap(),
            new FriendFurniMap(),
            new FriendListMap(protocolLimits.Value),
            new GameMap(),
            new GiftsMap(),
            new GroupForumsMap(),
            new HandshakeMap(),
            new HelpMap(protocolLimits.Value),
            new HotlooksMap(),
            new InventoryMap(protocolLimits.Value),
            new LandingViewMap(),
            new MarketplaceMap(),
            new ModeratorMap(),
            new MysteryBoxMap(),
            new NavigatorMap(),
            new NewNavigatorMap(),
            new NftMap(),
            new NotificationsMap(),
            new NuxMap(),
            new PollMap(),
            new PreferencesMap(),
            new QuestMap(),
            new RegisterMap(),
            new RoomMap(),
            new RoomDirectoryMap(),
            new RoomSettingsMap(protocolLimits.Value),
            new SoundMap(),
            new TalentMap(),
            new TrackingMap(),
            new UserClassificationMap(),
            new UserDefinedRoomEventsMap(),
            new UsersMap(),
            new VaultMap(),
            new AvailabilityMap(),
            new CallForHelpMap(),
            new PerkMap(),
        }
    )
{
    public override string Revision => "WIN63-202607011411-782849652";
}
