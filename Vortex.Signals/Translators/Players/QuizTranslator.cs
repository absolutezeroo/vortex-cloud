using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Submitted a quiz.</summary>
public sealed class QuizTranslator : ISignalTranslator<QuizSubmittedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.SubmitQuiz, [Facts.Quiz], TargetKind: FactKind.Text)];

    public ImmutableArray<ProgressSignal> Translate(QuizSubmittedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.SubmitQuiz,
                1,
                e.QuizCode,
                SignalFacts.Build().Text(Facts.Quiz, e.QuizCode)
            ),
        ];
}
