using System.Collections.Generic;
using Vortex.Specs.Model;

namespace Vortex.Specs.Analysis.Client;

/// <summary>A field as a client source describes it.</summary>
public sealed record ClientField
{
    public required string? Name { get; init; }

    public required WireType Type { get; init; }

    public string? SemanticType { get; init; }

    public string? Note { get; init; }

    public IReadOnlyList<ClientField> Children { get; init; } = [];
}

/// <summary>One packet as one client source describes it.</summary>
public sealed record ClientPacket
{
    public required string Canonical { get; init; }

    /// <summary>Named from the server's point of view, already flipped from the client's.</summary>
    public required PacketDirection Direction { get; init; }

    public required string DeclaredType { get; init; }

    public int? HeaderId { get; init; }

    public IReadOnlyList<ClientField> Fields { get; init; } = [];

    /// <summary>True when the reader could not follow the whole layout.</summary>
    public bool IsPartial { get; init; }

    public required EvidenceRef Evidence { get; init; }
}

public sealed record ClientScan
{
    public required string Origin { get; init; }

    public required EvidenceAuthority Authority { get; init; }

    /// <summary>The client build this tree is, when it declares one.</summary>
    public string? Revision { get; init; }

    /// <summary>
    /// True when this client is the same build the emulator targets. Only then may its header ids be
    /// compared with the emulator's; across builds the ids are simply different numbers for the same
    /// message and comparing them manufactures conflicts.
    /// </summary>
    public bool TargetsSameRevision { get; init; }

    public required IReadOnlyList<ClientPacket> Packets { get; init; }

    public IReadOnlyDictionary<string, int> IncomingHeaders { get; init; } =
        new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> OutgoingHeaders { get; init; } =
        new Dictionary<string, int>();

    /// <summary>Things the reader saw but could not resolve, so gaps are visible not silent.</summary>
    public IReadOnlyList<string> Unresolved { get; init; } = [];
}

/// <summary>
/// A source of client-side protocol knowledge.
/// </summary>
/// <remarks>
/// The interface is the seam the task calls for: today the ActionScript reader is a lightweight
/// syntactic scanner over a very regular decompiler output, and replacing it with a real AS3 parser
/// is a change behind this interface and nowhere else.
/// </remarks>
public interface IClientAnalyzer
{
    string Origin { get; }

    ClientScan Scan();
}
