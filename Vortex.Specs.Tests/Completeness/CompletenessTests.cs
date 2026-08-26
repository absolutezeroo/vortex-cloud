using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Vortex.Specs.Analysis.Client;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Completeness;
using Vortex.Specs.Model;
using Vortex.Specs.Pipeline;
using Vortex.Specs.Reasoning;
using Vortex.Specs.Tests.Fixtures;
using Xunit;

namespace Vortex.Specs.Tests.Completeness;

/// <summary>
/// Holds the completeness classifier to the one property that matters: a gap cannot disappear.
/// </summary>
/// <remarks>
/// Every case here is built from the target client downwards rather than from Vortex outwards,
/// because that is the direction the real bug runs in — a measurement derived from the emulator's own
/// collections reports a feature nobody implemented as absent from the denominator instead of
/// absent from the emulator, and comes out at 100%.
/// </remarks>
public class CompletenessTests
{
    private const string Revision = "WIN63-TEST";

    private static ClientPacket Incoming(string declaredType, int? headerId) =>
        new()
        {
            Canonical = Vortex.Specs.Naming.PacketNaming.Canonical(declaredType),
            Direction = PacketDirection.Incoming,
            DeclaredType = declaredType,
            HeaderId = headerId,
            Fields = [new ClientField { Name = null, Type = WireType.Int32 }],
            Evidence = new EvidenceRef
            {
                Kind = EvidenceKind.ClientComposer,
                Authority = EvidenceAuthority.ClientCode,
                Origin = $"as3:{Revision}",
                Source = $"../sources/{Revision}/src/com/sulake/outgoing/room/{declaredType}.as",
                Symbol = declaredType,
            },
        };

    private static ClientScan Target(params ClientPacket[] packets) =>
        new()
        {
            Origin = $"as3:{Revision}",
            Authority = EvidenceAuthority.ClientCode,
            Revision = Revision,
            TargetsSameRevision = true,
            Packets = packets,
        };

    private static EmulatorIncoming Mapped(string canonical, string? handler)
    {
        EmulatorIncoming basis = WorldBuilder.Incoming(canonical, ("ObjectId", WireType.Int32));

        return basis with
        {
            HandlerType = handler,
        };
    }

    private static EmulatorFlow Flow(string canonical, bool meaningful)
    {
        EvidenceRef evidence = new()
        {
            Kind = EvidenceKind.EmulatorHandler,
            Authority = EvidenceAuthority.VortexEmulator,
            Origin = "vortex",
            Source = $"Vortex.PacketHandlers/Room/{canonical}Handler.cs",
            Symbol = $"{canonical}Handler",
        };

        return new EmulatorFlow
        {
            HandlerType = $"{canonical}Handler",
            MessageType = $"{canonical}Message",
            PrimaryOperation = meaningful ? $"Move{canonical}Async" : null,
            Steps =
            [
                new FeatureFlowStep
                {
                    Order = 0,
                    Layer = "handler",
                    Symbol = $"{canonical}Handler",
                    Evidence = evidence,
                },
                .. meaningful
                    ?
                    [
                        new FeatureFlowStep
                        {
                            Order = 1,
                            Layer = "grain",
                            Symbol = $"IRoomGrain.Move{canonical}Async",
                            Evidence = evidence,
                        },
                    ]
                    : new List<FeatureFlowStep>(),
            ],
        };
    }

    private static CompletenessReport Analyze(SpecWorld world, CompletenessLedger? ledger = null)
    {
        List<string> notes = [];
        ResolvedSpecs specs = SpecPipeline.Resolve(world, notes);

        return new CompletenessAnalyzer().Analyze(world, specs, ledger ?? CompletenessLedger.Empty);
    }

    private static Obligation Single(CompletenessReport report, string name) =>
        report.Obligations.Single(o => o.Name == name);

    [Fact]
    public void A_packet_the_client_sends_and_vortex_has_never_heard_of_is_missing()
    {
        // Obfuscated class, header id nothing in Vortex answers on: no name, no parser, no handler.
        // This is the case a Vortex-derived denominator cannot represent at all.
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(),
            clients: [Target(Incoming("_SafeCls_3619", 3619))]
        );

        CompletenessReport report = Analyze(world);

