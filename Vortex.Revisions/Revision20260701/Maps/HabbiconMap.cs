using Vortex.Primitives.Networking.Revisions;
using Vortex.Protocol.Messages.Outgoing.Habbicons;
using Vortex.Revisions.Revision20260701.Parsers.Habbicons;
using Vortex.Revisions.Revision20260701.Serializers.Habbicons;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class HabbiconMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.GetHabbiconShopDataMessageEvent,
            new GetHabbiconShopDataMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetHabbiconInfoMessageEvent,
            new GetHabbiconInfoMessageParser()
        );
        builder.MapParser(MessageEvent.BuyHabbiconMessageEvent, new BuyHabbiconMessageParser());
        builder.MapParser(
            MessageEvent.BuyHabbiconCollectionMessageEvent,
            new BuyHabbiconCollectionMessageParser()
        );
        builder.MapParser(MessageEvent.ClaimHabbiconMessageEvent, new ClaimHabbiconMessageParser());
        builder.MapParser(
            MessageEvent.FavouriteHabbiconMessageEvent,
            new FavouriteHabbiconMessageParser()
        );
        builder.MapParser(
            MessageEvent.UnfavouriteHabbiconMessageEvent,
            new UnfavouriteHabbiconMessageParser()
        );
        builder.MapParser(
            MessageEvent.TriggerHabbiconMessageEvent,
            new TriggerHabbiconMessageParser()
        );
        builder.MapParser(MessageEvent.SendHabbiconMessageEvent, new SendHabbiconMessageParser());

        builder.MapSerializer(
            typeof(UserHabbiconsMessageComposer),
            new UserHabbiconsMessageComposerSerializer(MessageComposer.UserHabbiconsMessageComposer)
        );
        builder.MapSerializer(
            typeof(UserHabbiconStatusChangedMessageComposer),
            new UserHabbiconStatusChangedMessageComposerSerializer(
                MessageComposer.UserHabbiconStatusChangedMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(HabbiconShopDataMessageComposer),
            new HabbiconShopDataMessageComposerSerializer(
                MessageComposer.HabbiconShopDataMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(HabbiconInfoMessageComposer),
            new HabbiconInfoMessageComposerSerializer(MessageComposer.HabbiconInfoMessageComposer)
        );
        builder.MapSerializer(
            typeof(RoomUseHabbiconMessageComposer),
            new RoomUseHabbiconMessageComposerSerializer(
                MessageComposer.RoomUseHabbiconMessageComposer
            )
        );
    }
}
