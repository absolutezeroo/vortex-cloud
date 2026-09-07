using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One derby and how many took part.</summary>
public sealed record FishingDerbyRow(
    int Id,
    string NameKey,
    DateTime StartsAt,
    DateTime EndsAt,
    int Entries
);
