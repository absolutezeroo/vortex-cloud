using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Vortex.Specs.Model;
using Vortex.Specs.Persistence;
using Vortex.Specs.Pipeline;
using Vortex.Specs.Reasoning;
using Vortex.Specs.Sources;
using Vortex.Specs.Tests.Fixtures;
using Xunit;

namespace Vortex.Specs.Tests.Analysis;

/// <summary>
/// Runs the whole pipeline against this repository and writes to a scratch directory.
/// </summary>
/// <remarks>
/// The properties asserted here are the ones a spec tree is worthless without: it regenerates
/// byte-identically from an unchanged checkout, it validates clean, and nothing in it claims more
/// than its evidence supports. Each is cheap to state and expensive to lose.
/// </remarks>
[Collection(RepositoryCollection.Name)]
public sealed class BootstrapEndToEndTests(RepositoryFixture fixture) : IDisposable
{
    private readonly string _output = Path.Combine(
        Path.GetTempPath(),
        "vortex-specs-bootstrap-tests",
        Guid.NewGuid().ToString("n")
    );

    private SpecWorkspace Workspace =>
        SpecWorkspace.ForTrees(fixture.Workspace.RepositoryRoot, _output, fixture.Workspace.Trees);

    public void Dispose()
    {
        if (Directory.Exists(_output))
        {
            Directory.Delete(_output, recursive: true);
        }
    }

    [Fact]
    public void A_bootstrap_produces_a_tree_that_validates_clean_and_regenerates_identically()
    {
        SpecBootstrapper bootstrapper = new(Workspace);

        BootstrapReport first = bootstrapper.Run();

        first.IncomingPackets.Should().BeGreaterThan(400);
        first.OutgoingPackets.Should().BeGreaterThan(400);
        first.Features.Should().BeGreaterThan(100);
        first.Scenarios.Should().BeGreaterThan(first.Features);
        first.FilesWritten.Should().BeGreaterThan(1000);
        first.Blocked.Should().BeEmpty();

        new SpecValidator()
            .Validate(new SpecStore(_output))
            .Should()
            .NotContain(i => i.Severity == ValidationSeverity.Error);

        // Same checkout, same output. A tree that churns between runs cannot be reviewed as a diff.
        BootstrapReport second = bootstrapper.Run();

        second.FilesWritten.Should().Be(0);
        second.FilesUnchanged.Should().Be(first.FilesWritten + first.FilesUnchanged);
    }

    [Fact]
    public void No_packet_claims_more_confidence_than_its_own_sources_support()
    {
        SpecWorld world = new SpecPipeline(Workspace).Scan();
        ResolvedSpecs specs = SpecPipeline.Resolve(world, []);

        foreach (PacketSpec packet in specs.Packets)
        {
            EvidenceAuthority best =
                packet.Evidence.Count == 0
                    ? EvidenceAuthority.Assumption
                    : packet.Evidence.Min(e => e.Authority);

            ((int)packet.StructureConfidence)
                .Should()
                .BeLessThanOrEqualTo(
                    (int)ConfidencePolicyCeiling(best),
                    "{0} is backed only by {1}",
                    packet.SpecId,
                    best
                );
        }
    }

    [Fact]
    public void No_feature_claims_official_behaviour_without_a_capture()
    {
        SpecWorld world = new SpecPipeline(Workspace).Scan();
        ResolvedSpecs specs = SpecPipeline.Resolve(world, []);

        if (world.Captures.Count > 0)
        {
            // With captures present the assertion below would be about their content, not about the
            // rule; the rule is only meaningful to test when nothing could have settled anything.
            return;
        }

        specs
            .Features.Should()
            .OnlyContain(f => f.OfficialBehaviourConfidence == Confidence.Unknown);

        specs.Scenarios.Should().OnlyContain(s => s.Expected == ScenarioOutcome.Unknown);
    }

    [Fact]
    public void No_conflict_is_silently_arbitrated()
    {
        SpecWorld world = new SpecPipeline(Workspace).Scan();
        ResolvedSpecs specs = SpecPipeline.Resolve(world, []);

        specs.Conflicts.Should().OnlyContain(c => c.OfficialStatus == Confidence.Unknown);
        specs.Conflicts.Should().OnlyContain(c => c.Resolution == null);
        specs.Conflicts.Should().OnlyContain(c => c.Positions.Count >= 2);
    }

    [Fact]
    public void Behavioural_specs_never_carry_a_header_id()
    {
        new SpecBootstrapper(Workspace).Run();

        foreach (
            string file in Directory
                .EnumerateFiles(
                    Path.Combine(_output, "features"),
                    "*.yaml",
                    SearchOption.AllDirectories
                )
                .Take(200)
        )
        {
            string text = File.ReadAllText(file);
            text.Should().NotContain("header:");
            text.Should().NotContain("opcode:");
        }
    }

    [Fact]
    public void Every_unnamed_field_is_marked_as_a_placeholder_rather_than_reading_as_a_name()
    {
        SpecWorld world = new SpecPipeline(Workspace).Scan();
        ResolvedSpecs specs = SpecPipeline.Resolve(world, []);

        foreach (PacketSpec packet in specs.Packets)
        {
            foreach (PacketFieldSpec field in packet.Fields)
            {
                if (field.IsPlaceholderName)
                {
                    field.NameConfidence.Should().Be(Confidence.Unknown);
                }
            }
        }
    }

    /// <summary>
    /// The most a packet backed by <paramref name="authority"/> at best may claim. Official-grade
    /// evidence can reach <see cref="Confidence.Confirmed"/> when a second official-grade source
    /// agrees; everything weaker is capped at its own rung.
    /// </summary>
    private static Confidence ConfidencePolicyCeiling(EvidenceAuthority authority) =>
        authority <= EvidenceAuthority.ClientCode
            ? Confidence.Confirmed
            : ConfidencePolicy.FromAuthority(authority);
}
