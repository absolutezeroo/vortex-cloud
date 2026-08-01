using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Gifts;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class GiftsMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.ResetPhoneNumberStateMessageEvent,
            new ResetPhoneNumberStateMessageParser()
        );
        builder.MapParser(
            MessageEvent.SetPhoneNumberVerificationStatusMessageEvent,
            new SetPhoneNumberVerificationStatusMessageParser()
        );
        builder.MapParser(
            MessageEvent.TryPhoneNumberMessageEvent,
            new TryPhoneNumberMessageParser()
        );
        builder.MapParser(MessageEvent.VerifyCodeMessageEvent, new VerifyCodeMessageParser());
    }
}
