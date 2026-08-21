using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Vortex.Specs.Model;
using Vortex.Specs.Reasoning;
using Vortex.Specs.Tests.Fixtures;
using Xunit;

namespace Vortex.Specs.Tests.Reasoning;

public class PacketResolverTests
{
    private static PacketSpec Resolve(SpecWorld world, string name, PacketDirection direction) =>
        new PacketResolver().Resolve(world).Single(p => p.Name == name && p.Direction == direction);

    [Fact]
    public void The_strongest_complete_source_supplies_the_shape()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming:
                [
                    WorldBuilder.Incoming(
                        "MoveObject",
                        ("ObjectId", WireType.Int32),
                        ("X", WireType.Int32)
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
                        fields:
                        [
                            (null, WireType.Int32),
                            (null, WireType.Int32),
                            (null, WireType.Int32),
                            (null, WireType.Int32),
                        ]
                    )
                ),
            ]
        );

        PacketSpec packet = Resolve(world, "MoveObject", PacketDirection.Incoming);

        // Four fields, from the client, not the emulator's two.
        packet.Fields.Should().HaveCount(4);
        packet
            .Fields.Select(f => f.TypeConfidence)
            .Should()
            .AllBeEquivalentTo(Confidence.ClientConfirmed);
    }

    [Fact]
    public void Names_come_from_the_strongest_source_that_has_one_even_when_the_shape_does_not()
    {
        // The exact situation on the real trees: the official client fixes the layout for this build
        // but its identifiers are obfuscated away, while a reimplementation names every field.
        SpecWorld world = WorldBuilder.World(
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
                        fields: [(null, WireType.Int32), (null, WireType.Int32)]
                    )
                ),
                WorldBuilder.Client(
                    "nitro",
                    EvidenceAuthority.MultiImplementation,
                    sameRevision: false,
                    WorldBuilder.Packet(
                        "MoveObject",
                        PacketDirection.Incoming,
                        EvidenceAuthority.MultiImplementation,
                        "nitro",
                        fields: [("object_id", WireType.Int32), ("x", WireType.Int32)]
                    )
                ),
            ]
        );

        PacketSpec packet = Resolve(world, "MoveObject", PacketDirection.Incoming);

        packet.Fields.Select(f => f.Name).Should().Equal("object_id", "x");
        packet
            .Fields.Select(f => f.NameConfidence)
            .Should()
            .AllBeEquivalentTo(Confidence.MultiReferenceConfirmed);
        packet
            .Fields.Select(f => f.TypeConfidence)
            .Should()
            .AllBeEquivalentTo(Confidence.ClientConfirmed);
    }

    [Fact]
    public void A_type_the_shape_source_does_not_know_is_filled_in_from_the_next_source_down()
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

        PacketSpec packet = Resolve(world, "MoveObject", PacketDirection.Incoming);

        packet.Fields[0].Type.Should().Be(WireType.Int32);
        packet.Fields[0].TypeConfidence.Should().Be(Confidence.ImplementationObserved);
        packet.Fields[0].Name.Should().Be("x");
    }

    [Fact]
    public void A_field_nothing_names_gets_a_placeholder_and_says_so()
    {
        SpecWorld world = WorldBuilder.World(
            clients:
            [
                WorldBuilder.Client(
                    "official",
                    EvidenceAuthority.ClientCode,
                    sameRevision: true,
                    WorldBuilder.Packet(
                        "Mystery",
                        PacketDirection.Incoming,
                        EvidenceAuthority.ClientCode,
                        "official",
                        fields: [(null, WireType.Int32), (null, WireType.String)]
                    )
                ),
            ]
        );

        PacketSpec packet = Resolve(world, "Mystery", PacketDirection.Incoming);

        packet.Fields.Select(f => f.Name).Should().Equal("unknown_0", "unknown_1");
        packet.Fields.Should().OnlyContain(f => f.IsPlaceholderName);
        packet.Fields.Should().OnlyContain(f => f.NameConfidence == Confidence.Unknown);
    }

    [Fact]
    public void A_source_with_a_different_field_count_lends_no_names()
    {
        SpecWorld world = WorldBuilder.World(
            clients:
            [
                WorldBuilder.Client(
                    "official",
                    EvidenceAuthority.ClientCode,
                    sameRevision: true,
                    WorldBuilder.Packet(
                        "Drifted",
                        PacketDirection.Incoming,
                        EvidenceAuthority.ClientCode,
                        "official",
                        fields: [(null, WireType.Int32), (null, WireType.Int32)]
                    )
                ),
                WorldBuilder.Client(
                    "nitro",
                    EvidenceAuthority.MultiImplementation,
                    sameRevision: false,
                    WorldBuilder.Packet(
                        "Drifted",
                        PacketDirection.Incoming,
                        EvidenceAuthority.MultiImplementation,
                        "nitro",
                        fields:
                        [
                            ("a", WireType.Int32),
                            ("b", WireType.Int32),
                            ("c", WireType.Int32),
                        ]
                    )
                ),
            ]
        );

        PacketSpec packet = Resolve(world, "Drifted", PacketDirection.Incoming);

        // Describing a different shape means index 0 is not the same field, so its name is not a
        // name for ours.
        packet.Fields.Should().OnlyContain(f => f.IsPlaceholderName);
    }

    [Fact]
    public void A_partial_layout_never_wins_the_shape()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming:
                [
                    WorldBuilder.Incoming(
                        "Partialish",
                        ("A", WireType.Int32),
                        ("B", WireType.Int32),
                        ("C", WireType.String)
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
                        "Partialish",
                        PacketDirection.Incoming,
                        EvidenceAuthority.ClientCode,
                        "official",
                        partial: true,
                        fields: [(null, WireType.Int32)]
                    )
                ),
            ]
        );

        PacketSpec packet = Resolve(world, "Partialish", PacketDirection.Incoming);

        packet.Fields.Should().HaveCount(3);
        packet.Fields.Select(f => f.Name).Should().Equal("a", "b", "c");
    }

    [Fact]
    public void Every_source_s_own_view_is_kept_alongside_the_merged_one()
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
                        fields: [("x", WireType.Int32), ("y", WireType.Int32)]
                    )
                ),
            ]
        );

        PacketSpec packet = Resolve(world, "MoveObject", PacketDirection.Incoming);

        packet.Observations.Should().HaveCount(2);
        packet.Observations.Select(o => o.Origin).Should().Contain(["vortex", "arcturus"]);
        packet.Observations.Single(o => o.Origin == "arcturus").Fields.Should().HaveCount(2);
    }

    [Fact]
    public void An_obfuscated_client_class_is_joined_to_a_name_by_its_header_id()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming: [WorldBuilder.Incoming("MoveObject", ("X", WireType.Int32))],
                incomingHeaders: new Dictionary<string, int> { ["MoveObject"] = 1482 }
            ),
            clients:
            [
                WorldBuilder.Client(
                    "official",
                    EvidenceAuthority.ClientCode,
                    sameRevision: true,
                    WorldBuilder.Packet(
                        "_SafeCls_3667",
                        PacketDirection.Incoming,
                        EvidenceAuthority.ClientCode,
                        "official",
                        fields: [(null, WireType.Int32)]
                    ) with
                    {
                        DeclaredType = "_SafeCls_3667",
                        HeaderId = 1482,
                    }
                ),
            ]
        );

        IReadOnlyList<PacketSpec> packets = new PacketResolver().Resolve(world);

        packets.Should().ContainSingle(p => p.Name == "MoveObject");
        packets
            .Should()
            .NotContain(p => p.Name.StartsWith("_SafeCls_", System.StringComparison.Ordinal));
        packets.Single(p => p.Name == "MoveObject").Observations.Should().HaveCount(2);
    }

    [Fact]
    public void An_obfuscated_class_that_joins_to_nothing_is_reported_rather_than_dropped_quietly()
    {
        SpecWorld world = WorldBuilder.World(
            clients:
            [
                WorldBuilder.Client(
                    "official",
                    EvidenceAuthority.ClientCode,
                    sameRevision: true,
                    WorldBuilder.Packet(
                        "_SafeCls_9999",
                        PacketDirection.Incoming,
                        EvidenceAuthority.ClientCode,
                        "official",
                        fields: [(null, WireType.Int32)]
                    ) with
                    {
                        DeclaredType = "_SafeCls_9999",
                        HeaderId = 4242,
                    }
                ),
            ]
        );

        List<string> notes = [];
        new PacketResolver().Resolve(world, notes);

        notes.Should().ContainSingle();
        notes[0].Should().Contain("no usable name");
    }

    [Fact]
    public void A_packet_only_a_client_knows_about_is_recorded_as_unmapped_here()
    {
        SpecWorld world = WorldBuilder.World(
            clients:
            [
                WorldBuilder.Client(
                    "nitro",
                    EvidenceAuthority.MultiImplementation,
                    sameRevision: false,
                    WorldBuilder.Packet(
                        "SomethingWeNeverBuilt",
                        PacketDirection.Incoming,
                        EvidenceAuthority.MultiImplementation,
                        "nitro",
                        fields: [("a", WireType.Int32)]
                    )
                ),
            ]
        );

        PacketSpec packet = Resolve(world, "SomethingWeNeverBuilt", PacketDirection.Incoming);

        packet.MappedInVortex.Should().BeFalse();
        packet.VortexHandler.Should().BeNull();
    }

    [Fact]
    public void The_output_order_is_stable_across_runs()
    {
        SpecWorld world = WorldBuilder.World(
            emulator: WorldBuilder.Emulator(
                incoming:
                [
                    WorldBuilder.Incoming("Zulu", ("A", WireType.Int32)),
                    WorldBuilder.Incoming("Alpha", ("A", WireType.Int32)),
                    WorldBuilder.Incoming("Mike", ("A", WireType.Int32)),
                ]
            )
        );

        new PacketResolver()
            .Resolve(world)
            .Select(p => p.Name)
            .Should()
            .Equal("Alpha", "Mike", "Zulu");
    }
}
