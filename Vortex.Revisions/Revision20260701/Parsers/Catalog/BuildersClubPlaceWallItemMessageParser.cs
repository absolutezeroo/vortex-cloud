using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Catalog;

internal class BuildersClubPlaceWallItemMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new BuildersClubPlaceWallItemMessage
        {
            PageId = packet.PopInt(),
            OfferId = packet.PopInt(),
            ExtraParam = packet.PopString(),
            Location = packet.PopString(),
            ConfirmHideRoom = packet.PopBoolean(),
        };
}
