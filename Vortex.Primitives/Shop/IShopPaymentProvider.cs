using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Shop;

/// <summary>
/// One payment service provider. Everything a PSP differs in lives behind this interface: where to
/// send the browser, what shape its notification arrives in, and what it calls the payment.
/// </summary>
/// <remarks>
/// <para>
/// Adding Stripe, Paysafecard or a carrier-billing aggregator is one class implementing this and one
/// <c>AddSingleton&lt;IShopPaymentProvider, …&gt;</c>. Nothing in <c>ShopService</c>, in the
/// endpoints, or on the website knows a provider by name — the website posts a product code and gets
/// back a URL to go to, and which PSP that URL belongs to is not its business.
/// </para>
/// <para>
/// What a provider must NOT do is decide whether an order is paid. It reads its own wire format and
/// says what the message claims; the signature check, the amount check, the order lookup and the
/// grant are <c>ShopService</c>'s, identically for every provider. A PSP integration that could
/// approve its own payments would make the security of the shop a per-provider property.
/// </para>
/// <para>
/// The hotel never sees a card number. The provider hosts the payment page; the only thing that
/// crosses this boundary in the paying direction is an order id and an amount.
/// </para>
/// </remarks>
public interface IShopPaymentProvider
{
    /// <summary>
    /// The provider's key: the last segment of its webhook path and the value stored on an order.
    /// Lowercase ASCII, stable forever — orders keep it after the integration is retired.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Opens the payment at the provider and returns where the browser should go, or <c>null</c> when
    /// the provider hosts no page (the manual provider does not).
    /// </summary>
    /// <remarks>
    /// This is allowed to fail. A provider that is unreachable leaves the order Pending with no
    /// redirect, which is a truthful state — nothing was charged — and the player can start another.
    /// </remarks>
    Task<string?> StartAsync(ShopOrder order, CancellationToken ct);

    /// <summary>
    /// Reads one notification in this provider's own format. Returns false when the body is not a
    /// notification this provider recognises.
    /// </summary>
    /// <remarks>
    /// Called only AFTER the signature has been verified, so the body can be trusted to have come
    /// from the provider — but not to be well formed, and not to be about anything real. Saying
    /// "paid" here is a claim about the message, never a decision.
    /// </remarks>
    bool TryReadNotification(ShopWebhookRequest request, out ShopPaymentNotification notification);
}
