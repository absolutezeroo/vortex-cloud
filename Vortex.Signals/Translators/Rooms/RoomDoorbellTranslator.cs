using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// Somebody rang, and the owner decided.
/// </summary>
/// <remarks>
/// Only an admission counts. Turning people away is not an act to reward, and a task that advanced
/// either way would be farmed by refusing.
/// </remarks>
public sealed class RoomDoorbellTranslator : ISignalTranslator<RoomDoorbellAnsweredEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.AnswerDoorbell, [Facts.Room, Facts.Player], TargetKind: FactKind.PlayerId)];

    public ImmutableArray<ProgressSignal> Translate(RoomDoorbellAnsweredEvent e) =>
        e.Admitted
            ?
            [
                new(
                    e.ActorId.Value,
                    SignalActions.AnswerDoorbell,
                    1,
                    SignalValue.Id(e.TargetPlayerId.Value),
                    SignalFacts
                        .Build()
                        .Id(Facts.Room, e.RoomId)
                        .Id(Facts.Player, e.TargetPlayerId.Value)
                ),
            ]
            : [];
}
