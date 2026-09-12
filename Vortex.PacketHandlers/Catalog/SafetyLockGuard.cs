using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Outgoing.Catalog;

namespace Vortex.PacketHandlers.Catalog;

/// <summary>
/// Refuses a purchase while the account's safety lock is on.
/// </summary>
/// <remarks>
/// <para>
/// The CLIENT already hides both spending screens when the lock is set
/// (<c>PurchaseCatalogWidget</c> and <c>MarketPlaceCatalogWidget</c> both ask
/// <c>isAccountSafetyLocked()</c>), so a purchase arriving here while locked means a client that
/// does not — which is exactly the case the lock exists for. A gate the client alone enforces
/// protects nobody from the person who stole the account.
/// </para>
/// <para>
/// Answered as <see cref="CatalogPurchaseErrorType.PurchaseFailed"/>, the generic refusal: there is
/// no "you are locked" error type on this wire, and inventing a number the client does not know
/// would leave it showing nothing at all.
/// </para>
/// <para>
/// Read from the player grain's state rather than the database: it is asked on every purchase, and
/// the website refreshes that state the moment the lock moves.
/// </para>
/// </remarks>
internal static class SafetyLockGuard
{
    /// <summary>
    /// True when the purchase must not proceed — the refusal has already been sent.
    /// </summary>
    public static async ValueTask<bool> RefusedAsync(
        IGrainFactory grainFactory,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        bool locked = await grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .IsSafetyLockedAsync(ct)
            .ConfigureAwait(false);

        if (!locked)
        {
            return false;
        }

        await ctx.SendComposerAsync(
                new PurchaseNotAllowedMessageComposer
                {
                    ErrorType = CatalogPurchaseErrorType.PurchaseFailed,
                },
                ct
            )
            .ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// The same question without the catalog's answer, for the marketplace — which refuses in its
    /// own vocabulary.
    /// </summary>
    public static async ValueTask<bool> IsLockedAsync(
        IGrainFactory grainFactory,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .IsSafetyLockedAsync(ct)
            .ConfigureAwait(false);
}
