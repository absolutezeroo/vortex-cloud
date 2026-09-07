using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations.Catalogue.Contracts;

public sealed record UpdateFurnitureDefinitionRequest(
    int DefinitionId,
    int SpriteId,
    string Name,
    ProductType ProductType,
    FurnitureCategory FurniCategory,
    string Logic,
    int TotalStates,
    int Width,
    int Length,
    double StackHeight,
    bool CanStack,
    bool CanWalk,
    bool CanSit,
    bool CanLay,
    bool CanRecycle,
    bool CanTrade,
    bool CanGroup,
    bool CanSell,
    FurnitureUsageType UsagePolicy,
    string? ExtraData,
    StuffDataType StuffDataType,
    string Reason,
    string? VendingIds = null
) : IReasonedRequest;
