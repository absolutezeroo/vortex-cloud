using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations.Safety.Contracts;

/// <summary>Lock the player's ability to trade. See <see cref="BanPlayerRequest"/> for the
/// permanent/duration semantics and why lift is a separate request type.</summary>
public sealed record TradingLockRequest(
    int PlayerId,
    bool Permanent,
    int? DurationSeconds,
    string Reason
) : IReasonedRequest;
