using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Model;
using Vortex.Specs.Tests.Fixtures;
using Xunit;

namespace Vortex.Specs.Tests.Analysis;

[Collection(RepositoryCollection.Name)]
public class EmulatorAnalyzerTests(RepositoryFixture fixture)
{
    private EmulatorIncoming Incoming(string canonical) =>
        fixture.Scan.Incoming.Single(i => i.Canonical == canonical);

    private EmulatorOutgoing Outgoing(string canonical) =>
        fixture.Scan.Outgoing.Single(o => o.Canonical == canonical);

    [Fact]
    public void Finds_the_revision_the_emulator_targets()
    {
        fixture.Scan.Revision.Should().StartWith("WIN63-");
    }

    [Fact]
    public void Discovers_hundreds_of_mapped_packets_in_both_directions()
    {
        fixture.Scan.Incoming.Should().HaveCountGreaterThan(200);
        fixture.Scan.Outgoing.Should().HaveCountGreaterThan(200);
    }

    [Fact]
    public void Reads_the_MoveObject_layout_in_wire_order()
    {
        EmulatorIncoming move = Incoming("MoveObject");

        move.ParserType.Should().Be("MoveObjectMessageParser");
        move.MessageType.Should().Be("MoveObjectMessage");
        move.HeaderId.Should().BeGreaterThan(0);

        move.Layout.Select(op => (op.Name, op.Type))
            .Should()
            .Equal(
                ("ObjectId", WireType.Int32),
                ("X", WireType.Int32),
                ("Y", WireType.Int32),
                ("Rotation", WireType.Int32)
            );

        move.Layout[0].SemanticType.Should().Be("RoomObjectId");
        move.Layout[3].SemanticType.Should().Be("Rotation");
    }

    [Fact]
    public void Follows_a_fluent_write_chain_in_evaluation_order_not_syntax_order()
    {
        // FloorItemSerializer writes eight values as one chained expression. A pre-order walk of the
        // syntax tree yields them backwards, so this is the test that pins the ordering rule down.
        EmulatorOutgoing update = Outgoing("ObjectUpdate");

        IReadOnlyList<WireOp> flat = Flatten(update.Layout);

        flat.Select(op => op.Name)
            .Take(5)
            .Should()
            .Equal("ObjectId", "SpriteId", "X", "Y", "Rotation");
    }

    [Fact]
    public void Expands_a_shared_sub_serializer_into_a_named_block()
    {
        EmulatorOutgoing update = Outgoing("ObjectUpdate");

        WireOp block = update.Layout.Single(op => op.Type == WireType.Block);

        block.Name.Should().Be("FloorItem");
        block.Children.Should().NotBeEmpty();
    }

    [Fact]
    public void Records_a_written_constant_rather_than_inventing_a_field_name()
    {
        EmulatorOutgoing update = Outgoing("ObjectUpdate");
        IReadOnlyList<WireOp> flat = Flatten(update.Layout);

        WireOp expiration = flat.Single(op => op.ConstantValue == "-1");

        expiration.Name.Should().BeNull();
        expiration.Comment.Should().Contain("expiration");
    }

    [Fact]
    public void Links_an_incoming_packet_to_the_handler_that_receives_it()
    {
        Incoming("MoveObject").HandlerType.Should().Be("MoveObjectMessageHandler");
    }

    [Fact]
    public void Traces_a_handler_through_the_service_into_the_grain()
    {
        EmulatorFlow flow = fixture.Scan.Flows.Single(f => f.MessageType == "MoveObjectMessage");

        flow.Steps.Should().Contain(s => s.Symbol == "MoveObjectMessageHandler.HandleAsync");
        flow.Steps.Should()
            .Contain(s => s.Symbol.EndsWith("MoveFloorItemInRoomAsync", StringComparison.Ordinal));
        flow.Steps.Should().Contain(s => s.Layer == "service");
        flow.PrimaryOperation.Should().Be("MoveFloorItemInRoomAsync");
        flow.IsOrchestrationOnly.Should().BeTrue();
    }

    [Fact]
    public void Records_the_composer_the_move_flow_sends_and_who_receives_it()
    {
        EmulatorFlow flow = fixture.Scan.Flows.Single(f => f.MessageType == "MoveObjectMessage");

        FeatureOutgoing update = flow.Outgoing.First(o => o.Packet == "ObjectUpdate");

        // RoomService.MoveFloorItemInRoomAsync sends the rejection straight back down the actor's
        // own session, which is what makes this an actor-recipient rather than a room broadcast.
        update.Recipient.Should().Be(Recipient.Actor);
        update.RecipientConfidence.Should().Be(Confidence.ImplementationObserved);
    }

    [Fact]
    public void Records_the_guards_on_the_path_verbatim()
    {
        EmulatorFlow flow = fixture.Scan.Flows.Single(f => f.MessageType == "MoveObjectMessage");

        flow.Checks.Should().NotBeEmpty();
        flow.Checks.Should()
            .Contain(c => c.Expression.Contains("ctx.PlayerId <= 0", StringComparison.Ordinal));
        flow.Checks.Select(c => c.OnFail)
            .Should()
            .OnlyContain(outcome =>
                outcome == "return" || outcome == "throw" || outcome == "send_and_return"
            );
    }

    [Fact]
    public void Every_evidence_reference_points_at_a_real_repository_path()
    {
        IEnumerable<EvidenceRef> evidence = fixture
            .Scan.Incoming.Select(i => i.ParserEvidence)
            .Concat(fixture.Scan.Outgoing.Select(o => o.SerializerEvidence))
            .Where(e => e is not null)
            .Select(e => e!);

        foreach (EvidenceRef reference in evidence.Take(50))
        {
            reference.Source.Should().StartWith("Vortex.");
            reference.Source.Should().NotContain("\\");
            reference.Id.Should().StartWith("ev_");
        }
    }

    [Fact]
    public void Evidence_ids_are_stable_for_the_same_reference()
    {
        EvidenceRef first = Incoming("MoveObject").ParserEvidence!;
        string second = EvidenceRef.BuildId(first.Kind, first.Origin, first.Source, first.Symbol);

        first.Id.Should().Be(second);
    }

    [Fact]
    public void Header_constants_with_no_parser_behind_them_are_reported()
    {
        // Not an assertion that the list is empty — it is not, and that is the point: a constant
        // nothing maps is a packet the emulator can never answer, and the scan surfaces it.
        fixture.Scan.UnmappedHeaderConstants.Should().NotBeNull();
        fixture.Scan.UnmappedHeaderConstants.Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public void The_vortex_registry_is_keyed_by_symbolic_name_not_by_id()
    {
        RevisionRegistry registry = fixture.Scan.Registry;

        registry.Origin.Should().Be("vortex");
        registry.Authority.Should().Be(EvidenceAuthority.VortexEmulator);
        registry.Incoming.Should().ContainKey("MoveObject");
        registry.Outgoing.Should().ContainKey("ObjectUpdate");
        registry.Incoming["MoveObject"].Should().BeGreaterThan(0);
    }

    private static IReadOnlyList<WireOp> Flatten(IReadOnlyList<WireOp> ops)
    {
        List<WireOp> flat = [];

        foreach (WireOp op in ops)
        {
            if (op.Children.Count > 0)
            {
                flat.AddRange(Flatten(op.Children));
            }
            else
            {
                flat.Add(op);
            }
        }

        return flat;
    }
}
