using System;
using System.Collections.Immutable;

namespace Vortex.Primitives.Moderation;

public readonly record struct CfhTicketEvidenceSnapshot(
    int? RoomId,
    DateTime ReportedAtUtc,
    ImmutableArray<CfhEvidenceLine> Evidence
);
