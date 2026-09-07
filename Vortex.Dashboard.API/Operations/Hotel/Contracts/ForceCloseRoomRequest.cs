using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>Force-deactivate an active room. Does not itself evict occupants — pair with
/// <see cref="KickFromRoomRequest"/> per player if a hard clear is needed.</summary>
public sealed record ForceCloseRoomRequest(int RoomId, string Reason) : IReasonedRequest;
