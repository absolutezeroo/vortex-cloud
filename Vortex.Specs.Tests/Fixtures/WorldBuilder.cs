using System.Collections.Generic;
using System.Linq;
using Vortex.Specs.Analysis.Client;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Analysis.Reference;
using Vortex.Specs.Model;
using Vortex.Specs.Reasoning;

namespace Vortex.Specs.Tests.Fixtures;

/// <summary>
/// Assembles a <see cref="SpecWorld"/> from hand-written sources.
/// </summary>
/// <remarks>
/// The reconciliation rules — which source supplies the shape, which supplies each name, what counts
/// as a disagreement — need cases the real trees do not happen to contain: a client that is right
/// about the shape and silent about the types, two implementations that disagree on a field count,
/// a reader that stopped halfway. Those are constructed here; the analyzers themselves are tested
/// against the real trees.
/// </remarks>
public static class WorldBuilder
{
    public static EvidenceRef Evidence(string origin, EvidenceAuthority authority) =>
        new()
        {
            Kind = EvidenceKind.ClientComposer,
            Authority = authority,
            Origin = origin,
            Source = $"synthetic/{origin}.txt",
            Symbol = "Synthetic",
        };

    public static ClientPacket Packet(
        string canonical,
        PacketDirection direction,
        EvidenceAuthority authority,
        string origin,
        bool partial = false,
        params (string? Name, WireType Type)[] fields
    ) =>
        new()
        {
            Canonical = canonical,
            Direction = direction,
            DeclaredType = canonical + "Composer",
            Fields = [.. fields.Select(f => new ClientField { Name = f.Name, Type = f.Type })],
            IsPartial = partial,
            Evidence = Evidence(origin, authority),
        };

    public static ClientScan Client(
        string origin,
        EvidenceAuthority authority,
        bool sameRevision,
        params ClientPacket[] packets
    ) =>
        new()
        {
            Origin = origin,
            Authority = authority,
            TargetsSameRevision = sameRevision,
            Packets = packets,
        };

    public static ReferenceScan Reference(string origin, params ReferenceBehaviour[] behaviours) =>
        new()
        {
            Origin = origin,
            Authority = EvidenceAuthority.ReferenceEmulator,
            Behaviours = behaviours,
            Composers = [],
        };

    public static ReferenceBehaviour Behaviour(
        string canonical,
        string origin,
        IEnumerable<(string? Name, WireType Type)>? fields = null,
        IEnumerable<string>? emits = null
    )
    {
        EvidenceRef evidence = Evidence(origin, EvidenceAuthority.ReferenceEmulator);

        return new ReferenceBehaviour
        {
            Canonical = canonical,
            HandlerType = canonical + "MessageEvent",
            Fields =
            [
                .. (fields ?? []).Select(f => new ClientField { Name = f.Name, Type = f.Type }),
            ],
            Outgoing =
            [
                .. (emits ?? []).Select(
                    (packet, index) =>
                        new FeatureOutgoing
                        {
                            Packet = packet,
                            Recipient = Recipient.Actor,
                            RecipientConfidence = Confidence.ReferenceObserved,
                            Order = index,
                            Evidence = evidence,
                        }
                ),
            ],
            Evidence = evidence,
        };
    }

    public static EmulatorScan Emulator(
        IEnumerable<EmulatorIncoming>? incoming = null,
        IEnumerable<EmulatorOutgoing>? outgoing = null,
        IEnumerable<EmulatorFlow>? flows = null,
        IDictionary<string, int>? incomingHeaders = null
    )
    {
        EvidenceRef evidence = new()
        {
            Kind = EvidenceKind.EmulatorHeader,
            Authority = EvidenceAuthority.VortexEmulator,
            Origin = "vortex",
            Source = "Vortex.Revisions/Revision20260701/Headers.cs",
        };

        return new EmulatorScan
        {
            Revision = "WIN63-TEST",
            Incoming = [.. incoming ?? []],
            Outgoing = [.. outgoing ?? []],
            Flows = [.. flows ?? []],
            UnmappedHeaderConstants = [],
            UnmappedImplementations = [],
            Registry = new RevisionRegistry
            {
                Id = "WIN63-TEST",
                Origin = "vortex",
                Authority = EvidenceAuthority.VortexEmulator,
                TargetsSameRevision = true,
                Incoming = incomingHeaders is null
                    ? new Dictionary<string, int>()
                    : new Dictionary<string, int>(incomingHeaders),
                Outgoing = new Dictionary<string, int>(),
                Evidence = evidence,
            },
        };
    }

    public static EmulatorIncoming Incoming(
        string canonical,
        params (string? Name, WireType Type)[] fields
    ) =>
        new()
        {
            Canonical = canonical,
            HeaderConstant = canonical + "MessageEvent",
            ParserType = canonical + "MessageParser",
            MessageType = canonical + "Message",
            Layout =
            [
                .. fields.Select(
                    (f, i) =>
                        new WireOp
                        {
                            Type = f.Type,
                            Name = f.Name,
                            Line = i + 1,
                        }
                ),
            ],
            ParserEvidence = new EvidenceRef
            {
                Kind = EvidenceKind.EmulatorParser,
                Authority = EvidenceAuthority.VortexEmulator,
                Origin = "vortex",
                Source =
                    $"Vortex.Revisions/Revision20260701/Parsers/Room/{canonical}MessageParser.cs",
                Symbol = canonical + "MessageParser",
            },
        };

    public static SpecWorld World(
        EmulatorScan? emulator = null,
        IEnumerable<ClientScan>? clients = null,
        IEnumerable<ReferenceScan>? references = null
    ) =>
        new()
        {
            Emulator = emulator ?? Emulator(),
            Clients = [.. clients ?? []],
            References = [.. references ?? []],
            Captures = [],
            Observations = [],
            TriggerSummaries = [],
            Problems = [],
        };
}
