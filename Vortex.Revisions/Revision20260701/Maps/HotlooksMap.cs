using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Hotlooks;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class HotlooksMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(MessageEvent.GetHotLooksMessageEvent, new GetHotLooksMessageParser());
    }
}
