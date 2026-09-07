using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>Every capability in one namespace -- the part before the first dot.</summary>
public sealed record CapabilityGroup(string Area, IReadOnlyList<string> Capabilities);
