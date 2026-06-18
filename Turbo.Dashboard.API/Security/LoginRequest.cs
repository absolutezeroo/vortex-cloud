namespace Turbo.Dashboard.API.Security;

/// <summary>Credentials posted to <c>POST /api/login</c> to start a dashboard session.</summary>
public sealed record LoginRequest(string? Email, string? Password);
