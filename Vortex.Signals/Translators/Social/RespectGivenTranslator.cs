using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Respect given. The target is who received it, so a distinct task can require different people.</summary>
public sealed class RespectGivenTranslator : ISignalTranslator<RespectGivenEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.GiveRespect, [Facts.Player], TargetKind: FactKind.PlayerId)];

    public ImmutableArray<ProgressSignal> Translate(RespectGivenEvent e) =>
        [
            new(
                e.ActorPlayerId,
                SignalActions.GiveRespect,
                1,
                Target: SignalValue.Id(e.TargetPlayerId),
                SignalFacts.Build().Id(Facts.Player, e.TargetPlayerId)
            ),
        ];
}
