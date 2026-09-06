using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record DeleteRoleRequest(int RoleId, string Reason) : IReasonedRequest;
