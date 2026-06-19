using Turbo.Primitives.Messages.Incoming.Groupforums;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Groupforums;

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
