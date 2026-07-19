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

/// <summary>A player entered an LTD (limited edition) raffle.</summary>
public sealed record LtdRaffleEnteredEvent(int PlayerId, int SeriesId, int Cost) : IEvent;

/// <summary>A player won an LTD raffle and was granted the rare item.</summary>
public sealed record LtdRaffleWonEvent(
    int PlayerId,
    int SeriesId,
    int SerialNumber,
    int FurniDefinitionId
) : IEvent;
