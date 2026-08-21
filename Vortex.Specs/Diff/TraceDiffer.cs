using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Specs.Captures;
using Vortex.Specs.Model;

namespace Vortex.Specs.Diff;

/// <summary>One server-to-client packet in a trace.</summary>
public sealed record TracedPacket
{
    public required string Name { get; init; }

    public Recipient Recipient { get; init; } = Recipient.Unknown;

    public IReadOnlyDictionary<string, string> Fields { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}

/// <summary>What one side did in response to one trigger.</summary>
public sealed record PacketTrace
{
    public required string Origin { get; init; }

    public required string Trigger { get; init; }

    public required IReadOnlyList<TracedPacket> Emitted { get; init; }

    /// <summary>Every trigger in a capture, as one trace each.</summary>
    public static IReadOnlyList<PacketTrace> FromCapture(
        CaptureDocument capture,
        IReadOnlyList<CaptureObservation> observations
    )
    {
        Dictionary<int, CaptureMessage> byIndex = capture.Messages.ToDictionary(m => m.Index);
        List<PacketTrace> traces = [];

        foreach (CaptureObservation observation in observations)
        {
            List<TracedPacket> emitted = [];
            int cursor = observation.TriggerIndex + 1;

            foreach (string name in observation.EmittedPackets)
            {
                CaptureMessage? message = null;

                while (byIndex.TryGetValue(cursor, out CaptureMessage? candidate))
                {
                    cursor++;

                    if (candidate.Direction == CaptureDirection.ServerToClient)
                    {
                        message = candidate;
                        break;
                    }
                }

                emitted.Add(
                    new TracedPacket
                    {
                        Name = name,
                        Recipient = message?.Recipient ?? Recipient.Unknown,
                        Fields =
                            message?.Fields
                            ?? new Dictionary<string, string>(StringComparer.Ordinal),
                    }
                );
            }

            traces.Add(
                new PacketTrace
                {
                    Origin = $"capture:{capture.Id}",
                    Trigger = observation.TriggerPacket,
                    Emitted = emitted,
                }
            );
        }

        return traces;
    }
}

public enum TraceDifferenceKind
{
    /// <summary>In the reference trace, absent from the emulator's.</summary>
    Missing,

    /// <summary>In the emulator's trace, absent from the reference.</summary>
    Extra,

    /// <summary>Present in both but at a different position in the sequence.</summary>
    Reordered,

    /// <summary>Same packet, different recipient.</summary>
    RecipientMismatch,

    /// <summary>Same packet, a field carries a different value.</summary>
    FieldMismatch,
}

public sealed record TraceDifference(
    TraceDifferenceKind Kind,
    string Packet,
    string Detail,
    int? ReferenceIndex = null,
    int? ActualIndex = null
);

/// <summary>
/// Compares two traces of the same trigger.
/// </summary>
/// <remarks>
/// Alignment is by longest common subsequence rather than position, so one missing packet at the
/// front reports as one missing packet instead of turning every packet after it into a mismatch.
/// The comparison covers order, recipient and field values, because a packet that arrives in the
/// wrong order or reaches the wrong audience is a real difference that a "same set of packets" check
/// would call a pass.
/// </remarks>
public sealed class TraceDiffer
{
    public IReadOnlyList<TraceDifference> Compare(PacketTrace reference, PacketTrace actual)
    {
        List<TraceDifference> differences = [];
        IReadOnlyList<TracedPacket> left = reference.Emitted;
        IReadOnlyList<TracedPacket> right = actual.Emitted;

        int[,] lengths = new int[left.Count + 1, right.Count + 1];

        for (int i = left.Count - 1; i >= 0; i--)
        {
            for (int j = right.Count - 1; j >= 0; j--)
            {
                lengths[i, j] = string.Equals(left[i].Name, right[j].Name, StringComparison.Ordinal)
                    ? lengths[i + 1, j + 1] + 1
                    : Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
            }
        }

        int x = 0;
        int y = 0;

        while (x < left.Count && y < right.Count)
        {
            if (string.Equals(left[x].Name, right[y].Name, StringComparison.Ordinal))
            {
                differences.AddRange(ComparePair(left[x], right[y], x, y));
                x++;
                y++;
                continue;
            }

            if (lengths[x + 1, y] >= lengths[x, y + 1])
            {
                differences.Add(Absent(left[x], x, right));
                x++;
            }
            else
            {
                differences.Add(Unexpected(right[y], y, left));
                y++;
            }
        }

        while (x < left.Count)
        {
            differences.Add(Absent(left[x], x, right));
            x++;
        }

        while (y < right.Count)
        {
            differences.Add(Unexpected(right[y], y, left));
            y++;
        }

        return differences;
    }

