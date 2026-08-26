using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Analysis.Reference;
using Vortex.Specs.Model;
using Vortex.Specs.Naming;

namespace Vortex.Specs.Reasoning;

/// <summary>
/// Groups packets into behavioural features.
/// </summary>
/// <remarks>
/// The grouping is taken from structure that already exists rather than invented: a feature is
/// "the domain operation a handler delegates to", which is a fact about the code, so two packets
/// land in the same feature exactly when they reach the same operation. Inventing a taxonomy here
/// would produce categories no implementation agrees with and that nothing can be checked against.
/// </remarks>
public sealed class FeatureBuilder
{
    public IReadOnlyList<FeatureSpec> Build(SpecWorld world, IReadOnlyList<PacketSpec> packets)
    {
        Dictionary<string, EmulatorIncoming> incomingByMessage = world
            .Emulator.Incoming.Where(i => i.MessageType is not null)
            .GroupBy(i => i.MessageType!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        Dictionary<string, List<EmulatorFlow>> grouped = new(StringComparer.Ordinal);
        Dictionary<string, string> domains = new(StringComparer.Ordinal);

        foreach (EmulatorFlow flow in world.Emulator.Flows)
        {
            string domain = DomainOf(flow, packets, incomingByMessage);
            string id = BuildId(domain, flow);

            if (!grouped.TryGetValue(id, out List<EmulatorFlow>? bucket))
            {
                bucket = [];
                grouped[id] = bucket;
                domains[id] = domain;
            }

            bucket.Add(flow);
        }

        List<FeatureSpec> features = [];

        foreach (
            KeyValuePair<string, List<EmulatorFlow>> entry in grouped.OrderBy(
                e => e.Key,
                StringComparer.Ordinal
            )
        )
        {
            features.Add(
                Assemble(entry.Key, domains[entry.Key], entry.Value, world, incomingByMessage)
            );
        }

        return features;
    }

    private static FeatureSpec Assemble(
        string id,
        string domain,
        IReadOnlyList<EmulatorFlow> flows,
        SpecWorld world,
        IReadOnlyDictionary<string, EmulatorIncoming> incomingByMessage
    )
    {
        // Every packet that produces this message, not the first one found. A client can send the
        // same message from two surfaces on two header ids — the wired variable write does — and
        // naming only one of them leaves the other looking like a packet whose handler goes nowhere.
        List<string> triggers =
        [
            .. flows
                .SelectMany(f =>
                    world
                        .Emulator.Incoming.Where(i =>
                            string.Equals(i.MessageType, f.MessageType, StringComparison.Ordinal)
                        )
                        .Select(i => i.Canonical)
                        .DefaultIfEmpty(PacketNaming.Canonical(f.MessageType))
                )
                .Distinct(StringComparer.Ordinal)
                .OrderBy(t => t, StringComparer.Ordinal),
        ];

        List<FeatureFlowStep> steps =
        [
            .. flows
                .SelectMany(f => f.Steps)
                .GroupBy(s => s.Symbol, StringComparer.Ordinal)
                .Select(g => g.First())
                .OrderBy(s => s.Order)
                .Select((s, i) => s with { Order = i }),
        ];

        List<FeatureOutgoing> outgoing =
        [
            .. flows
                .SelectMany(f => f.Outgoing)
                .GroupBy(o => $"{o.Packet}|{o.Recipient}", StringComparer.Ordinal)
                .Select(g => g.First())
                .OrderBy(o => o.Packet, StringComparer.Ordinal),
        ];

        List<FeatureMutation> mutations =
        [
            .. flows
                .SelectMany(f => f.Mutations)
                .GroupBy(m => $"{m.Target}={m.Expression}", StringComparer.Ordinal)
                .Select(g => g.First())
                .OrderBy(m => m.Target, StringComparer.Ordinal),
        ];

        List<string> references =
        [
            .. world
                .References.Where(r =>
                    r.Behaviours.Any(b => triggers.Contains(b.Canonical, StringComparer.Ordinal))
                )
                .Select(r => r.Origin)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(r => r, StringComparer.Ordinal),
        ];

        // A capture is the only thing that can say the emission order is real. Without one the order
        // recorded here is this emulator's, and calling that "strict" would be a claim about Habbo
        // that nothing supports.
        Captures.TriggerSummary? captured = world.TriggerSummaries.FirstOrDefault(s =>
            triggers.Contains(s.TriggerPacket, StringComparer.Ordinal)
        );

        string ordering = captured is { OrderingIsStable: true, ObservationCount: > 1 }
            ? "strict"
            : "unknown";

        List<EvidenceAuthority> agreeing = [EvidenceAuthority.VortexEmulator];
        agreeing.AddRange(
            world
                .References.Where(r =>
                    r.Behaviours.Any(b => triggers.Contains(b.Canonical, StringComparer.Ordinal))
                )
                .Select(r => r.Authority)
        );

        if (captured is not null)
        {
            agreeing.Add(captured.BestAuthority);
        }

        return new FeatureSpec
        {
            Id = id,
            Domain = domain,
            Title = TitleOf(id),
            TriggerPackets = triggers,
            Flow = steps,
            Checks =
            [
                .. flows
                    .SelectMany(f => f.Checks)
                    .GroupBy(c => c.Expression, StringComparer.Ordinal)
                    .Select(g => g.First()),
            ],
            Mutations = mutations,
            Outgoing = outgoing,
            OutgoingOrdering = ordering,
            ReachesPersistence = flows.Any(f => f.ReachesPersistence),
            // Four ways a handler can be shown to do something, because the walker records them in
            // four places. A handler whose whole body is one grain call comes back with a single
            // step and one mutation, and counting only the steps calls it a stub — which is how a
            // fully implemented feature ends up on a list of things nobody built.
            ObservedInVortex =
                steps.Count > 1
                || outgoing.Count > 0
                || mutations.Count > 0
                || flows.Any(f => f.ReachesPersistence),
            ObservedInReferences = references,
            // Only a capture can speak for the official server. Everything else in this record is
            // evidence about implementations, however many of them agree.
            OfficialBehaviourConfidence = captured is null
                ? Confidence.Unknown
                : ConfidencePolicy.Combine(agreeing),
            // Every reference the body makes, not a sample of them. A spec that cites an evidence id
            // it does not define is a dead link, and the validator is right to call it one.
            Evidence =
            [
                .. flows
                    .SelectMany(f =>
                        f.Steps.Select(step => step.Evidence)
                            .Concat(f.Checks.Select(check => check.Evidence))
                            .Concat(f.Mutations.Select(mutation => mutation.Evidence))
                            .Concat(f.Outgoing.Select(outgoing => outgoing.Evidence))
                    )
                    .DistinctBy(e => e.Id)
                    .Order(EvidenceRef.ByAuthorityThenId.Instance),
            ],
        };
    }

    /// <summary>
    /// The feature id: the domain the handler lives in plus the operation it delegates to. A handler
    /// that delegates to nothing is named after its own message, which is what a stub looks like from
    /// the outside and is reported as such.
    /// </summary>
    private static string BuildId(string domain, EmulatorFlow flow)
    {
        string operation = flow.PrimaryOperation ?? PacketNaming.Canonical(flow.MessageType);

        if (operation.EndsWith("Async", StringComparison.Ordinal))
        {
            operation = operation[..^"Async".Length];
        }

        return $"{domain}.{PacketNaming.SnakeCase(operation)}";
    }

    private static string DomainOf(
        EmulatorFlow flow,
        IReadOnlyList<PacketSpec> packets,
        IReadOnlyDictionary<string, EmulatorIncoming> incomingByMessage
    )
    {
        string canonical = incomingByMessage.TryGetValue(
            flow.MessageType,
            out EmulatorIncoming? incoming
        )
            ? incoming.Canonical
            : PacketNaming.Canonical(flow.MessageType);

        PacketSpec? packet = packets.FirstOrDefault(p =>
            p.Direction == PacketDirection.Incoming
            && string.Equals(p.Name, canonical, StringComparison.Ordinal)
        );

        if (packet is not null && packet.Domain != "unsorted")
        {
            return packet.Domain;
        }

        // Fall back to the handler's own folder, which is the other place this repository already
        // records which domain a message belongs to.
        string source = flow.Steps.Count > 0 ? flow.Steps[0].Evidence.Source : string.Empty;
        string[] parts = source.Split('/');

        return parts.Length >= 2 && parts[0] == "Vortex.PacketHandlers"
            ? PacketNaming.NormalizeDomain(parts[1])
            : "unsorted";
    }

    private static string TitleOf(string id)
    {
        int dot = id.IndexOf('.', StringComparison.Ordinal);
        string operation = dot < 0 ? id : id[(dot + 1)..];

        return operation.Replace('_', ' ');
    }
}
