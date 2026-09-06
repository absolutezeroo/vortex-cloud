using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Habbicon collections completed. The target is the collection code, so a task can name one.</summary>
public sealed class HabbiconCollectionTranslator
    : ISignalTranslator<HabbiconCollectionCompletedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.CompleteHabbiconCollection, [Facts.Collection], TargetKind: FactKind.Text)];

    public ImmutableArray<ProgressSignal> Translate(HabbiconCollectionCompletedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.CompleteHabbiconCollection,
                1,
                Target: e.CollectionCode,
                SignalFacts.Build().Text(Facts.Collection, e.CollectionCode)
            ),
        ];
}
