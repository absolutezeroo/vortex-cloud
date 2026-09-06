using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations;

public sealed record CreateCatalogProductRequest(
    int OfferId,
    ProductType ProductType,
    int? FurnitureDefinitionId,
    string? ExtraParam,
    int Quantity,
    int UniqueSize,
    int UniqueRemaining,
    bool BuildersClubEligible,
    string Reason
) : IReasonedRequest;
