using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

public sealed record AssignRoleRequest(int AccountId, int RoleId, string Reason) : IReasonedRequest;
