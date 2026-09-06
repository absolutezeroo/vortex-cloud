using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Badges equipped. One signal per badge worn, so a target can name a specific one.</summary>
public sealed class BadgeTranslator : ISignalTranslator<BadgesEquippedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.WearBadge, [Facts.Badge], TargetKind: FactKind.BadgeCode)];

    public ImmutableArray<ProgressSignal> Translate(BadgesEquippedEvent e)
    {
        if (e.BadgeCodes.IsDefaultOrEmpty)
        {
            return [];
        }

        ImmutableArray<ProgressSignal>.Builder builder =
            ImmutableArray.CreateBuilder<ProgressSignal>(e.BadgeCodes.Length);

        foreach (string badgeCode in e.BadgeCodes)
        {
            builder.Add(
                new ProgressSignal(
                    e.PlayerId.Value,
                    SignalActions.WearBadge,
                    1,
                    badgeCode,
                    SignalFacts.Build().Text(Facts.Badge, badgeCode)
                )
            );
        }

        return builder.MoveToImmutable();
    }
}
