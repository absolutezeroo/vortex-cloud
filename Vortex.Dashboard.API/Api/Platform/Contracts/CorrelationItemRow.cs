using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One item event carrying the correlation id.</summary>
public sealed record CorrelationItemRow(DateTime OccurredAt, long ItemId, string EventType);
