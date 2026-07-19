using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Users;

internal class UpdateGuildColorsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UpdateGuildColorsMessage
        {
            GroupId = packet.PopInt(),
            PrimaryColorId = packet.PopInt(),
            SecondaryColorId = packet.PopInt(),
        };
}
