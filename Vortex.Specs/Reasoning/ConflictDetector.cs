using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Vortex.Specs.Analysis.Client;
using Vortex.Specs.Analysis.Reference;
using Vortex.Specs.Model;

namespace Vortex.Specs.Reasoning;

/// <summary>
/// Finds and records the places where sources disagree.
/// </summary>
/// <remarks>
/// Nothing here resolves anything. A conflict document exists so the disagreement survives contact
/// with the next person to touch the feature: silently picking the strongest source would produce a
/// spec that looks settled and is not, and the whole point of this system is that unsettled things
/// stay visibly unsettled.
/// </remarks>
public sealed class ConflictDetector
{
    public IReadOnlyList<ConflictSpec> Detect(SpecWorld world, IReadOnlyList<PacketSpec> packets)
    {
        List<ConflictSpec> conflicts = [];

        conflicts.AddRange(FieldCountConflicts(packets));
        conflicts.AddRange(FieldTypeConflicts(packets));
        conflicts.AddRange(HeaderIdConflicts(world));
        conflicts.AddRange(BehaviourConflicts(world));

        return
        [
            .. conflicts
                .OrderBy(c => c.Kind)
                .ThenBy(c => c.Subject, StringComparer.Ordinal)
                .ThenBy(c => c.Id, StringComparer.Ordinal),
        ];
    }

    private static IEnumerable<ConflictSpec> FieldCountConflicts(IReadOnlyList<PacketSpec> packets)
    {
        foreach (PacketSpec packet in packets)
        {
            // Partial layouts are excluded on purpose: a reader that stopped early has not claimed
            // the packet is short, and treating it as a claim would fill the output with noise that
            // buries the real disagreements.
            List<PacketLayoutObservation> complete =
            [
                .. packet.Observations.Where(o => !o.IsPartial && o.Fields.Count > 0),
            ];

            if (complete.Select(o => o.Fields.Count).Distinct().Count() < 2)
            {
                continue;
            }

            yield return new ConflictSpec
            {
                Id = BuildId(ConflictKind.FieldCount, packet.SpecId),
                Kind = ConflictKind.FieldCount,
                Subject = $"{packet.SpecId}: field count",
                PacketName = packet.Name,
                Positions =
                [
                    .. complete
                        .OrderBy(o => (int)o.Authority)
                        .ThenBy(o => o.Origin, StringComparer.Ordinal)
                        .Select(o => new ConflictPosition
                        {
                            Origin = o.Origin,
                            Authority = o.Authority,
                            Claim = string.Format(
                                CultureInfo.InvariantCulture,
                                "{0} fields: {1}",
                                o.Fields.Count,
                                string.Join(", ", o.Fields.Select(f => f.Type.Wire()))
                            ),
                            Evidence = o.Evidence,
                        }),
                ],
            };
        }
    }

    private static IEnumerable<ConflictSpec> FieldTypeConflicts(IReadOnlyList<PacketSpec> packets)
    {
        foreach (PacketSpec packet in packets)
        {
            List<PacketLayoutObservation> complete =
            [
                .. packet.Observations.Where(o => !o.IsPartial && o.Fields.Count > 0),
            ];

            if (complete.Count < 2)
            {
                continue;
            }

            int width = complete[0].Fields.Count;

            if (complete.Any(o => o.Fields.Count != width))
            {
                // Already reported as a count conflict; comparing index by index across different
                // widths would report the same disagreement a second time under another name.
                continue;
            }

            for (int index = 0; index < width; index++)
            {
                int position = index;

                // A source that could not work out the type is not disagreeing about it. Counting
                // "unknown" as a position turns every gap in a reader into a conflict and buries the
                // handful of places where two sources genuinely say different things.
                List<PacketLayoutObservation> claiming =
                [
                    .. complete.Where(o => o.Fields[position].Type != WireType.Unknown),
                ];

                if (claiming.Select(o => o.Fields[position].Type).Distinct().Count() < 2)
                {
                    continue;
                }

                yield return new ConflictSpec
                {
                    Id = BuildId(ConflictKind.FieldType, $"{packet.SpecId}#{index}"),
                    Kind = ConflictKind.FieldType,
                    Subject = $"{packet.SpecId}: type of field {index}",
                    PacketName = packet.Name,
                    Positions =
                    [
                        .. claiming
                            .OrderBy(o => (int)o.Authority)
                            .ThenBy(o => o.Origin, StringComparer.Ordinal)
                            .Select(o => new ConflictPosition
                            {
                                Origin = o.Origin,
                                Authority = o.Authority,
                                Claim =
                                    $"{o.Fields[position].Name}: {o.Fields[position].Type.Wire()}",
                                Evidence = o.Evidence,
                            }),
                    ],
                };
            }
        }
    }

