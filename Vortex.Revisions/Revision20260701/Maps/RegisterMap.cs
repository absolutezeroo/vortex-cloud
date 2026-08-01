using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Register;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class RegisterMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.UpdateFigureDataMessageEvent,
            new UpdateFigureDataMessageParser()
        );
    }
}
