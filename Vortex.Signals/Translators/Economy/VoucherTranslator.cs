using System.Collections.Immutable;
using Vortex.Primitives.Events;
using Vortex.Primitives.Signals;

namespace Vortex.Signals.Translators;

/// <summary>Redeemed a voucher. The amount granted is the progress.</summary>
public sealed class VoucherTranslator : ISignalTranslator<VoucherRedeemedEvent>
{
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [new(SignalActions.RedeemVoucher, [Facts.Voucher, Facts.Price], TargetKind: FactKind.Text)];

    public ImmutableArray<ProgressSignal> Translate(VoucherRedeemedEvent e) =>
        [
            new(
                e.PlayerId,
                SignalActions.RedeemVoucher,
                e.Amount,
                e.Code,
                SignalFacts.Build().Text(Facts.Voucher, e.Code).Number(Facts.Price, e.Amount)
            ),
        ];
}
