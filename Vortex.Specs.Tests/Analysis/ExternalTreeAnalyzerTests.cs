using System;
using System.Linq;
using FluentAssertions;
using Vortex.Specs.Analysis.Client;
using Vortex.Specs.Analysis.Reference;
using Vortex.Specs.Model;
using Vortex.Specs.Sources;
using Vortex.Specs.Tests.Fixtures;
using Xunit;

namespace Vortex.Specs.Tests.Analysis;

/// <summary>
/// Exercises the client and reference readers against the real sibling checkouts.
/// </summary>
/// <remarks>
/// Skipped when those checkouts are absent rather than failed: they are other people's repositories
/// and are not part of this one, so a machine without them is a normal machine. What must never
/// happen is a reader that silently returns nothing being mistaken for a machine without the tree,
/// which is why every assertion here is about substance and not merely about not throwing.
/// </remarks>
[Collection(RepositoryCollection.Name)]
public class ExternalTreeAnalyzerTests(RepositoryFixture fixture)
{
    private SourceTree? Tree(Func<SourceTree, bool> predicate) =>
        fixture.Workspace.Trees.FirstOrDefault(predicate);

    private SourceTree? NitroTree => Tree(t => t.Id == "nitro");

    /// <summary>
    /// Selected by kind, not by name. Reference trees are named after their directory now — the one
    /// present is <c>habbo-arcturus-daybreak</c> — and a hardcoded "arcturus" quietly turned every
    /// assertion below into an early return the day that changed.
    /// </summary>
    private SourceTree? ArcturusTree => Tree(t => t.Kind == SourceTreeKind.ReferenceEmulator);

    private SourceTree? TargetClientTree =>
        Tree(t =>
            t.Kind == SourceTreeKind.OfficialClient
            && t.Revision is not null
            && t.Revision.StartsWith("WIN63-2026070", StringComparison.Ordinal)
        );

    [Fact]
    public void Nitro_yields_named_typed_fields_for_a_client_to_server_packet()
    {
        if (NitroTree is null)
        {
            return;
        }

        ClientScan scan = new NitroClientAnalyzer(fixture.Workspace, NitroTree).Scan();

        scan.Packets.Should().HaveCountGreaterThan(400);

        ClientPacket move = scan.Packets.Single(p =>
            p.Canonical == "MoveObject" && p.Direction == PacketDirection.Incoming
        );

        move.Fields.Select(f => (f.Name, f.Type))
            .Should()
            .Equal(
                ("object_id", WireType.Int32),
                ("x", WireType.Int32),
                ("y", WireType.Int32),
                ("rotation", WireType.Int32)
            );
    }

    [Fact]
    public void Nitro_expands_a_shared_field_block_when_parsing_a_server_to_client_packet()
    {
        if (NitroTree is null)
        {
            return;
        }

        ClientScan scan = new NitroClientAnalyzer(fixture.Workspace, NitroTree).Scan();

        ClientPacket update = scan.Packets.Single(p =>
            p.Canonical == "ObjectUpdate" && p.Direction == PacketDirection.Outgoing
        );

        ClientField block = update.Fields.Single(f => f.Type == WireType.Block);
        block.Children.Select(c => c.Name).Should().StartWith(["object_id", "sprite_id", "x", "y"]);
    }

    [Fact]
    public void Nitro_is_flagged_as_a_different_revision_so_its_ids_are_never_compared()
    {
        if (NitroTree is null)
        {
            return;
        }

        ClientScan scan = new NitroClientAnalyzer(fixture.Workspace, NitroTree).Scan();

        scan.TargetsSameRevision.Should().BeFalse();
        scan.Authority.Should().Be(EvidenceAuthority.MultiImplementation);
        scan.IncomingHeaders.Should().ContainKey("MoveObject");
    }

    [Fact]
    public void The_official_client_registry_binds_header_ids_to_classes_in_both_directions()
    {
        if (TargetClientTree is null)
        {
            return;
        }

        ClientScan scan = new As3ClientAnalyzer(
            fixture.Workspace,
            TargetClientTree,
            fixture.Scan.Revision
        ).Scan();

        scan.Authority.Should().Be(EvidenceAuthority.ClientCode);
        scan.TargetsSameRevision.Should().BeTrue();

        scan.Packets.Where(p => p.Direction == PacketDirection.Incoming)
            .Should()
            .HaveCountGreaterThan(300);
        scan.Packets.Where(p => p.Direction == PacketDirection.Outgoing)
            .Should()
            .HaveCountGreaterThan(300);

        // The registry is what makes an obfuscated class usable: without a header id it cannot be
        // joined to a symbolic name at all.
        scan.Packets.Count(p => p.HeaderId is not null).Should().BeGreaterThan(400);
    }

