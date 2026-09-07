namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>Who sells the most, by credits taken. The name is null for a deleted player.</summary>
public sealed record MarketplaceSeller(int SellerId, string? SellerName, int Sales, long Volume);
