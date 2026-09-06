using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>
/// A badge arrived, however it arrived.
/// </summary>
/// <remarks>
/// Distinct from <c>wear_badge</c>, which is about display. Earning is the achievement; wearing is
/// the choice — and a task meaning "collect ten badges" was previously only expressible as "wear
/// ten", which a player can satisfy by fiddling with their profile.
/// </remarks>
public sealed class BadgeGrantedTranslator : ISignalTranslator<BadgeGrantedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.EarnBadge, [Facts.Badge], TargetKind: FactKind.BadgeCode)];

    public ImmutableArray<ProgressSignal> Translate(BadgeGrantedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.EarnBadge,
                1,
                e.BadgeCode,
                SignalFacts.Build().Text(Facts.Badge, e.BadgeCode)
            ),
        ];
}
