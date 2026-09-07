namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>How many routes one domain owns, and which verbs it answers.</summary>
public sealed record ApiDomainGroup(string Domain, int RouteCount, string[] Methods);
