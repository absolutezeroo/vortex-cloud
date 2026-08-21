using System;
using System.IO;
using FluentAssertions;
using Vortex.Specs.Persistence;
using Vortex.Specs.Yaml;
using Xunit;

namespace Vortex.Specs.Tests.Persistence;

/// <summary>
/// The incremental-update contract: generated content is the tool's to rewrite, everything a person
/// wrote is theirs and survives untouched, and a hand edit inside the generated block stops the
/// regeneration instead of being reverted.
/// </summary>
public sealed class SpecStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "vortex-specs-store-tests",
        Guid.NewGuid().ToString("n")
    );

    private SpecStore Store => new(_root);

    private static YamlMapping Generated(string name, int fields = 4) =>
        YamlNode
            .Mapping()
            .Set("name", name)
            .Set("direction", "incoming")
            .Set("field_count", fields);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void A_first_write_creates_the_file_with_empty_hand_written_blocks()
    {
        SpecWriteResult result = Store.Write(
            "packets/MoveObject.yaml",
            "packet",
            Generated("MoveObject")
        );

        result.Outcome.Should().Be(SpecWriteOutcome.Created);

        YamlMapping document = Store.Read("packets/MoveObject.yaml")!;
        document.String("spec").Should().Be("packet");
        document.Mapping("generated")!.String("name").Should().Be("MoveObject");
        document.Mapping("verified")!.Entries.Should().BeEmpty();
        document.Mapping("manual")!.Entries.Should().BeEmpty();
        document.String("generated_digest").Should().StartWith("sha256:");
    }

    [Fact]
    public void Rewriting_the_same_content_touches_nothing()
    {
        Store.Write("p.yaml", "packet", Generated("MoveObject"));

        Store
            .Write("p.yaml", "packet", Generated("MoveObject"))
            .Outcome.Should()
            .Be(SpecWriteOutcome.Unchanged);
    }

    [Fact]
    public void A_changed_generated_block_is_replaced_and_hand_written_blocks_survive()
    {
        Store.Write("p.yaml", "packet", Generated("MoveObject"));

        string path = Path.Combine(_root, "p.yaml");
        string text = File.ReadAllText(path);
        File.WriteAllText(
            path,
            text.Replace(
                "verified: {}",
                "verified:\n  fields:\n  - index: 3\n    name: direction\nmanualnote: keep",
                StringComparison.Ordinal
            )
        );

        SpecWriteResult result = Store.Write(
            "p.yaml",
            "packet",
            Generated("MoveObject", fields: 5)
        );

        result.Outcome.Should().Be(SpecWriteOutcome.Updated);

        YamlMapping document = Store.Read("p.yaml")!;
        document.Mapping("generated")!.Int("field_count").Should().Be(5);

        YamlMapping verified = document.Mapping("verified")!;
        ((YamlMapping)verified.SequenceAt("fields")!.Items[0])
            .String("name")
            .Should()
            .Be("direction");
    }

    [Fact]
    public void A_hand_edit_inside_the_generated_block_blocks_the_overwrite()
    {
        Store.Write("p.yaml", "packet", Generated("MoveObject"));

        string path = Path.Combine(_root, "p.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path)
                .Replace("field_count: 4", "field_count: 99", StringComparison.Ordinal)
        );

        SpecWriteResult result = Store.Write("p.yaml", "packet", Generated("MoveObject"));

        result.Outcome.Should().Be(SpecWriteOutcome.Blocked);
        result.Detail.Should().Contain("edited by hand");

        // The edit is still there, and the regeneration is beside it for a person to reconcile.
        File.ReadAllText(path).Should().Contain("field_count: 99");
        File.Exists(path + ".regenerated.yaml").Should().BeTrue();
    }

    [Fact]
    public void Force_replaces_a_hand_edited_generated_block_but_still_keeps_verified()
    {
        Store.Write("p.yaml", "packet", Generated("MoveObject"));

        string path = Path.Combine(_root, "p.yaml");
        File.WriteAllText(
            path,
            File.ReadAllText(path)
                .Replace("field_count: 4", "field_count: 99", StringComparison.Ordinal)
                .Replace("verified: {}", "verified:\n  keep: me", StringComparison.Ordinal)
        );

        SpecWriteResult result = Store.Write(
            "p.yaml",
            "packet",
            Generated("MoveObject"),
            force: true
        );

        result.Outcome.Should().Be(SpecWriteOutcome.Updated);

        YamlMapping document = Store.Read("p.yaml")!;
        document.Mapping("generated")!.Int("field_count").Should().Be(4);
        document.Mapping("verified")!.String("keep").Should().Be("me");
    }

    [Fact]
    public void A_file_that_is_no_longer_readable_is_left_alone_rather_than_clobbered()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "p.yaml"), "\tthis is not the spec format\n");

        SpecWriteResult result = Store.Write("p.yaml", "packet", Generated("MoveObject"));

        result.Outcome.Should().Be(SpecWriteOutcome.Blocked);
        File.ReadAllText(Path.Combine(_root, "p.yaml")).Should().Contain("not the spec format");
    }

    [Fact]
    public void The_digest_is_of_the_rendered_block_so_reading_it_back_gives_the_same_answer()
    {
        Store.Write("p.yaml", "packet", Generated("MoveObject"));

        YamlMapping document = Store.Read("p.yaml")!;

        SpecStore.Digest(document["generated"]!).Should().Be(document.String("generated_digest"));
    }

    [Theory]
    [InlineData("MoveObject", "MoveObject")]
    [InlineData("room.move_floor_item", "room.move_floor_item")]
    [InlineData("as3:WIN63-2026", "as3_WIN63-2026")]
    [InlineData("a/b\\c", "a_b_c")]
    [InlineData("", "unnamed")]
    public void File_names_are_made_safe_without_losing_identity(string input, string expected)
    {
        SpecStore.FileName(input).Should().Be(expected);
    }

    [Fact]
    public void Two_names_differing_only_in_case_get_separate_files_and_a_reported_collision()
    {
        SpecPathAllocator allocator = new();

        string first = allocator.Allocate("packets", "handshake", "UniqueID.yaml");
        string second = allocator.Allocate("packets", "handshake", "UniqueId.yaml");

        second.Should().NotBe(first);
        allocator.Collisions.Should().ContainSingle();
        allocator.Collisions[0].Should().Contain("case-insensitive");
    }

    [Fact]
    public void The_same_path_asked_for_twice_is_not_a_collision()
    {
        SpecPathAllocator allocator = new();

        allocator.Allocate("a", "b.yaml").Should().Be(allocator.Allocate("a", "b.yaml"));
        allocator.Collisions.Should().BeEmpty();
    }
}
