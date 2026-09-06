using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Turned on an avatar effect, and for how long.</summary>
public sealed class AvatarEffectTranslator : ISignalTranslator<AvatarEffectActivatedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(
            SignalActions.ActivateEffect,
            [Facts.Effect, Facts.DurationSeconds],
            TargetKind: FactKind.OpaqueId
        ),
    ];

    public ImmutableArray<ProgressSignal> Translate(AvatarEffectActivatedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.ActivateEffect,
                1,
                SignalValue.Id(e.EffectId),
                SignalFacts
                    .Build()
                    .Id(Facts.Effect, e.EffectId)
                    .Number(Facts.DurationSeconds, e.DurationSeconds)
            ),
        ];
}
