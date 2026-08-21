using FluentAssertions;
using Vortex.Specs.Naming;
using Xunit;

namespace Vortex.Specs.Tests.Reasoning;

/// <summary>
/// Every cross-source comparison hangs off canonicalisation, so the cases here are real type names
/// taken from all four trees rather than invented examples.
/// </summary>
public class PacketNamingTests
{
    [Theory]
    // Vortex
    [InlineData("MoveObjectMessage", "MoveObject")]
    [InlineData("MoveObjectMessageParser", "MoveObject")]
    [InlineData("MoveObjectMessageHandler", "MoveObject")]
    [InlineData("ObjectUpdateMessageComposer", "ObjectUpdate")]
    [InlineData("ObjectUpdateMessageComposerSerializer", "ObjectUpdate")]
    [InlineData("MoveObjectMessageEvent", "MoveObject")]
    // Nitro
    [InlineData("MoveObjectComposer", "MoveObject")]
    [InlineData("ObjectUpdateMessage", "ObjectUpdate")]
    [InlineData("YouAreControllerMessage", "YouAreController")]
    // Arcturus
    [InlineData("RoomHeightMapMessageComposer", "RoomHeightMap")]
    // Official client, unobfuscated survivors
    [InlineData("GetNftCreditsMessageComposer", "GetNftCredits")]
    [InlineData("PhotoCompetitionMessageComposer", "PhotoCompetition")]
    public void Real_names_from_every_tree_reduce_to_the_same_symbol(
        string typeName,
        string expected
    )
    {
        PacketNaming.Canonical(typeName).Should().Be(expected);
    }

    [Fact]
    public void Only_one_suffix_comes_off_so_a_two_word_tail_is_not_mangled()
    {
        // Stripping in a loop would take "Message" and then "Event" and land on "Room", colliding
        // with an unrelated packet.
        PacketNaming.Canonical("RoomEventMessage").Should().Be("RoomEvent");
    }

    [Fact]
    public void A_name_with_no_suffix_is_left_alone()
    {
        PacketNaming.Canonical("Ping").Should().Be("Ping");
        PacketNaming.Canonical("Event").Should().Be("Event");
    }

    [Theory]
    [InlineData("ObjectId", "object_id")]
    [InlineData("X", "x")]
    [InlineData("StackHeight", "stack_height")]
    [InlineData("objectId", "object_id")]
    [InlineData("roomId", "room_id")]
    [InlineData("NFTAssetId", "nft_asset_id")]
    [InlineData("already_snake", "already_snake")]
    [InlineData("", "")]
    public void Snake_case_handles_both_conventions_and_acronyms(string input, string expected)
    {
        PacketNaming.SnakeCase(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("_SafeCls_3667")]
    [InlineData("_SafeStr_4841")]
    [InlineData("class_165")]
    [InlineData("_Str_223")]
    public void Decompiler_and_obfuscator_names_are_recognised_as_meaningless(string name)
    {
        PacketNaming.IsSyntheticTypeName(name).Should().BeTrue();
    }

    [Theory]
    [InlineData("MoveObjectComposer")]
    [InlineData("GetNftCreditsMessageComposer")]
    [InlineData("class_of_service")]
    public void Real_names_are_not_mistaken_for_synthetic_ones(string name)
    {
        PacketNaming.IsSyntheticTypeName(name).Should().BeFalse();
    }

    [Theory]
    [InlineData(
        "Vortex.Revisions/Revision20260701/Parsers/Room/Engine/MoveObjectMessageParser.cs",
        "room"
    )]
    [InlineData("Vortex.Revisions/Revision20260701/Serializers/Catalog/X.cs", "catalog")]
    [InlineData(
        "../c/sources/NITRO/packages/nitro-shared/src/packets/outgoing/Room/Engine/MoveObjectComposer.ts",
        "room"
    )]
    [InlineData(
        "../c/sources/HABBO/src/main/java/com/eu/habbo/messages/incoming/rooms/items/MoveObjectMessageEvent.java",
        "room"
    )]
    [InlineData(
        "../c/sources/WIN63/src/com/sulake/habbo/communication/messages/outgoing/camera/X.as",
        "camera"
    )]
    [InlineData("../c/sources/WIN63/src/unknowns/_SafePkg_2136/_SafeCls_3667.as", "unsorted")]
    public void The_domain_comes_from_each_tree_s_own_folders(string path, string expected)
    {
        PacketNaming.DomainFromSourcePath(path).Should().Be(expected);
    }

    [Fact]
    public void A_missing_path_is_unsorted_rather_than_a_guess()
    {
        PacketNaming.DomainFromSourcePath(null).Should().Be("unsorted");
    }
}
