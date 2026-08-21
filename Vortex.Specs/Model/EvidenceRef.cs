using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Vortex.Specs.Model;

/// <summary>
/// One traceable observation. Every non-trivial claim in a spec points at one of these by id, so a
/// reader can always get from "the spec says X" back to the file and line that made it say X.
/// </summary>
public sealed record EvidenceRef
{
    public required EvidenceKind Kind { get; init; }

    public required EvidenceAuthority Authority { get; init; }

    /// <summary>
    /// Which tree the observation came from: "vortex", "client:WIN63-...", "reference:arcturus",
    /// "capture:the-capture-id". Free-form on purpose — a new source must not need a model change.
    /// </summary>
    public required string Origin { get; init; }

    /// <summary>Repository-relative path, or a capture id when there is no file.</summary>
    public required string Source { get; init; }

    /// <summary>The symbol inside <see cref="Source"/>, when there is one.</summary>
    public string? Symbol { get; init; }

    public int? Line { get; init; }

    public string? Note { get; init; }

    /// <summary>
    /// Stable across runs: derived from the content of the reference, never from a counter. Two
    /// scans of an unchanged tree produce byte-identical spec files.
    /// </summary>
    public string Id => BuildId(Kind, Origin, Source, Symbol);

    public static string BuildId(EvidenceKind kind, string origin, string source, string? symbol)
    {
        string material = string.Join(
            ' ',
            kind.ToString(),
            origin,
            source.Replace('\\', '/'),
            symbol ?? string.Empty
        );

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        StringBuilder builder = new("ev_", 15);

        for (int i = 0; i < 6; i++)
        {
            builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Deterministic ordering for on-disk output: strongest authority first, then by id so equal
    /// authorities never shuffle between runs.
    /// </summary>
    public sealed class ByAuthorityThenId : IComparer<EvidenceRef>
    {
        public static readonly ByAuthorityThenId Instance = new();

        public int Compare(EvidenceRef? x, EvidenceRef? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return 1;
            }

            if (y is null)
            {
                return -1;
            }

            int byAuthority = ((int)x.Authority).CompareTo((int)y.Authority);

            return byAuthority != 0 ? byAuthority : string.CompareOrdinal(x.Id, y.Id);
        }
    }
}
