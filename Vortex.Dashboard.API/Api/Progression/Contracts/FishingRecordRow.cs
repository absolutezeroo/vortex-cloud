using System;

namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One entry of the records board.
/// </summary>
/// <remarks>Both names are null when the row they came from has been deleted.</remarks>
public sealed record FishingRecordRow(
    int Id,
    int PlayerId,
    string? PlayerName,
    int SpeciesId,
    string? SpeciesNameKey,
    int BestWeight,
    int CaughtCount,
    DateTime BestAt
);
