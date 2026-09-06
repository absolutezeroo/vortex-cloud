using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Claimed vault income, from one category of holdings.</summary>
public sealed class VaultIncomeTranslator : ISignalTranslator<VaultIncomeClaimedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.ClaimVaultIncome, [Facts.Code, Facts.Quantity], TargetKind: FactKind.Text)];

    public ImmutableArray<ProgressSignal> Translate(VaultIncomeClaimedEvent e) =>
        [
            new(
                e.PlayerId.Value,
                SignalActions.ClaimVaultIncome,
                e.Rewards,
                e.Category,
                SignalFacts.Build().Text(Facts.Code, e.Category).Number(Facts.Quantity, e.Rewards)
            ),
        ];
}
