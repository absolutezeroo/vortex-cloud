using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.RoomDirectory;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class RoomDirectoryMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.RoomNetworkOpenConnectionMessageEvent,
            new RoomNetworkOpenConnectionMessageParser()
        );
    }
}
