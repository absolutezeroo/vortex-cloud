using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// The load test: what it is doing, what it measured, and the runs already on disk.
/// </summary>
/// <param name="Running">One of the four phases that are not idle or finished, spelled out so the
/// page does not re-derive it from the phase name.</param>
/// <param name="Residue">Non-null means rows were left in the hotel. Surfaced rather than logged:
/// it is the one outcome of a run that outlives the run.</param>
/// <param name="ReportPath">The artefact. A number on a page cannot be attached to anything.</param>
/// <param name="Runs">Read from the files, so the list survives a restart -- which is the whole
/// reason a run writes one.</param>
public sealed record BenchmarkState(
    string Phase,
    bool Running,
    BenchmarkPlanView? Plan,
    DateTime? StartedAt,
    DateTime? EndedAt,
    int ConnectedClients,
    int PlacedFurniture,
    int RoomId,
    bool Enabled,
    bool BorrowedRoom,
    string? Error,
    string? Residue,
    string? ReportPath,
    IReadOnlyList<BenchmarkSampleView> Samples,
    BenchmarkVerdictView Verdict,
    IReadOnlyList<BenchmarkRunView> Runs,
    BenchmarkSummaryView? Summary
);
