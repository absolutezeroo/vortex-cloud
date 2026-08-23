using Vortex.Primitives.Networking.Revisions;
using Vortex.Protocol.Messages.Outgoing.Userclassification;
using Vortex.Revisions.Revision20260701.Parsers.UserClassification;
using Vortex.Revisions.Revision20260701.Serializers.UserClassification;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class UserClassificationMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.PeerUsersClassificationMessageEvent,
            new PeerUsersClassificationMessageParser()
        );
        builder.MapParser(
            MessageEvent.RoomUsersClassificationMessageEvent,
            new RoomUsersClassificationMessageParser()
        );

        builder.MapSerializer(
            typeof(UserClassificationMessageComposer),
            new UserClassificationMessageComposerSerializer(
                MessageComposer.UserClassificationComposer
            )
        );
    }
}
