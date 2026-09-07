namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>How much of each journal the account produced inside the window.</summary>
public sealed record ProfileActivity(int AuditEvents, int LedgerEvents, int ItemEvents);
