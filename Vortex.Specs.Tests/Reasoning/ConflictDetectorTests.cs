using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Model;
using Vortex.Specs.Reasoning;
using Vortex.Specs.Tests.Fixtures;
using Xunit;

namespace Vortex.Specs.Tests.Reasoning;

public class ConflictDetectorTests
{
    private static IReadOnlyList<ConflictSpec> Detect(SpecWorld world) =>
        new ConflictDetector().Detect(world, new PacketResolver().Resolve(world));

    [Fact]
    public void Sources_agreeing_produce_no_conflict()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("MoveObject", ("X", WireType.Int32))]
            ),
            references:
            [
                WorldBuilder.Reference(
                    "arcturus",
                    WorldBuilder.Behaviour(
                        "MoveObject",
                        "arcturus",
                        fields: [("x", WireType.Int32)]
                    )
                ),
            ]
        );

        Detect(world).Should().BeEmpty();
    }

    [Fact]
    public void Two_sources_disagreeing_on_the_field_count_is_a_conflict_that_keeps_both_positions()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming:
                [
                    WorldBuilder.Incoming(
                        "MoveObject",
                        ("X", WireType.Int32),
                        ("Y", WireType.Int32)
                    ),
                ]
            ),
            references:
            [
                WorldBuilder.Reference(
                    "arcturus",
                    WorldBuilder.Behaviour(
                        "MoveObject",
                        "arcturus",
                        fields:
                        [
                            ("x", WireType.Int32),
                            ("y", WireType.Int32),
                            ("rot", WireType.Int32),
                        ]
                    )
                ),
            ]
        );

        ConflictSpec conflict = Detect(world).Single(c => c.Kind == ConflictKind.FieldCount);

        conflict.Positions.Should().HaveCount(2);
        conflict.Positions.Select(p => p.Origin).Should().Contain(["vortex", "arcturus"]);
        // Never arbitrated. The disagreement is the answer until evidence closes it.
        conflict.OfficialStatus.Should().Be(Confidence.Unknown);
        conflict.Resolution.Should().BeNull();
    }

    [Fact]
    public void A_partial_layout_is_not_treated_as_a_claim_about_the_field_count()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming:
                [
                    WorldBuilder.Incoming(
                        "MoveObject",
                        ("X", WireType.Int32),
                        ("Y", WireType.Int32)
                    ),
                ]
            ),
            clients:
            [
                WorldBuilder.Client(
                    "official",
                    EvidenceAuthority.ClientCode,
                    sameRevision: true,
                    WorldBuilder.Packet(
                        "MoveObject",
                        PacketDirection.Incoming,
                        EvidenceAuthority.ClientCode,
                        "official",
                        partial: true,
                        fields: [(null, WireType.Int32)]
                    )
                ),
            ]
        );

        Detect(world).Should().NotContain(c => c.Kind == ConflictKind.FieldCount);
    }

    [Fact]
    public void An_unknown_type_is_a_gap_not_a_disagreement()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("MoveObject", ("X", WireType.Int32))]
            ),
            clients:
            [
                WorldBuilder.Client(
                    "official",
                    EvidenceAuthority.ClientCode,
                    sameRevision: true,
                    WorldBuilder.Packet(
                        "MoveObject",
                        PacketDirection.Incoming,
                        EvidenceAuthority.ClientCode,
                        "official",
                        fields: [(null, WireType.Unknown)]
                    )
                ),
            ]
        );

        Detect(world).Should().NotContain(c => c.Kind == ConflictKind.FieldType);
    }

    [Fact]
    public void Two_sources_genuinely_disagreeing_on_a_type_is_a_conflict()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("Chat", ("Text", WireType.String))]
            ),
            references:
            [
                WorldBuilder.Reference(
                    "arcturus",
                    WorldBuilder.Behaviour("Chat", "arcturus", fields: [("text", WireType.Int32)])
                ),
            ]
        );

        ConflictSpec conflict = Detect(world).Single(c => c.Kind == ConflictKind.FieldType);

        conflict.Subject.Should().Contain("field 0");
        conflict
            .Positions.Select(p => p.Claim)
            .Should()
            .Contain(c => c.Contains("string", System.StringComparison.Ordinal));
        conflict
            .Positions.Select(p => p.Claim)
            .Should()
            .Contain(c => c.Contains("int32", System.StringComparison.Ordinal));
    }

    [Fact]
    public void Header_ids_from_a_client_targeting_another_build_are_never_compared()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("MoveObject", ("X", WireType.Int32))],
                incomingHeaders: new Dictionary<string, int> { ["MoveObject"] = 1482 }
            ),
            clients:
            [
                WorldBuilder.Client(
                    "nitro",
                    EvidenceAuthority.MultiImplementation,
                    sameRevision: false,
                    WorldBuilder.Packet(
                        "MoveObject",
                        PacketDirection.Incoming,
                        EvidenceAuthority.MultiImplementation,
                        "nitro",
                        fields: [("x", WireType.Int32)]
                    )
                ) with
                {
                    // Nitro's own build numbers this 2828. Reporting that as a disagreement would
                    // flag several hundred non-conflicts and make the list worthless.
                    IncomingHeaders = new Dictionary<string, int> { ["MoveObject"] = 2828 },
                },
            ]
        );

        Detect(world).Should().NotContain(c => c.Kind == ConflictKind.HeaderId);
    }

    [Fact]
    public void A_same_build_client_disagreeing_on_a_header_id_is_a_conflict()
    {
        // This is the bug class where a header is registered on an invented id: nothing can ever
        // reach the handler, and only the client's own registry can prove it.
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("MoveObject", ("X", WireType.Int32))],
                incomingHeaders: new Dictionary<string, int> { ["MoveObject"] = 9015 }
            ),
            clients:
            [
                WorldBuilder.Client(
                    "official",
                    EvidenceAuthority.ClientCode,
                    sameRevision: true,
                    WorldBuilder.Packet(
                        "MoveObject",
                        PacketDirection.Incoming,
                        EvidenceAuthority.ClientCode,
                        "official",
                        fields: [("x", WireType.Int32)]
                    )
                ) with
                {
                    IncomingHeaders = new Dictionary<string, int> { ["MoveObject"] = 1482 },
                },
            ]
        );

        ConflictSpec conflict = Detect(world).Single(c => c.Kind == ConflictKind.HeaderId);

        conflict.Positions.Select(p => p.Claim).Should().Contain(["1482", "9015"]);
    }

    [Fact]
    public void Implementations_answering_a_trigger_differently_is_a_behaviour_conflict()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("MoveObject", ("X", WireType.Int32))],
                flows:
                [
                    new EmulatorFlow
                    {
                        HandlerType = "MoveObjectMessageHandler",
                        MessageType = "MoveObjectMessage",
                        Outgoing =
                        [
                            new FeatureOutgoing
                            {
                                Packet = "ObjectUpdate",
                                Recipient = Recipient.Actor,
                                Evidence = WorldBuilder.Evidence(
                                    "vortex",
                                    EvidenceAuthority.VortexEmulator
                                ),
                            },
                        ],
                        Steps =
                        [
                            new FeatureFlowStep
                            {
                                Order = 0,
                                Layer = "handler",
                                Symbol = "MoveObjectMessageHandler.HandleAsync",
                                Evidence = WorldBuilder.Evidence(
                                    "vortex",
                                    EvidenceAuthority.VortexEmulator
                                ),
                            },
                        ],
                    },
                ]
            ),
            references:
            [
                WorldBuilder.Reference(
                    "arcturus",
                    WorldBuilder.Behaviour(
                        "MoveObject",
                        "arcturus",
                        fields: [("x", WireType.Int32)],
                        emits: ["NotificationDialog", "ObjectUpdate"]
                    )
                ),
            ]
        );

        ConflictSpec conflict = Detect(world).Single(c => c.Kind == ConflictKind.Behaviour);

        conflict.Positions.Select(p => p.Claim).Should().Contain("ObjectUpdate");
        conflict
            .Positions.Select(p => p.Claim)
            .Should()
            .Contain("NotificationDialog then ObjectUpdate");
        conflict.OfficialStatus.Should().Be(Confidence.Unknown);
    }

    [Fact]
    public void Conflict_ids_are_stable_so_a_resolution_written_into_one_is_not_orphaned()
    {
        string first = ConflictDetector.BuildId(
            ConflictKind.FieldCount,
            "incoming/MoveObject: field count"
        );
        string second = ConflictDetector.BuildId(
            ConflictKind.FieldCount,
            "incoming/MoveObject: field count"
        );

        first.Should().Be(second);
        first.Should().StartWith("cf_");
        ConflictDetector
            .BuildId(ConflictKind.FieldType, "incoming/MoveObject: field count")
            .Should()
            .NotBe(first);
    }
}
