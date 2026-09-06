using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the staff/role operations, each carrying a mandatory audited <c>Reason</c>.
/// <c>Capabilities</c> on the role update is the complete set — anything absent is revoked.
/// </summary>
public sealed record CreateRoleRequest(string Key, string Name, string Reason) : IReasonedRequest;
