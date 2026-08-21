using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Vortex.Specs.Model;
using Vortex.Specs.Persistence;
using Vortex.Specs.Yaml;
using Xunit;

namespace Vortex.Specs.Tests.Persistence;

public sealed class SpecValidatorTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "vortex-specs-validator-tests",
        Guid.NewGuid().ToString("n")
    );

    private SpecStore Store => new(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private IReadOnlyList<ValidationIssue> Validate() => new SpecValidator().Validate(Store);

    private static YamlMapping EvidenceBlock(string id, string authority) =>
        YamlNode
            .Mapping()
            .Set("id", id)
            .Set("kind", "emulator_parser")
            .Set("authority", authority)
            .Set("origin", "vortex")
            .Set("source", "Vortex.Revisions/X.cs");

    [Fact]
    public void A_well_formed_spec_produces_nothing()
    {
        Store.Write(
            "p.yaml",
            "packet",
            YamlNode
                .Mapping()
                .Set("name", "MoveObject")
                .Set("structure_confidence", "implementation_observed")
                .Set(
                    "evidence",
                    YamlNode.Sequence([EvidenceBlock("ev_aabbccddeeff", "vortex_emulator")])
                )
        );

        Validate().Should().BeEmpty();
    }

    [Fact]
    public void A_confidence_that_outranks_its_evidence_is_an_error()
    {
        // The whole ladder depends on this: a "confirmed" sitting next to reference-emulator evidence
        // is a claim nothing backs, and it is the most dangerous shape a spec file can take.
        Store.Write(
            "p.yaml",
            "packet",
            YamlNode
                .Mapping()
                .Set("name", "MoveObject")
                .Set(
                    "evidence",
                    YamlNode.Sequence([
                        EvidenceBlock("ev_aabbccddeeff", "reference_emulator")
                            .Set("confidence", "confirmed"),
                    ])
                )
        );

        IReadOnlyList<ValidationIssue> issues = Validate();

        issues.Should().Contain(i => i.Severity == ValidationSeverity.Error);
        issues.Should().Contain(i => i.Message.Contains("outranks", StringComparison.Ordinal));
    }

    [Fact]
    public void A_confidence_level_nobody_defined_is_an_error()
    {
        Store.Write(
            "p.yaml",
            "packet",
            YamlNode.Mapping().Set("name", "X").Set("structure_confidence", "pretty_sure")
        );

        Validate()
            .Should()
            .Contain(i =>
                i.Severity == ValidationSeverity.Error
                && i.Message.Contains("pretty_sure", StringComparison.Ordinal)
            );
    }

    [Fact]
    public void A_header_id_inside_a_behavioural_spec_is_an_error()
    {
        // Ids are per-revision. One in a feature spec ties the behaviour to a single client build
        // without saying so, which is exactly what the separate revision registries prevent.
        Store.Write(
            "f.yaml",
            "feature",
            YamlNode.Mapping().Set("id", "room.move").Set("header", 1482)
        );

        Validate()
            .Should()
            .Contain(i =>
                i.Severity == ValidationSeverity.Error
                && i.Message.Contains("header id", StringComparison.Ordinal)
            );
    }

    [Fact]
    public void A_revision_registry_may_of_course_contain_ids()
    {
        Store.Write(
            "revisions/r.yaml",
            "revision",
            YamlNode
                .Mapping()
                .Set("revision", "WIN63")
                .Set("incoming", YamlNode.Mapping().Set("MoveObject", 1482))
        );

        Validate().Should().BeEmpty();
    }

    [Fact]
    public void An_evidence_id_that_points_at_nothing_is_a_warning()
    {
        Store.Write(
            "p.yaml",
            "packet",
            YamlNode.Mapping().Set("name", "X").Set("evidence", YamlNode.Scalar("ev_deadbeef0000"))
        );

        Validate()
            .Should()
            .Contain(i =>
                i.Severity == ValidationSeverity.Warning
                && i.Message.Contains("ev_deadbeef0000", StringComparison.Ordinal)
            );
    }

    [Fact]
    public void A_hand_edited_generated_block_is_reported_so_the_blocked_write_is_not_a_surprise()
    {
        Store.Write("p.yaml", "packet", YamlNode.Mapping().Set("name", "MoveObject"));

        string path = Path.Combine(_root, "p.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path).Replace("MoveObject", "MoveThing", StringComparison.Ordinal)
        );

        Validate()
            .Should()
            .Contain(i =>
                i.Severity == ValidationSeverity.Warning
                && i.Message.Contains("hand-edited", StringComparison.Ordinal)
            );
    }

    [Fact]
    public void A_spec_from_a_newer_format_version_is_an_error_rather_than_read_wrongly()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(
            Path.Combine(_root, "future.yaml"),
            $"spec: packet\nspec_version: {SpecConstants.SpecFormatVersion + 5}\n"
                + "generated_digest: \"sha256:0\"\ngenerated:\n  name: X\n"
        );

        Validate()
            .Should()
            .Contain(i =>
                i.Severity == ValidationSeverity.Error
                && i.Message.Contains("newer format", StringComparison.Ordinal)
            );
    }

    [Fact]
    public void Issues_come_back_errors_first_and_in_a_stable_order()
    {
        Store.Write("a.yaml", "packet", YamlNode.Mapping().Set("structure_confidence", "nope"));
        Store.Write(
            "b.yaml",
            "packet",
            YamlNode.Mapping().Set("evidence", YamlNode.Scalar("ev_000000000000"))
        );

        IReadOnlyList<ValidationIssue> first = Validate();
        IReadOnlyList<ValidationIssue> second = Validate();

        first.Select(i => i.Message).Should().Equal(second.Select(i => i.Message));
        first[0].Severity.Should().Be(ValidationSeverity.Error);
    }
}
