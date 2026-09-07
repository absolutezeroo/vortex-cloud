namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>How many audited actions one category produced in the last hour.</summary>
public sealed record AuditCategoryCount(string Category, int Count);
