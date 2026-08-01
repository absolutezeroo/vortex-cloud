using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.UserClassification;

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
    }
}
