using Vortex.Primitives.Messages.Incoming.GroupForums;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.GroupForums;

internal class UpdateForumSettingsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UpdateForumSettingsMessage
        {
            GroupId = packet.PopInt(),
            ReadPermission = packet.PopInt(),
            PostMessagePermission = packet.PopInt(),
            PostThreadPermission = packet.PopInt(),
            ModeratePermission = packet.PopInt(),
        };
}
