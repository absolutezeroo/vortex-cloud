using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One currency movement carrying the correlation id.</summary>
public sealed record CorrelationLedgerRow(
    DateTime OccurredAt,
    long PlayerId,
    string Currency,
    long Delta,
    long BalanceAfter,
    int? ActivityPointType
);
