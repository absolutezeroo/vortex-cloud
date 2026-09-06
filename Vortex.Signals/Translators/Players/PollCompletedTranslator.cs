using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Answered a poll. A rejection is not an answer, and has its own event we do not read.</summary>
public sealed class PollCompletedTranslator : ISignalTranslator<PollCompletedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.CompletePoll, [Facts.Poll], TargetKind: FactKind.Text)];

    public ImmutableArray<ProgressSignal> Translate(PollCompletedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.CompletePoll,
                1,
                e.PollCode,
                SignalFacts.Build().Text(Facts.Poll, e.PollCode)
            ),
        ];
}
