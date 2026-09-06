using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record UpdateRoleRequest(int RoleId, string Key, string Name, string Reason)
    : IReasonedRequest;
