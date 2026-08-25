using Vortex.Primitives.Players;

namespace Vortex.Primitives.Events;

/// <summary>
/// The marketplace, as an investigation reads it. The ledger already records the credits moving, but
/// money alone cannot answer the question actually being asked -- which item, listed by whom, at
/// what price, bought by whom. These carry the act; the ledger carries the amount.
/// </summary>
public sealed record MarketplaceOfferListedEvent(
    PlayerId SellerId,
    int OfferId,
    int DefinitionId,
    int Price
) : IEvent;

/// <summary>A seller pulled their own offer back before it sold; the item returns to them.</summary>
public sealed record MarketplaceOfferCancelledEvent(
    PlayerId SellerId,
    int OfferId,
    int DefinitionId,
    int Price
) : IEvent;

/// <summary>
/// An offer was bought. Both sides are named: this is the one economy event where the interesting
/// pattern is a pair of accounts, not a single one.
/// </summary>
public sealed record MarketplaceOfferBoughtEvent(
    PlayerId BuyerId,
    PlayerId SellerId,
    int OfferId,
    int DefinitionId,
    int Price
) : IEvent;

/// <summary>A seller collected the credits their sold offers had been holding.</summary>
public sealed record MarketplaceCreditsRedeemedEvent(PlayerId SellerId, int Credits, int OfferCount)
    : IEvent;
