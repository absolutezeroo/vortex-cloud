using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Grains;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Catalog;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.PacketHandlers.Catalog;

/// <summary>
/// Redeems a voucher code for currency.
/// </summary>
/// <remarks>
/// Everything before the grain call is there because the grain is NAMED after the code
/// (<c>GetVoucherGrain(code)</c>): an activation and a database read per distinct string tried.
/// Unchecked, a client decided how much memory and how many queries one packet cost, and could sit
/// there guessing codes for currency at no cost to itself (SEC-11). Shape first, then allowance,
/// then the grain.
/// </remarks>
public class RedeemVoucherMessageHandler(
    IGrainFactory grainFactory,
    IVoucherAttemptLimiter attemptLimiter
) : IMessageHandler<RedeemVoucherMessage>
{
    public async ValueTask HandleAsync(
        RedeemVoucherMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        // Before the grain is named, not after: a string that cannot be a code must not become a
        // grain key. VoucherCode.MaxLength mirrors the column, so nothing refused here could ever
        // have matched a row.
        if (!VoucherCode.IsWellFormed(message.Code))
        {
            await RefuseAsync(ctx, ct).ConfigureAwait(false);

            return;
        }

        if (!attemptLimiter.MayAttempt(ctx.PlayerId))
        {
            await RefuseAsync(ctx, ct).ConfigureAwait(false);

            return;
        }

        VoucherRedeemResult result = await grainFactory
            .GetVoucherGrain(message.Code!)
            .RedeemAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        // Only wrong codes cost an attempt. Redeeming real ones back to back is never slowed.
        if (!result.Success)
        {
            attemptLimiter.RecordFailure(ctx.PlayerId);
        }
    }

    /// <summary>
    /// Answers in the same words the grain uses for an unknown code, deliberately. Letting a
    /// guesser tell "no such code" apart from "you are being throttled" hands them the one bit they
    /// need to pace themselves.
    /// </summary>
    private static async Task RefuseAsync(MessageContext ctx, CancellationToken ct) =>
        await ctx.SendComposerAsync(
                new VoucherRedeemErrorMessageComposer { ErrorCode = "not_found" },
                ct
            )
            .ConfigureAwait(false);
}
