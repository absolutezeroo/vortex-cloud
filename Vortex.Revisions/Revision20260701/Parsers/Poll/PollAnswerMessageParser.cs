using Vortex.Primitives.Messages.Incoming.Poll;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Poll;

internal class PollAnswerMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new PollAnswerMessage();
}
