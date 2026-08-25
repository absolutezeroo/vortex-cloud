namespace Vortex.Primitives.Events;

/// <summary>
/// A player's wallet balance changed. <paramref name="Delta"/> is signed (negative = debit). This is
/// the single source of truth for currency movement; consumers derive intent from the sign.
/// </summary>
public sealed record CurrencyChangedEvent(
    int PlayerId,
    string Currency,
    int? ActivityPointType,
    long Delta,
    long BalanceAfter
) : IEvent;

/// <summary>A player completed a catalog purchase.</summary>
public sealed record CatalogPurchasedEvent(
    int PlayerId,
    string CatalogType,
    int OfferId,
    int Quantity,
    int CreditCost
) : IEvent;

/// <summary>
/// A player bought a catalog offer for someone else. Raised alongside <see cref="CatalogPurchasedEvent"/>
/// -- that one records the spend, this one records who received the goods, which is the part a gift
/// adds and the part an operator needs when tracing where furniture came from.
/// </summary>
public sealed record CatalogGiftPurchasedEvent(
    int BuyerPlayerId,
    int ReceiverPlayerId,
    int OfferId,
    int CreditCost
) : IEvent;

/// <summary>A player bought a targeted (personalised/promotional) offer at its special price.</summary>
public sealed record TargetedOfferPurchasedEvent(
    int PlayerId,
    int OfferId,
    string Identifier,
    int Quantity,
    int CreditCost,
    int ActivityPointCost
) : IEvent;

/// <summary>A player entered an LTD (limited edition) raffle.</summary>
public sealed record LtdRaffleEnteredEvent(int PlayerId, int SeriesId, int Cost) : IEvent;

/// <summary>A player won an LTD raffle and was granted the rare item.</summary>
public sealed record LtdRaffleWonEvent(
    int PlayerId,
    int SeriesId,
    int SerialNumber,
    int FurniDefinitionId
) : IEvent;

/// <summary>
/// A voucher code was redeemed. The wallet credit lands in the ledger either way; this says which
/// code bought it, which is the only way to notice one code being spread around.
/// </summary>
public sealed record VoucherRedeemedEvent(
    int PlayerId,
    string Code,
    int Amount,
    int? ActivityPointType
) : IEvent;
