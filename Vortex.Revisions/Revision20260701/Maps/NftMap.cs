using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Nft;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class NftMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(MessageEvent.GetNftCreditsMessageEvent, new GetNftCreditsMessageParser());
        builder.MapParser(
            MessageEvent.GetSelectedNftWardrobeOutfitMessageEvent,
            new GetSelectedNftWardrobeOutfitMessageParser()
        );
        builder.MapParser(MessageEvent.GetSilverMessageEvent, new GetSilverMessageParser());
        builder.MapParser(
            MessageEvent.GetUserNftWardrobeMessageEvent,
            new GetUserNftWardrobeMessageParser()
        );
        builder.MapParser(
            MessageEvent.SaveUserNftWardrobeMessageEvent,
            new SaveUserNftWardrobeMessageParser()
        );
    }
}
