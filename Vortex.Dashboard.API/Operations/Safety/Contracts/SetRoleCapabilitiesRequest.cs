using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Safety.Contracts;

public sealed record SetRoleCapabilitiesRequest(
    int RoleId,
    IReadOnlyCollection<string> Capabilities,
    string Reason
) : IReasonedRequest;
