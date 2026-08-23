using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Help;

namespace Vortex.Revisions.Revision20260701.Parsers.Help;

internal class GetQuizQuestionsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetQuizQuestionsMessage { QuizCode = packet.PopString() };
}
