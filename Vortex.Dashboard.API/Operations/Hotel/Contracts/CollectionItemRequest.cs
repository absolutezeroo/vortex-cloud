using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

public sealed record CollectionItemRequest(
    int ItemId,
    int CollectionId,
    string ProductCode,
    string ItemTypeId,
    int ProductTypeId,
    int Score,
    string Rarity,
    int SortOrder,
    string Reason
) : IReasonedRequest;
