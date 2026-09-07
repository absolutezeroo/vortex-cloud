using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record StoreOfferRequest(
    int OfferId,
    string ProductCode,
    int EmeraldPrice,
    bool IsFeatured,
    bool IsLimited,
    int MintLimit,
    string ItemTypeId,
    int ProductTypeId,
    int Score,
    string Rarity,
    bool Enabled,
    int SortOrder,
    string Reason
) : IReasonedRequest;
