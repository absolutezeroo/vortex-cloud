using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Accepted a quest. Completion already had an action; starting one did not.</summary>
public sealed class QuestAcceptedTranslator : ISignalTranslator<QuestAcceptedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.AcceptQuest, [Facts.Code], TargetKind: FactKind.Text)];

    public ImmutableArray<ProgressSignal> Translate(QuestAcceptedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.AcceptQuest,
                1,
                e.CampaignCode,
                SignalFacts.Build().Text(Facts.Code, e.CampaignCode)
            ),
        ];
}
