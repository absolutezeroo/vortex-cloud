using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Vortex.Specs.Captures;
using Vortex.Specs.Diff;
using Vortex.Specs.Model;
using Vortex.Specs.Tests.Fixtures;
using Xunit;

namespace Vortex.Specs.Tests.Diff;

public class TraceDifferTests
{
    private static PacketTrace Trace(string origin, params string[] packets) =>
        new()
        {
            Origin = origin,
            Trigger = "MoveObject",
            Emitted = [.. packets.Select(p => new TracedPacket { Name = p })],
        };

    [Fact]
    public void Identical_traces_differ_in_nothing()
    {
        new TraceDiffer()
            .Compare(Trace("a", "RoomReady", "Objects"), Trace("b", "RoomReady", "Objects"))
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void A_packet_the_emulator_never_sends_is_reported_missing()
    {
        IReadOnlyList<TraceDifference> differences = new TraceDiffer().Compare(
            Trace("reference", "RoomReady", "FloorHeightMap", "Objects"),
            Trace("vortex", "RoomReady", "Objects")
        );

        differences.Should().ContainSingle();
        differences[0].Kind.Should().Be(TraceDifferenceKind.Missing);
        differences[0].Packet.Should().Be("FloorHeightMap");
    }

    [Fact]
    public void A_packet_only_the_emulator_sends_is_reported_extra()
    {
        IReadOnlyList<TraceDifference> differences = new TraceDiffer().Compare(
            Trace("reference", "RoomReady"),
            Trace("vortex", "RoomReady", "Chatty")
        );

        differences.Should().ContainSingle();
        differences[0].Kind.Should().Be(TraceDifferenceKind.Extra);
        differences[0].Packet.Should().Be("Chatty");
    }

    [Fact]
    public void The_same_packets_in_a_different_order_are_reported_as_reordered()
    {
        IReadOnlyList<TraceDifference> differences = new TraceDiffer().Compare(
            Trace("reference", "RoomReady", "Objects", "Items", "Users"),
            Trace("vortex", "RoomReady", "Users", "Objects", "Items")
        );

        differences.Should().NotBeEmpty();
        differences.Should().OnlyContain(d => d.Kind == TraceDifferenceKind.Reordered);
    }

    [Fact]
    public void One_missing_packet_at_the_front_does_not_cascade_into_every_later_packet()
    {
        // Alignment by longest common subsequence rather than by position. Comparing index by index
        // would report four differences here instead of the one that is real.
        IReadOnlyList<TraceDifference> differences = new TraceDiffer().Compare(
            Trace("reference", "RoomReady", "A", "B", "C", "D"),
            Trace("vortex", "A", "B", "C", "D")
        );

        differences.Should().ContainSingle();
        differences[0].Packet.Should().Be("RoomReady");
    }

    [Fact]
    public void The_same_packet_to_the_wrong_audience_is_a_difference()
    {
        PacketTrace reference = new()
        {
            Origin = "reference",
            Trigger = "MoveObject",
            Emitted = [new TracedPacket { Name = "ObjectUpdate", Recipient = Recipient.RoomUsers }],
        };
        PacketTrace actual = new()
        {
            Origin = "vortex",
            Trigger = "MoveObject",
            Emitted = [new TracedPacket { Name = "ObjectUpdate", Recipient = Recipient.Actor }],
        };

        IReadOnlyList<TraceDifference> differences = new TraceDiffer().Compare(reference, actual);

        differences.Should().ContainSingle();
        differences[0].Kind.Should().Be(TraceDifferenceKind.RecipientMismatch);
        differences[0].Detail.Should().Contain("room_users").And.Contain("actor");
    }

    [Fact]
    public void An_unknown_recipient_on_either_side_is_not_a_disagreement()
    {
        PacketTrace reference = new()
        {
            Origin = "reference",
            Trigger = "MoveObject",
            Emitted = [new TracedPacket { Name = "ObjectUpdate", Recipient = Recipient.RoomUsers }],
        };
        PacketTrace actual = new()
        {
            Origin = "vortex",
            Trigger = "MoveObject",
            Emitted = [new TracedPacket { Name = "ObjectUpdate" }],
        };

        new TraceDiffer().Compare(reference, actual).Should().BeEmpty();
    }

    [Fact]
    public void A_field_that_differs_is_reported_with_both_values()
    {
        PacketTrace reference = new()
        {
            Origin = "reference",
            Trigger = "MoveObject",
            Emitted =
            [
                new TracedPacket
                {
                    Name = "ObjectUpdate",
                    Fields = new Dictionary<string, string> { ["x"] = "7" },
                },
            ],
        };
        PacketTrace actual = new()
        {
            Origin = "vortex",
            Trigger = "MoveObject",
            Emitted =
            [
                new TracedPacket
                {
                    Name = "ObjectUpdate",
                    Fields = new Dictionary<string, string> { ["x"] = "3" },
                },
            ],
        };

        IReadOnlyList<TraceDifference> differences = new TraceDiffer().Compare(reference, actual);

        differences.Should().ContainSingle();
        differences[0].Kind.Should().Be(TraceDifferenceKind.FieldMismatch);
        differences[0].Detail.Should().Be("x: expected 7, got 3");
    }

    [Fact]
    public void Two_captures_of_the_same_action_diff_end_to_end()
    {
        using TemporaryCapture official = new("a.json", TemporaryCapture.MoveFurnitureOfficial);
        using TemporaryCapture mine = new("b.json", TemporaryCapture.MoveFurnitureEmulator);

        CaptureImporter importer = new();
        CaptureDocument left = importer.Read(official.Path_);
        CaptureDocument right = importer.Read(mine.Path_);

        IReadOnlyList<PacketTrace> reference = PacketTrace.FromCapture(
            left,
            importer.Observe(left, null)
        );
        IReadOnlyList<PacketTrace> actual = PacketTrace.FromCapture(
            right,
            importer.Observe(right, null)
        );

        // The second trigger is the interesting one: the official server answers with a dialog and
        // an update, this emulator answers with the update alone.
        IReadOnlyList<TraceDifference> differences = new TraceDiffer().Compare(
            reference[1],
            actual[1]
        );

        differences
            .Should()
            .Contain(d =>
                d.Kind == TraceDifferenceKind.Missing && d.Packet == "NotificationDialog"
            );
    }

    [Fact]
    public void The_rendered_diff_reads_like_a_unified_diff()
    {
        string rendered = TraceDiffer.Render(
            Trace("reference", "RoomReady", "Objects", "Items"),
            Trace("vortex", "RoomReady"),
            new TraceDiffer().Compare(
                Trace("reference", "RoomReady", "Objects", "Items"),
                Trace("vortex", "RoomReady")
            )
        );

        rendered.Should().Contain("--- reference");
        rendered.Should().Contain("+++ vortex");
        rendered.Should().Contain("-Objects");
        rendered.Should().Contain("-Items");
        rendered.Should().Contain(" RoomReady");
    }
}
