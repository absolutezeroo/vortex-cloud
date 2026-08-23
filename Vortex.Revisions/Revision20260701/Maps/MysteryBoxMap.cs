using Vortex.Primitives.Networking.Revisions;
using Vortex.Protocol.Messages.Outgoing.Mysterybox;
using Vortex.Revisions.Revision20260701.Parsers.MysteryBox;
using Vortex.Revisions.Revision20260701.Serializers.MysteryBox;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class MysteryBoxMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.MysteryBoxWaitingCanceledMessageEvent,
            new MysteryBoxWaitingCanceledMessageParser()
        );

        builder.MapSerializer(
            typeof(CancelMysteryBoxWaitMessageComposer),
            new CancelMysteryBoxWaitMessageComposerSerializer(
                MessageComposer.CancelMysteryBoxWaitMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(GotMysteryBoxPrizeMessageComposer),
            new GotMysteryBoxPrizeMessageComposerSerializer(
                MessageComposer.GotMysteryBoxPrizeMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(MysteryBoxKeysMessageComposer),
            new MysteryBoxKeysMessageComposerSerializer(
                MessageComposer.MysteryBoxKeysMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(ShowMysteryBoxWaitMessageComposer),
            new ShowMysteryBoxWaitMessageComposerSerializer(
                MessageComposer.ShowMysteryBoxWaitMessageComposer
            )
        );
    }
}