        report.Obligations.Should().ContainSingle();
        report.Obligations[0].Name.Should().Be("header:3619");
        report.Obligations[0].Status.Should().Be(ObligationStatus.Missing);
        report.Obligations[0].MappedInVortex.Should().BeFalse();
    }

    [Fact]
    public void A_packet_described_but_bound_by_no_revision_map_is_missing()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(),
            clients: [Target(Incoming("MoveObjectComposer", 3619))]
        );

        CompletenessReport report = Analyze(world);

        Obligation obligation = Single(report, "MoveObject");
        obligation.Status.Should().Be(ObligationStatus.Missing);
        obligation.Reason.Should().Contain("no revision map binds it");
    }

    [Fact]
    public void A_mapped_packet_no_handler_receives_is_missing()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [Mapped("MoveObject", handler: null)],
                incomingHeaders: new Dictionary<string, int> { ["MoveObject"] = 3619 }
            ),
            clients: [Target(Incoming("_SafeCls_3619", 3619))]
        );

        CompletenessReport report = Analyze(world);

        Obligation obligation = Single(report, "MoveObject");
        obligation.MappedInVortex.Should().BeTrue();
        obligation.Status.Should().Be(ObligationStatus.Missing);
    }

    [Fact]
    public void A_parser_that_names_no_message_is_unknown_not_missing()
    {
        // The wired parsers derive from a shared base and name their record in a typeof. With the
        // message type unresolved the flow analyzer has nothing to join a handler to, and reporting
        // that as "no handler" would publish the reader's blind spot as the emulator's gap.
        EmulatorIncoming anonymous = Mapped("UpdateAction", handler: null) with
        {
            MessageType = null,
        };

        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [anonymous],
                incomingHeaders: new Dictionary<string, int> { ["UpdateAction"] = 2197 }
            ),
            clients: [Target(Incoming("_SafeCls_2689", 2197))]
        );

        CompletenessReport report = Analyze(world);

        Obligation obligation = Single(report, "UpdateAction");
        obligation.Status.Should().Be(ObligationStatus.Unknown);
        obligation.Reason.Should().Contain("does not name the message it produces");
    }

    [Fact]
    public void A_handler_that_reaches_nothing_is_partial_not_implemented()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [Mapped("MoveObject", "MoveObjectHandler")],
                flows: [Flow("MoveObject", meaningful: false)],
                incomingHeaders: new Dictionary<string, int> { ["MoveObject"] = 3619 }
            ),
            clients: [Target(Incoming("_SafeCls_3619", 3619))]
        );

        CompletenessReport report = Analyze(world);

        Single(report, "MoveObject").Status.Should().Be(ObligationStatus.Partial);
    }

    [Fact]
    public void A_handler_that_reaches_a_domain_operation_is_implemented_and_not_complete()
    {
        CompletenessReport report = Analyze(Implemented());

        Obligation obligation = Single(report, "MoveObject");
        obligation.Status.Should().Be(ObligationStatus.Implemented);
        obligation.FeatureId.Should().NotBeNull();
        report.Count(ObligationStatus.Complete).Should().Be(0);
    }

    [Fact]
    public void A_client_class_with_no_header_id_is_held_outside_the_denominator()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(),
            clients: [Target(Incoming("_SafeCls_9001", headerId: null))]
        );

        CompletenessReport report = Analyze(world);

        report.Obligations.Should().BeEmpty();
        report.UnresolvedSurface.Should().ContainSingle();
        report.UnresolvedSurface[0].Status.Should().Be(ObligationStatus.UnresolvedSurface);
    }

    [Fact]
    public void A_client_of_another_build_is_evidence_and_never_the_denominator()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(),
            clients:
            [
                new ClientScan
                {
                    Origin = "as3:WIN63-OLD",
                    Authority = EvidenceAuthority.ClientCode,
                    Revision = "WIN63-OLD",
                    TargetsSameRevision = false,
                    Packets = [Incoming("_SafeCls_101", 101)],
                },
            ]
        );

        CompletenessReport report = Analyze(world);

        report.HasTargetClient.Should().BeFalse();
        report.Obligations.Should().BeEmpty();
    }

    [Fact]
    public void Two_client_classes_for_one_message_are_one_obligation()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [Mapped("MoveObject", "MoveObjectHandler")],
                incomingHeaders: new Dictionary<string, int> { ["MoveObject"] = 3619 }
            ),
            clients: [Target(Incoming("_SafeCls_3619", 3619), Incoming("MoveObjectComposer", 3619))]
        );

        CompletenessReport report = Analyze(world);

        report.Obligations.Should().ContainSingle();
    }

    [Fact]
    public void An_exclusion_without_its_paper_trail_is_a_problem_not_an_exclusion()
    {
        CompletenessLedger ledger = CompletenessLedger.FromText(
            """
            version: 1
            obligations:
              incoming/MoveObject:
                reachability: not_applicable
                reason: "too hard"
            """,
            null
        );

        CompletenessReport report = Analyze(Implemented(), ledger);

        // Still scored on its merits, and the half-written record is reported rather than obeyed.
        Single(report, "MoveObject").Status.Should().Be(ObligationStatus.Implemented);
        report.Problems.Should().ContainSingle(p => p.Contains("evidence, decided_by"));
    }

    [Fact]
    public void An_exclusion_carrying_its_paper_trail_is_honoured()
    {
        CompletenessLedger ledger = CompletenessLedger.FromText(
            """
            version: 1
            obligations:
              incoming/MoveObject:
                reachability: not_applicable
                reason: "the target client exits before this becomes reachable"
                evidence:
                  - "client:WIN63-TEST:_SafeCls_3619"
                decided_by: "ADR-FC-001"
            """,
            null
        );

        CompletenessReport report = Analyze(Implemented(), ledger);

        Obligation obligation = Single(report, "MoveObject");
        obligation.Status.Should().Be(ObligationStatus.NotApplicable);
        obligation.DecisionId.Should().Be("ADR-FC-001");
        report.Problems.Should().BeEmpty();
    }

    [Fact]
    public void Verification_cannot_promote_something_nobody_implemented()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(),
            clients: [Target(Incoming("MoveObjectComposer", 3619))]
        );

        CompletenessLedger ledger = CompletenessLedger.FromText(
            null,
            """
            version: 1
            obligations:
              incoming/MoveObject:
                verified_at_commit: "abc1234"
                tests:
                  - "Vortex.Rooms.Tests/MoveObjectTests.cs"
                protocol:
                  - "docs/habbo-specs/packets/incoming/room/move_object.yaml"
            """
        );

        CompletenessReport report = Analyze(world, ledger);

        Single(report, "MoveObject").Status.Should().Be(ObligationStatus.Missing);
        report.Problems.Should().ContainSingle(p => p.Contains("may only promote an implemented"));
    }

    [Fact]
    public void Verification_promotes_exactly_one_rung()
    {
        CompletenessLedger ledger = CompletenessLedger.FromText(
            null,
            """
            version: 1
            obligations:
              incoming/MoveObject:
                verified_at_commit: "abc1234"
                tests:
                  - "Vortex.Rooms.Tests/MoveObjectTests.cs"
                protocol:
                  - "docs/habbo-specs/packets/incoming/room/move_object.yaml"
            """
        );

        CompletenessReport report = Analyze(Implemented(), ledger);

        Obligation obligation = Single(report, "MoveObject");
        obligation.Status.Should().Be(ObligationStatus.Complete);
        obligation.VerifiedAtCommit.Should().Be("abc1234");
        report.Problems.Should().BeEmpty();
    }

    [Fact]
    public void No_target_client_produces_no_score_rather_than_a_clean_hundred_percent()
    {
        CompletenessReport report = Analyze(WorldBuilder.World(WorldBuilder.Emulator()));

        report.HasTargetClient.Should().BeFalse();
        report.Share(0).Should().Be("n/a");
        report.Problems.Should().ContainSingle(p => p.Contains("no official client targeting"));
    }

    [Fact]
    public void The_generated_tree_is_byte_identical_for_unchanged_input()
    {
        CompletenessReport report = Analyze(Implemented());

        IReadOnlyList<GeneratedFile> first = CompletenessWriter.Render(report);
        IReadOnlyList<GeneratedFile> second = CompletenessWriter.Render(Analyze(Implemented()));

        first.Select(f => f.RelativePath).Should().Contain("SUMMARY.md");
        first.Select(f => f.RelativePath).Should().Contain("matrix.yaml");
        second.Should().BeEquivalentTo(first, o => o.WithStrictOrdering());
    }

    [Fact]
    public void A_contradictory_header_join_is_unknown_rather_than_a_flattering_guess()
    {
        // The name joins a mapped packet with a handler, but the client sends it on a different id.
        // One of the two claims is wrong and static analysis cannot say which.
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [Mapped("MoveObject", "MoveObjectHandler")],
                flows: [Flow("MoveObject", meaningful: true)],
                incomingHeaders: new Dictionary<string, int> { ["MoveObject"] = 1234 }
            ),
            clients: [Target(Incoming("MoveObjectComposer", 3619))]
        );

        CompletenessReport report = Analyze(world);

        Obligation obligation = Single(report, "MoveObject");
        obligation.Status.Should().Be(ObligationStatus.Unknown);
        obligation.Reason.Should().Contain("3619").And.Contain("1234");
    }

    /// <summary>A world where MoveObject is mapped, handled and reaches a domain operation.</summary>
    private static SpecWorld Implemented() =>
        WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [Mapped("MoveObject", "MoveObjectHandler")],
                flows: [Flow("MoveObject", meaningful: true)],
                incomingHeaders: new Dictionary<string, int> { ["MoveObject"] = 3619 }
            ),
            clients: [Target(Incoming("_SafeCls_3619", 3619))]
        );
}
