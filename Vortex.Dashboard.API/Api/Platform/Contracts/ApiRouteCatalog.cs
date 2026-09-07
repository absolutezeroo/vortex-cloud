using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// Every route this dashboard serves, with what it takes and what it costs to call.
/// </summary>
/// <remarks>
/// Read off the running endpoint table rather than a list somebody maintains, so a route that
/// exists is in here and a route that was deleted is not.
/// </remarks>
public sealed record ApiRouteCatalog(
    string Version,
    DateTime GeneratedAt,
    IReadOnlyList<ApiRouteDescriptor> Routes,
    IReadOnlyList<ApiDomainGroup> Groups,
    IReadOnlyList<ApiMethodUsage> MethodUsage
);
