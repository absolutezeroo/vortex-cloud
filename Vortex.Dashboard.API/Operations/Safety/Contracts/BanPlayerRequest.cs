using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Suspend the player's linked account. Kept separate from <see cref="UnbanPlayerRequest"/> —
/// folding "lift" into this same request via a nullable field would be a footgun since the domain
/// method's <c>bannedUntil: null</c> means lift, not "no change". <paramref name="Permanent"/> true
/// ignores <paramref name="DurationSeconds"/>.
/// </summary>
public sealed record BanPlayerRequest(
    int PlayerId,
    bool Permanent,
    int? DurationSeconds,
    string Reason
) : IReasonedRequest;