    /// <summary>
    /// Compares header ids, and only between sources that target the same client build.
    /// </summary>
    /// <remarks>
    /// This is the one comparison that would be catastrophic to get wrong in the permissive
    /// direction: Nitro and Arcturus target other builds, so comparing their tables with this one
    /// would flag several hundred non-conflicts and make the conflict list worthless. Where the
    /// comparison is legitimate — the official client for this exact build against this emulator's
    /// own table — it catches a header nobody can ever reach, which is a bug class this repository
    /// has shipped before.
    /// </remarks>
    private static IEnumerable<ConflictSpec> HeaderIdConflicts(SpecWorld world)
    {
        RevisionRegistry vortex = world.Emulator.Registry;

        foreach (ClientScan client in world.Clients.Where(c => c.TargetsSameRevision))
        {
            foreach (
                (string name, int clientId, bool incoming) in client
                    .IncomingHeaders.Select(e => (e.Key, e.Value, true))
                    .Concat(client.OutgoingHeaders.Select(e => (e.Key, e.Value, false)))
                    .OrderBy(e => e.Item1, StringComparer.Ordinal)
            )
            {
                IReadOnlyDictionary<string, int> table = incoming
                    ? vortex.Incoming
                    : vortex.Outgoing;

                if (!table.TryGetValue(name, out int vortexId) || vortexId == clientId)
                {
                    continue;
                }

                string direction = incoming ? "incoming" : "outgoing";

                yield return new ConflictSpec
                {
                    Id = BuildId(ConflictKind.HeaderId, $"{direction}/{name}"),
                    Kind = ConflictKind.HeaderId,
                    Subject = $"{direction}/{name}: header id for {vortex.Id}",
                    PacketName = name,
                    Positions =
                    [
                        new ConflictPosition
                        {
                            Origin = client.Origin,
                            Authority = client.Authority,
                            Claim = clientId.ToString(CultureInfo.InvariantCulture),
                            Evidence = client.Packets.First().Evidence,
                        },
                        new ConflictPosition
                        {
                            Origin = "vortex",
                            Authority = EvidenceAuthority.VortexEmulator,
                            Claim = vortexId.ToString(CultureInfo.InvariantCulture),
                            Evidence = vortex.Evidence,
                        },
                    ],
                };
            }
        }
    }

    /// <summary>
    /// Compares what each implementation sends back for the same trigger. Where a capture exists it
    /// joins the discussion as another position — a decisive one, but still recorded as a position
    /// rather than used to quietly delete the others.
    /// </summary>
    private static IEnumerable<ConflictSpec> BehaviourConflicts(SpecWorld world)
    {
        Dictionary<string, List<ConflictPosition>> byPacket = new(StringComparer.Ordinal);

        foreach (Analysis.Emulator.EmulatorFlow flow in world.Emulator.Flows)
        {
            string canonical = Naming.PacketNaming.Canonical(flow.MessageType);
            Add(
                byPacket,
                canonical,
                new ConflictPosition
                {
                    Origin = "vortex",
                    Authority = EvidenceAuthority.VortexEmulator,
                    Claim = Describe(flow.Outgoing.Select(o => o.Packet)),
                    Evidence =
                        flow.Steps.Count > 0
                            ? flow.Steps[0].Evidence
                            : world.Emulator.Registry.Evidence,
                }
            );
        }

        foreach (ReferenceScan reference in world.References)
        {
            foreach (ReferenceBehaviour behaviour in reference.Behaviours)
            {
                Add(
                    byPacket,
                    behaviour.Canonical,
                    new ConflictPosition
                    {
                        Origin = reference.Origin,
                        Authority = reference.Authority,
                        Claim = Describe(behaviour.Outgoing.Select(o => o.Packet)),
                        Evidence = behaviour.Evidence,
                    }
                );
            }
        }

        foreach (Captures.TriggerSummary summary in world.TriggerSummaries)
        {
            Add(
                byPacket,
                summary.TriggerPacket,
                new ConflictPosition
                {
                    Origin = $"capture ({summary.ObservationCount} observations)",
                    Authority = summary.BestAuthority,
                    Claim = Describe(
                        summary.Sequences.Count > 0 ? summary.Sequences[0].Sequence : []
                    ),
                    Evidence = new EvidenceRef
                    {
                        Kind = EvidenceKind.Capture,
                        Authority = summary.BestAuthority,
                        Origin = "capture",
                        Source = "docs/habbo-specs/evidence/captures",
                        Symbol = summary.TriggerPacket,
                    },
                }
            );
        }

        foreach (
            KeyValuePair<string, List<ConflictPosition>> entry in byPacket.OrderBy(
                e => e.Key,
                StringComparer.Ordinal
            )
        )
        {
            if (
                entry.Value.Count < 2
                || entry.Value.Select(p => p.Claim).Distinct(StringComparer.Ordinal).Count() < 2
            )
            {
                continue;
            }

            // One side sending nothing at all is usually an unimplemented handler rather than a
            // disagreement about behaviour, and it is already reported as an unknown.
            if (entry.Value.Any(p => p.Claim == "nothing"))
            {
                continue;
            }

            yield return new ConflictSpec
            {
                Id = BuildId(ConflictKind.Behaviour, entry.Key),
                Kind = ConflictKind.Behaviour,
                Subject = $"{entry.Key}: packets emitted in response",
                PacketName = entry.Key,
                Positions =
                [
                    .. entry
                        .Value.OrderBy(p => (int)p.Authority)
                        .ThenBy(p => p.Origin, StringComparer.Ordinal),
                ],
            };
        }
    }

    private static void Add(
        Dictionary<string, List<ConflictPosition>> map,
        string key,
        ConflictPosition position
    )
    {
        if (!map.TryGetValue(key, out List<ConflictPosition>? bucket))
        {
            bucket = [];
            map[key] = bucket;
        }

        bucket.Add(position);
    }

    private static string Describe(IEnumerable<string> packets)
    {
        List<string> ordered = [.. packets];

        return ordered.Count == 0 ? "nothing" : string.Join(" then ", ordered);
    }

    /// <summary>
    /// Stable id from the conflict's own identity, so the same disagreement keeps the same filename
    /// across runs and a resolution written into it is not orphaned by the next scan.
    /// </summary>
    public static string BuildId(ConflictKind kind, string subject)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{kind}:{subject}"));
        StringBuilder builder = new(16);
        builder.Append("cf_");

        for (int i = 0; i < 5; i++)
        {
            builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
