using Turbo.Primitives.Messages.Incoming.GroupForums;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.GroupForums;

internal class UpdateThreadMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UpdateThreadMessage
        {
            GroupId = packet.PopInt(),
            ThreadId = packet.PopInt(),
            IsLocked = packet.PopBoolean(),
            IsSticky = packet.PopBoolean(),
        };
}
