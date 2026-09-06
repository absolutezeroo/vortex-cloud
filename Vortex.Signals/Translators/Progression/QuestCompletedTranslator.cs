using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Quests completed.
/// </summary>
/// <remarks>
/// One progression system feeding another: the quest system raises this, and reward tracks consume
/// it. A consumer must never map an action back onto the system that produces it, or a completion
/// would feed itself.
/// </remarks>
public sealed class QuestCompletedTranslator : ISignalTranslator<QuestCompletedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.CompleteQuest, [])];

    public ImmutableArray<ProgressSignal> Translate(QuestCompletedEvent e) =>
        [new(e.PlayerId.Value, SignalActions.CompleteQuest, 1, Target: null, [])];
}
