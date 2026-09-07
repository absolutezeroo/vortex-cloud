using System;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One run already written to disk.
/// </summary>
/// <param name="Grade">The verdict the report stored. The history list draws a badge from it,
/// and the read used to leave it out -- so every row's badge read Unknown.</param>
public sealed record BenchmarkRunView(
    string FileName,
    string Path,
    long SizeBytes,
    DateTime WrittenAtUtc,
    int Players,
    int Furniture,
    int DurationSeconds,
    string Label,
    string Phase,
    int RoomId,
    bool BorrowedRoom,
    int PeakClients,
    double WorstRttMs,
    long Failures,
    string Grade,
    string Headline
);
