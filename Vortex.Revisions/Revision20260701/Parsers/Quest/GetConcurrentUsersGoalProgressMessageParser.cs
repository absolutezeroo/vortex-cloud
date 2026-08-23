using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Quest;

namespace Vortex.Revisions.Revision20260701.Parsers.Quest;

internal class GetConcurrentUsersGoalProgressMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetConcurrentUsersGoalProgressMessage();
}
