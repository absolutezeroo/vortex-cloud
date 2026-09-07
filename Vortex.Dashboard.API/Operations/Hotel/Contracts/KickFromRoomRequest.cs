using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Dashboard.API.Operations.Safety.Contracts;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>Remove one player from a room they are currently in. One-time removal, not a ban —
/// use <see cref="BanPlayerRequest"/> for account-wide sanctions.</summary>
public sealed record KickFromRoomRequest(int RoomId, int PlayerId, string Reason)
    : IReasonedRequest;
