using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vortex.Specs.Analysis.Client;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Model;
using Vortex.Specs.Naming;
using Vortex.Specs.Pipeline;
using Vortex.Specs.Reasoning;

namespace Vortex.Specs.Completeness;

/// <summary>
/// Measures what the target client can ask for against what this repository does about it.
/// </summary>
/// <remarks>
/// The denominator is the whole design. It comes from the official client of the build this emulator
/// targets and from nothing else — not from <see cref="ResolvedSpecs.Features"/>, which
/// <c>FeatureBuilder</c> derives from Vortex flows and which therefore cannot contain a feature
/// Vortex has never heard of. Counting against a Vortex-derived collection would report a hotel with
/// no rooms as complete, which is the one answer this program exists to never give.
/// </remarks>
public sealed class CompletenessAnalyzer
{
    private sealed record Candidate(string Id, string Name, ClientPacket Packet);

    public CompletenessReport Analyze(
        SpecWorld world,
        ResolvedSpecs specs,
        CompletenessLedger ledger
    )
    {
        List<string> problems = [.. ledger.Problems];

        List<ClientScan> targets =
        [
            .. world
                .Clients.Where(c =>
                    c.Authority == EvidenceAuthority.ClientCode && c.TargetsSameRevision
                )
                .OrderBy(c => c.Origin, StringComparer.Ordinal),
        ];

        if (targets.Count == 0)
        {
            problems.Add(
                "no official client targeting "
                    + $"{world.Emulator.Revision} was found, so there is no obligation surface to "
                    + "score against; place the build's decompiled sources beside this checkout"
            );

            return new CompletenessReport
            {
                TargetRevision = null,
                TargetOrigin = null,
                Obligations = [],
                UnresolvedSurface = [],
                Problems = [.. problems],
            };
        }

        Dictionary<int, string> namesById = ReverseTable(world.Emulator.Registry.Incoming);
        Dictionary<string, PacketSpec> incoming = specs
            .Packets.Where(p => p.Direction == PacketDirection.Incoming)
            .ToDictionary(p => p.Name, StringComparer.Ordinal);
        ILookup<string, FeatureSpec> featuresByTrigger = specs
            .Features.SelectMany(f => f.TriggerPackets.Select(t => (Trigger: t, Feature: f)))
            .ToLookup(e => e.Trigger, e => e.Feature, StringComparer.Ordinal);
        Dictionary<string, EmulatorIncoming> emulatorIncoming = world
            .Emulator.Incoming.GroupBy(i => i.Canonical, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        Dictionary<string, Candidate> scored = new(StringComparer.Ordinal);
        Dictionary<string, Candidate> unresolved = new(StringComparer.Ordinal);

        foreach (ClientScan client in targets)
        {
            foreach (
                ClientPacket packet in client
                    .Packets.Where(p => p.Direction == PacketDirection.Incoming)
                    .OrderBy(p => p.DeclaredType, StringComparer.Ordinal)
            )
            {
                if (packet.HeaderId is not int header)
                {
                    // A class that reads the wire but that the reader could not tie to a header id.
                    // It is a real surface and it stays visible; it is not a scored obligation
                    // because nobody can say which message it is.
                    unresolved.TryAdd(
                        $"incoming/{packet.DeclaredType}",
                        new Candidate($"incoming/{packet.DeclaredType}", packet.Canonical, packet)
                    );
                    continue;
                }

                string name = ResolveName(packet, header, namesById);
                string id = $"incoming/{name}";

                // The same message can be declared by more than one class in a decompiled tree. One
                // message the client can send is one obligation.
                scored.TryAdd(id, new Candidate(id, name, packet));
            }
        }

        List<Obligation> obligations =
        [
            .. scored
                .Values.Select(c =>
                    Classify(
                        c,
                        incoming,
                        emulatorIncoming,
                        featuresByTrigger,
                        world.Emulator.Registry
                    )
                )
                .Select(o => ApplyLedger(o, ledger, problems)),
        ];

        List<Obligation> unresolvedSurface =
        [
            .. unresolved
                .Values.Select(c => new Obligation
                {
                    Id = c.Id,
                    Name = c.Name,
                    Domain = PacketNaming.DomainFromSourcePath(c.Packet.Evidence.Source),
                    HeaderId = null,
                    ClientClass = c.Packet.DeclaredType,
                    Status = ObligationStatus.UnresolvedSurface,
                    Reason = "the client registry does not bind this class to a header id",
                })
                .Select(o => ApplyLedger(o, ledger, problems)),
        ];

        FlagStaleLedgerEntries(ledger, obligations, unresolvedSurface, problems);

        return new CompletenessReport
        {
            // A client is only in this list because it targets the build this emulator answers on,
            // so the emulator's own revision string is the right fallback when the tree does not
            // declare one — not a null that would read downstream as "there is no target".
            TargetRevision = targets[0].Revision ?? world.Emulator.Revision,
            TargetOrigin = targets[0].Origin,
            Obligations =
            [
                .. obligations
                    .OrderBy(o => o.Domain, StringComparer.Ordinal)
                    .ThenBy(o => o.Name, StringComparer.Ordinal),
            ],
            UnresolvedSurface =
            [
                .. unresolvedSurface
                    .OrderBy(o => o.Domain, StringComparer.Ordinal)
                    .ThenBy(o => o.ClientClass, StringComparer.Ordinal),
            ],
            Problems = [.. problems.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
        };
    }

    /// <summary>
    /// Works out which packet a target-client class is, by the same rule the packet resolver uses.
    /// </summary>
    /// <remarks>
    /// It has to be the same rule or the join is fiction: the resolver renames an obfuscated class to
    /// whatever Vortex answers on for its header id, so keying obligations by the obfuscated name
    /// would report every implemented feature as missing. Where no id joins and no name survived
    /// obfuscation the obligation is keyed by the id itself — the client can send that number and
    /// this emulator has nothing bound to it, which is precisely a gap and not a naming problem.
    /// </remarks>
    private static string ResolveName(
        ClientPacket packet,
        int header,
        IReadOnlyDictionary<int, string> namesById
    ) =>
        namesById.TryGetValue(header, out string? joined) ? joined
        : PacketNaming.IsSyntheticTypeName(packet.DeclaredType) ? $"header:{header}"
        : packet.Canonical;

    private static Obligation Classify(
        Candidate candidate,
        IReadOnlyDictionary<string, PacketSpec> incoming,
        IReadOnlyDictionary<string, EmulatorIncoming> emulator,
        ILookup<string, FeatureSpec> featuresByTrigger,
        RevisionRegistry registry
    )
    {
        PacketSpec? spec = incoming.GetValueOrDefault(candidate.Name);
        int header = candidate.Packet.HeaderId!.Value;

        string domain = spec is { Domain: not "unsorted" } named
            ? named.Domain
            : PacketNaming.DomainFromSourcePath(candidate.Packet.Evidence.Source);

        Obligation basis = new()
        {
            Id = candidate.Id,
            Name = candidate.Name,
            Domain = domain,
            HeaderId = header,
            ClientClass = candidate.Packet.DeclaredType,
            Status = ObligationStatus.Missing,
            Reason = string.Empty,
            MappedInVortex = spec?.MappedInVortex ?? false,
            VortexHandler = spec?.VortexHandler,
            ConflictIds = spec?.ConflictIds ?? [],
            UnknownIds = spec?.UnknownIds ?? [],
        };

        if (spec is null)
        {
            return basis with
            {
                Reason =
                    $"the client sends header {header.ToString(CultureInfo.InvariantCulture)} and "
                    + "nothing in this repository is bound to it",
            };
        }

        if (!spec.MappedInVortex)
        {
            return basis with
            {
                Reason =
                    "the packet is described but no revision map binds it, so it cannot arrive",
            };
        }

        if (spec.VortexHandler is null)
        {
            // A parser that never names the message it produces gives the flow analyzer nothing to
            // join a handler to, so "no handler" here would be the analyzer's blind spot reported as
            // the emulator's gap. Absent evidence is not evidence of absence.
            if (
                emulator.TryGetValue(candidate.Name, out EmulatorIncoming? mapped)
                && mapped.MessageType is null
            )
            {
                return basis with
                {
                    Status = ObligationStatus.Unknown,
                    Reason =
                        $"{mapped.ParserType ?? "the parser"} does not name the message it produces, "
                        + "so no handler could be looked for",
                };
            }

            return basis with
            {
                Reason = "mapped, but no handler receives the parsed message",
            };
        }

        // A name that joins but an id that does not. One of the two claims is wrong and the analyzer
        // cannot say which, so it says so rather than picking the flattering one.
        if (
            registry.Incoming.TryGetValue(candidate.Name, out int vortexHeader)
            && vortexHeader != header
        )
        {
            return basis with
            {
                Status = ObligationStatus.Unknown,
                Reason =
                    $"the client sends this as {header.ToString(CultureInfo.InvariantCulture)} and "
                    + $"Vortex answers on {vortexHeader.ToString(CultureInfo.InvariantCulture)}; "
                    + "one of the two is wrong",
            };
        }

        FeatureSpec? feature = featuresByTrigger[candidate.Name]
            .OrderByDescending(f => f.ObservedInVortex)
            .ThenBy(f => f.Id, StringComparer.Ordinal)
            .FirstOrDefault();

        if (feature is null)
        {
            return basis with
            {
                Status = ObligationStatus.Partial,
                Reason = $"{spec.VortexHandler} receives it; no flow past the handler was observed",
            };
        }

        if (!feature.ObservedInVortex)
        {
            return basis with
            {
                Status = ObligationStatus.Partial,
                FeatureId = feature.Id,
                Reason = $"{feature.Id} exists but the handler reaches no domain operation",
            };
        }

        return basis with
        {
            Status = ObligationStatus.Implemented,
            FeatureId = feature.Id,
            Reason = $"{feature.Id} reaches a domain operation from {spec.VortexHandler}",
        };
    }

    /// <summary>
    /// Applies the two hand-maintained files, in the only order that cannot launder a gap.
    /// </summary>
    /// <remarks>
    /// A verification record promotes exactly one rung, from implemented to complete, and only from
    /// there. Pointed at anything else it is not ignored — it is reported, because a record claiming
    /// a missing obligation was verified is either a lie or a stale file, and both need fixing.
    /// </remarks>
    private static Obligation ApplyLedger(
        Obligation obligation,
        CompletenessLedger ledger,
        List<string> problems
    )
    {
        Obligation result = obligation;

        if (ledger.Decisions.TryGetValue(obligation.Id, out DecisionRecord? decision))
        {
            result = result with
            {
                Status = ObligationStatus.NotApplicable,
                DecisionId = decision.DecidedBy,
                Reason = decision.Reason,
            };
        }

        if (!ledger.Verifications.TryGetValue(obligation.Id, out VerificationRecord? verification))
        {
            return result;
        }

        if (result.Status != ObligationStatus.Implemented)
        {
            problems.Add(
                $"verification.yaml: {obligation.Id} is {result.Status.Wire()}; a verification "
                    + "record may only promote an implemented obligation"
            );

            return result;
        }

        return result with
        {
            Status = ObligationStatus.Complete,
            VerifiedAtCommit = verification.VerifiedAtCommit,
            Reason = $"{result.Reason}; verified at {verification.VerifiedAtCommit}",
        };
    }

    /// <summary>
    /// Reports ledger entries that name nothing in the target surface, so the files do not silently
    /// accumulate records for packets the client no longer has.
    /// </summary>
    private static void FlagStaleLedgerEntries(
        CompletenessLedger ledger,
        IReadOnlyList<Obligation> scored,
        IReadOnlyList<Obligation> unresolved,
        List<string> problems
    )
    {
        HashSet<string> known = [.. scored.Select(o => o.Id), .. unresolved.Select(o => o.Id)];

        foreach (string key in ledger.Decisions.Keys.Where(k => !known.Contains(k)))
        {
            problems.Add($"decisions.yaml: {key} is not an obligation of the target client");
        }

        foreach (string key in ledger.Verifications.Keys.Where(k => !known.Contains(k)))
        {
            problems.Add($"verification.yaml: {key} is not an obligation of the target client");
        }
    }

    private static Dictionary<int, string> ReverseTable(IReadOnlyDictionary<string, int> table)
    {
        Dictionary<int, string> reversed = [];

        foreach (
            KeyValuePair<string, int> entry in table.OrderBy(e => e.Key, StringComparer.Ordinal)
        )
        {
            reversed.TryAdd(entry.Value, entry.Key);
        }

        return reversed;
    }
}
