using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations.Catalogue.Contracts;

/// <summary>Grant silver or emeralds to a player. <paramref name="Currency"/> is the
/// <c>CurrencyType</c> name — credits and activity points have their own endpoints, which carry the
/// extra handling those two need.</summary>
public sealed record GiveCollectiblesCurrencyRequest(
    int PlayerId,
    string Currency,
    int Amount,
    string Reason
) : IReasonedRequest;
