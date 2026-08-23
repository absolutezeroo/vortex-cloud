using System.Collections.Immutable;
using System.IO;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Help;

namespace Vortex.Revisions.Revision20260701.Parsers.Help;

internal class PostQuizAnswersMessageParser(int maxQuizAnswers) : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        string quizCode = packet.PopString();
        int count = packet.PopInt();

        if (count < 0 || count > maxQuizAnswers)
        {
            throw new InvalidDataException(
                $"Client declared an invalid quiz answer count of {count} (max {maxQuizAnswers})."
            );
        }

        ImmutableArray<int>.Builder answers = ImmutableArray.CreateBuilder<int>(count);

        for (int i = 0; i < count; i++)
        {
            answers.Add(packet.PopInt());
        }

        return new PostQuizAnswersMessage { QuizCode = quizCode, Answers = answers.ToImmutable() };
    }
}
