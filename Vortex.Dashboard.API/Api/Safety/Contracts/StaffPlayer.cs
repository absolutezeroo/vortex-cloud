namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>One habbo on a staff account, with its head.</summary>
public sealed record StaffPlayer(int Id, string Name, string? AvatarUrl);
