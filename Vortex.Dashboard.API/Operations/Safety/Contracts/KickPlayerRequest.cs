using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations.Safety.Contracts;

/// <summary>Force-disconnect a player by dropping their active session.</summary>
public sealed record KickPlayerRequest(int PlayerId, string Reason) : IReasonedRequest;
