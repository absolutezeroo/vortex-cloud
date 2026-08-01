using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Nux;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class NuxMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.NewUserExperienceGetGiftsMessageEvent,
            new NewUserExperienceGetGiftsMessageParser()
        );
        builder.MapParser(
            MessageEvent.NewUserExperienceScriptProceedEvent,
            new NewUserExperienceScriptProceedMessageParser()
        );
        builder.MapParser(
            MessageEvent.SelectInitialRoomEvent,
            new SelectInitialRoomMessageParser()
        );
    }
}
