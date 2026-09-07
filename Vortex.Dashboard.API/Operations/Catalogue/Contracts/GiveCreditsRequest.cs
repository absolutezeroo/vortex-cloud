using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations.Catalogue.Contracts;

/// <summary>Grant credits to a player's wallet. <paramref name="Reason"/> is mandatory and audited.</summary>
public sealed record GiveCreditsRequest(int PlayerId, int Amount, string Reason) : IReasonedRequest;