    [Fact]
    public void The_official_client_recovers_real_field_names_from_surviving_getters()
    {
        if (TargetClientTree is null)
        {
            return;
        }

        ClientScan scan = new As3ClientAnalyzer(
            fixture.Workspace,
            TargetClientTree,
            fixture.Scan.Revision
        ).Scan();

        int named = scan
            .Packets.Where(p => p.Direction == PacketDirection.Outgoing)
            .SelectMany(p => p.Fields)
            .Count(f => f.Name is not null);

        named.Should().BeGreaterThan(200);

        // And never the obfuscator's own identifiers.
        scan.Packets.SelectMany(p => p.Fields)
            .Select(f => f.Name)
            .Where(n => n is not null)
            .Should()
            .OnlyContain(n =>
                !n!.Contains("safe_str", StringComparison.Ordinal)
                && !n.StartsWith("param", StringComparison.Ordinal)
            );
    }

    [Fact]
    public void A_composer_the_client_actually_constructs_is_stronger_evidence_than_one_it_merely_declares()
    {
        if (TargetClientTree is null)
        {
            return;
        }

        ClientScan scan = new As3ClientAnalyzer(
            fixture.Workspace,
            TargetClientTree,
            fixture.Scan.Revision
        ).Scan();

        ClientPacket move = scan.Packets.Single(p =>
            p.Direction == PacketDirection.Incoming
            && p.HeaderId == fixture.Scan.Registry.Incoming["MoveObject"]
        );

        // The client is written to send this, so the server has to support it — a stronger claim
        // than "the class is present in the build", which a withdrawn feature would also satisfy.
        move.Evidence.Authority.Should().Be(EvidenceAuthority.ClientMandated);
        move.Evidence.Kind.Should().Be(EvidenceKind.ClientCallSite);
        move.Evidence.Note.Should().Contain("call site");

        // Composers nothing constructs stay at the weaker rung rather than being promoted wholesale.
        scan.Packets.Should()
            .Contain(p =>
                p.Direction == PacketDirection.Incoming
                && p.Evidence.Authority == EvidenceAuthority.ClientCode
            );
    }

    [Fact]
    public void Arcturus_yields_a_field_layout_and_the_composers_a_handler_answers_with()
    {
        if (ArcturusTree is null)
        {
            return;
        }

        ReferenceScan scan = new ArcturusReferenceAnalyzer(fixture.Workspace, ArcturusTree).Scan();

        scan.Behaviours.Should().HaveCountGreaterThan(300);
        scan.Authority.Should().Be(EvidenceAuthority.ReferenceEmulator);

        ReferenceBehaviour move = scan.Behaviours.Single(b => b.Canonical == "MoveObject");

        move.Fields.Select(f => f.Type)
            .Should()
            .Equal(WireType.Int32, WireType.Int32, WireType.Int32, WireType.Int32);
        move.Fields.Select(f => f.Name).Should().Equal("furni_id", "x", "y", "rotation");

        move.Outgoing.Select(o => o.Packet).Should().Contain("ObjectUpdate");
        move.Outgoing.Should().OnlyContain(o => o.Recipient == Recipient.Actor);
        move.Outgoing.Select(o => o.Order).Should().BeInAscendingOrder();
    }

    [Fact]
    public void Arcturus_header_tables_are_read_but_belong_to_its_own_revision()
    {
        if (ArcturusTree is null)
        {
            return;
        }

        ReferenceScan scan = new ArcturusReferenceAnalyzer(fixture.Workspace, ArcturusTree).Scan();

        scan.IncomingHeaders.Should().ContainKey("MoveObject");
        scan.OutgoingHeaders.Should().ContainKey("ObjectUpdate");

        // Different build, different numbers. Recorded so the difference is visible, never compared.
        scan.IncomingHeaders["MoveObject"]
            .Should()
            .NotBe(fixture.Scan.Registry.Incoming["MoveObject"]);
    }

    [Fact]
    public void A_composer_that_delegates_to_a_helper_is_reported_as_partial()
    {
        if (ArcturusTree is null)
        {
            return;
        }

        ReferenceScan scan = new ArcturusReferenceAnalyzer(fixture.Workspace, ArcturusTree).Scan();

        ReferenceComposerLayout update = scan.Composers.Single(c => c.Canonical == "ObjectUpdate");

        // ObjectUpdateMessageComposer calls item.serializeFloorData, whose bytes this reader does not
        // follow. Saying so is what stops "Arcturus writes 4 fields" being read as a disagreement.
        update.IsPartial.Should().BeTrue();
    }
}
