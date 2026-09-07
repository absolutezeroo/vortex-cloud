using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>One currency movement on this account.</summary>
public sealed record PlayerLedgerRow(
    DateTime OccurredAt,
    string Currency,
    long Delta,
    long BalanceAfter,
    int? ActivityPointType,
    string? CorrelationId
);
