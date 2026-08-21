using System.Collections.Generic;

namespace Vortex.Specs.Model;

/// <summary>
/// A name-to-id table for one revision, from one source.
/// </summary>
/// <remarks>
/// Header ids are per-revision and are the one thing that must never leak into behavioural specs:
/// <c>MoveObject</c> is 1482 in the client this emulator targets, 2828 in Nitro's table and 248 in
/// Arcturus's, and all three are correct for their own revision. Feature and packet specs therefore
/// speak in names only, and ids live exclusively in these registries.
/// </remarks>
public sealed record RevisionRegistry
{
    /// <summary>File-safe id, e.g. <c>WIN63-202607011411-782849652</c>.</summary>
    public required string Id { get; init; }

    /// <summary>The revision string the source itself declares, when it declares one.</summary>
    public string? RevisionString { get; init; }

    /// <summary>Where the table was read from: <c>vortex</c>, <c>nitro</c>, <c>arcturus</c>, <c>as3</c>.</summary>
    public required string Origin { get; init; }

    public required EvidenceAuthority Authority { get; init; }

    /// <summary>
    /// True when the table is known to describe the same client build as
    /// <see cref="SpecConstants.TargetRevision"/>. Only same-revision tables may be compared for
    /// header conflicts; comparing across revisions manufactures conflicts out of thin air.
    /// </summary>
    public bool TargetsSameRevision { get; init; }

    public required IReadOnlyDictionary<string, int> Incoming { get; init; }

    public required IReadOnlyDictionary<string, int> Outgoing { get; init; }

    public required EvidenceRef Evidence { get; init; }
}

public static class SpecConstants
{
    /// <summary>
    /// The client build this emulator targets. Read from the embedded revision at scan time rather
    /// than assumed; this constant is only the fallback used when that read fails.
    /// </summary>
    public const string TargetRevision = "WIN63-202607011411-782849652";

    /// <summary>Bumped when the on-disk spec shape changes in a way readers must notice.</summary>
    public const int SpecFormatVersion = 1;
}
