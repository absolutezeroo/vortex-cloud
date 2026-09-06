using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations;

/// <summary>Take a database dump now, on top of the schedule. Carries only the reason: where it is
/// written and how many are kept is bootstrap configuration, not an operator's choice.</summary>
public sealed record CreateDatabaseBackupRequest(string Reason) : IReasonedRequest;
