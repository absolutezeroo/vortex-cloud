using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Safety.Contracts;

public sealed record DeleteRoleRequest(int RoleId, string Reason) : IReasonedRequest;
