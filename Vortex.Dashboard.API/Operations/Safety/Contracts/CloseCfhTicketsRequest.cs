using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Close one or more CFH tickets. <paramref name="Reason"/> is 1=Useless, 2=Sanctioned,
/// 3=Resolved (see <c>Vortex.Primitives.Moderation.CfhTicketCloseReason</c>). Closing does not itself
/// apply a sanction — sanction the reported player as a separate ban action if warranted.
/// </summary>
public sealed record CloseCfhTicketsRequest(int[] IssueIds, int Reason, bool Sanctioned);
