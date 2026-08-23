using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

/// <summary>
/// The quiz itself — which is only a list of question numbers. No text goes over the wire: the
/// client builds every question and every option from its own localization, looking up
/// <c>quiz.&lt;code&gt;.question.&lt;id&gt;</c> and <c>quiz.&lt;code&gt;.answer.&lt;id&gt;.&lt;n&gt;</c>
/// until an option is missing. So a question number the hotel's texts do not define shows up as an
/// empty question with no answers, not as an error.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record QuizDataMessageComposer : IComposer
{
    [Id(0)]
    public required string QuizCode { get; init; }

    /// <summary>Order matters: the answers come back positionally against this list.</summary>
    [Id(1)]
    public required ImmutableArray<int> QuestionIds { get; init; }
}
