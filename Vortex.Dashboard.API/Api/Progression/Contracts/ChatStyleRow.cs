namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One chat style.
/// </summary>
/// <param name="ClientStyleId">The id the client knows it by, which is not this table's key.</param>
public sealed record ChatStyleRow(int Id, int ClientStyleId, int Owners);
