using Vortex.Primitives.Packets;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Polls.Snapshots;

namespace Vortex.Revisions.Revision20260701.Serializers.Poll;

/// <summary>
/// Writes one question block, shared by the poll-contents and word-quiz composers because the
/// client parses both with the same routine.
/// </summary>
/// <remarks>
/// Field order is the client's <c>parseQuestion</c>: id, sort order, type, text, category, answer
/// type, answer count. The choice list follows <b>only</b> for the single- and multiple-choice
/// types — the client reads it under that same condition, so writing choices for a text question
/// would desynchronise every field after it.
/// </remarks>
internal static class PollQuestionWriter
{
    public static void Write(IServerPacket packet, PollQuestionSnapshot question)
    {
        bool hasChoices =
            question.QuestionType
            is PollQuestionType.SingleChoice
                or PollQuestionType.MultipleChoice;

        packet.WriteInteger(question.Id);
        packet.WriteInteger(question.SortOrder);
        packet.WriteInteger((int)question.QuestionType);
        packet.WriteString(question.QuestionText);
        packet.WriteInteger(question.QuestionCategory);
        packet.WriteInteger(question.QuestionAnswerType);
        packet.WriteInteger(hasChoices ? question.Choices.Length : 0);

        if (!hasChoices)
        {
            return;
        }

        foreach (PollChoiceSnapshot choice in question.Choices)
        {
            packet.WriteString(choice.Value);
            packet.WriteString(choice.ChoiceText);
            packet.WriteInteger(choice.ChoiceType);
        }
    }
}
