using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations;

/// <summary>Grant activity points of a given type to a player.</summary>
public sealed record GiveActivityPointsRequest(int PlayerId, int Type, int Amount, string Reason)
    : IReasonedRequest;
