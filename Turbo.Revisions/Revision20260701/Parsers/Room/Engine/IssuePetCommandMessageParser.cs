using Turbo.Primitives.Messages.Incoming.Room.Engine;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Parsers.Room.Engine;

internal class IssuePetCommandMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new IssuePetCommandMessage { PetId = packet.PopInt(), CommandId = packet.PopInt() };
}
