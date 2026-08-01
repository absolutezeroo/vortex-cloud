using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.FriendFurni;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class FriendFurniMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.FriendFurniConfirmLockMessageEvent,
            new FriendFurniConfirmLockMessageParser()
        );
    }
}
