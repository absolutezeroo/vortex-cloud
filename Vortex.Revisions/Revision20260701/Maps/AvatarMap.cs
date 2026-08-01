using Vortex.Primitives.Messages.Outgoing.Avatar;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Avatar;
using Vortex.Revisions.Revision20260701.Serializers.Avatar;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class AvatarMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.ChangeUserNameInRoomMessageEvent,
            new ChangeUserNameInRoomMessageParser()
        );
        builder.MapParser(
            MessageEvent.ChangeUserNameMessageEvent,
            new ChangeUserNameMessageParser()
        );
        builder.MapParser(MessageEvent.CheckUserNameMessageEvent, new CheckUserNameMessageParser());
        builder.MapParser(MessageEvent.GetWardrobeMessageEvent, new GetWardrobeMessageParser());
        builder.MapParser(
            MessageEvent.SaveWardrobeOutfitMessageEvent,
            new SaveWardrobeOutfitMessageParser()
        );

        builder.MapSerializer(
            typeof(ChangeUserNameResultMessageComposer),
            new ChangeUserNameResultMessageComposerSerializer(
                MessageComposer.ChangeUserNameResultMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(CheckUserNameResultMessageComposer),
            new CheckUserNameResultMessageComposerSerializer(
                MessageComposer.CheckUserNameResultMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(FigureUpdateEventMessageComposer),
            new FigureUpdateEventMessageComposerSerializer(MessageComposer.FigureUpdateComposer)
        );
        builder.MapSerializer(
            typeof(WardrobeMessageComposer),
            new WardrobeMessageComposerSerializer(MessageComposer.WardrobeMessageComposer)
        );
    }
}
