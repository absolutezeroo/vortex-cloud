using Vortex.Protocol.Messages.Outgoing.Avatar;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Avatar;
using Vortex.Revisions.Revision20260701.Serializers.Avatar;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class AvatarMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.ChangeUserNameMessageEvent,
            new ChangeUserNameMessageParser()
        );
        // Same parser, second id: the onboarding dialog claims names on 879 (see Headers.cs).
        builder.MapParser(
            MessageEvent.ClaimNewUserNameMessageEvent,
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
