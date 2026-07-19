using Vortex.Primitives.Messages.Incoming.Catalog;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Catalog;

public class BuildersClubPlaceRoomItemMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new BuildersClubPlaceRoomItemMessage
        {
            PageId = packet.PopInt(),
            OfferId = packet.PopInt(),
            ExtraParam = packet.PopString(),
            X = packet.PopInt(),
            Y = packet.PopInt(),
            Direction = packet.PopInt(),
            ConfirmHideRoom = packet.PopBoolean(),
        };
}