    private static TraceDifference Absent(
        TracedPacket packet,
        int index,
        IReadOnlyList<TracedPacket> other
    )
    {
        int elsewhere = IndexOf(other, packet.Name);

        return elsewhere >= 0
            ? new TraceDifference(
                TraceDifferenceKind.Reordered,
                packet.Name,
                $"expected at position {index}, found at position {elsewhere}",
                index,
                elsewhere
            )
            : new TraceDifference(
                TraceDifferenceKind.Missing,
                packet.Name,
                $"expected at position {index}, never sent",
                index
            );
    }

    private static TraceDifference Unexpected(
        TracedPacket packet,
        int index,
        IReadOnlyList<TracedPacket> other
    )
    {
        int elsewhere = IndexOf(other, packet.Name);

        return elsewhere >= 0
            ? new TraceDifference(
                TraceDifferenceKind.Reordered,
                packet.Name,
                $"sent at position {index}, expected at position {elsewhere}",
                elsewhere,
                index
            )
            : new TraceDifference(
                TraceDifferenceKind.Extra,
                packet.Name,
                $"sent at position {index}, not expected at all",
                null,
                index
            );
    }

    private static int IndexOf(IReadOnlyList<TracedPacket> packets, string name)
    {
        for (int i = 0; i < packets.Count; i++)
        {
            if (string.Equals(packets[i].Name, name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static IEnumerable<TraceDifference> ComparePair(
        TracedPacket expected,
        TracedPacket actual,
        int referenceIndex,
        int actualIndex
    )
    {
        // Unknown on either side means nobody looked, which is not the same as a disagreement.
        if (
            expected.Recipient != Recipient.Unknown
            && actual.Recipient != Recipient.Unknown
            && expected.Recipient != actual.Recipient
        )
        {
            yield return new TraceDifference(
                TraceDifferenceKind.RecipientMismatch,
                expected.Name,
                $"expected {expected.Recipient.Wire()}, sent to {actual.Recipient.Wire()}",
                referenceIndex,
                actualIndex
            );
        }

        foreach (
            KeyValuePair<string, string> field in expected.Fields.OrderBy(
                f => f.Key,
                StringComparer.Ordinal
            )
        )
        {
            if (!actual.Fields.TryGetValue(field.Key, out string? value))
            {
                continue;
            }

            if (!string.Equals(field.Value, value, StringComparison.Ordinal))
            {
                yield return new TraceDifference(
                    TraceDifferenceKind.FieldMismatch,
                    expected.Name,
                    $"{field.Key}: expected {field.Value}, got {value}",
                    referenceIndex,
                    actualIndex
                );
            }
        }
    }

    /// <summary>Renders a comparison the way a unified diff reads, for the console and reports.</summary>
    public static string Render(
        PacketTrace reference,
        PacketTrace actual,
        IReadOnlyList<TraceDifference> differences
    )
    {
        List<string> lines =
        [
            $"trigger: {reference.Trigger}",
            $"--- {reference.Origin}",
            $"+++ {actual.Origin}",
        ];

        HashSet<string> missing =
        [
            .. differences
                .Where(d => d.Kind is TraceDifferenceKind.Missing or TraceDifferenceKind.Reordered)
                .Select(d => d.Packet),
        ];
        HashSet<string> extra =
        [
            .. differences
                .Where(d => d.Kind is TraceDifferenceKind.Extra or TraceDifferenceKind.Reordered)
                .Select(d => d.Packet),
        ];

        foreach (TracedPacket packet in reference.Emitted)
        {
            lines.Add((missing.Contains(packet.Name) ? "-" : " ") + packet.Name);
        }

        foreach (TracedPacket packet in actual.Emitted)
        {
            if (extra.Contains(packet.Name))
            {
                lines.Add("+" + packet.Name);
            }
        }

        foreach (
            TraceDifference difference in differences.Where(d =>
                d.Kind is TraceDifferenceKind.RecipientMismatch or TraceDifferenceKind.FieldMismatch
            )
        )
        {
            lines.Add($"! {difference.Packet}: {difference.Detail}");
        }

        return string.Join('\n', lines);
    }
}
