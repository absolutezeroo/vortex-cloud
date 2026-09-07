namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One route.
/// </summary>
/// <param name="Capabilities">The authorization policies the route carries. Empty means anonymous,
/// which is what <paramref name="RequiresAuth"/> says in one word.</param>
public sealed record ApiRouteDescriptor(
    string Domain,
    string Path,
    string[] Methods,
    string[] Tags,
    string[] Capabilities,
    bool RequiresAuth,
    string? DisplayName
);
