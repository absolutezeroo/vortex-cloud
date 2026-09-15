using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Protocol.Messages.Incoming.Inventory.Pets;
using Vortex.Protocol.Messages.Incoming.Marketplace;
using Vortex.Protocol.Messages.Incoming.Room.Pets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Room;

/// <summary>
///     Three parsers that read fewer values than the client's composer is handed. The field-count
///     check could not see any of them: these AS3 composers build their payload in a way the spec
///     scanner cannot follow, so the client layout is recorded as "0 fields, partial" and
///     <c>ConflictDetector</c> excludes partial layouts on purpose. The four CallForHelp variants
///     with the same defect are covered in <c>Help/CallForHelpVariantWireTests</c>.
///
///     Every id below is cross-checked against the client's own registry
///     (<c>communication/_SafeCls_2046.as</c>), never against the header table's comments.
/// </summary>
public sealed class ShortReadWireTests
{
    private const int BreedPetsMessageEvent = 1922; // _composers[1922] = _SafeCls_2980
    private const int ConfirmPetBreedingEvent = 2872; // _composers[2872] = _SafeCls_3418
    private const int GetMarketplaceOffersMessageEvent = 2731; // _composers[2731] = _SafeCls_1953

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket BuildClientPacket(int header, Action<ServerPacket> write)
    {
        ServerPacket sp = new(header);
        write(sp);
        return new ClientPacket(header, sp.ToArray());
    }

    private static T Parse<T>(int header, Action<ServerPacket> write)
        where T : class =>
        Revision
            .Parsers[header]
            .Parse(BuildClientPacket(header, write))
            .Should()
            .BeOfType<T>()
            .Subject;

    /// <summary>
    ///     All three breeding buttons send this packet and differ only by the leading action code
    ///     (AvatarInfoWidget.as:1737-1764). Reading only two ints made PetOneId the action code and
    ///     PetTwoId the first pet, so the lookup was for a pet with id 0, 1 or 2 and always failed.
    /// </summary>
    [Fact]
    public void BreedPetsParser_ReadsTheActionCodeBeforeBothPetIds()
    {
        BreedPetsMessage message = Parse<BreedPetsMessage>(
            BreedPetsMessageEvent,
            sp => sp.WriteInteger(2).WriteInteger(4271).WriteInteger(9182)
        );

        message.Action.Should().Be(PetBreedingActionType.Accept);
        message.PetOneId.Should().Be(4271);
        message.PetTwoId.Should().Be(9182);
    }

    /// <summary>
    ///     <c>confirmPetBreeding(stuffId, name, petOne, petTwo)</c>. The single int we used to read
    ///     was the nest's furniture id, taken for a pet id.
    /// </summary>
    [Fact]
    public void ConfirmPetBreedingParser_ReadsTheNestNameAndBothParents()
    {
        ConfirmPetBreedingMessage message = Parse<ConfirmPetBreedingMessage>(
            ConfirmPetBreedingEvent,
            sp => sp.WriteInteger(55).WriteString("Pixel").WriteInteger(4271).WriteInteger(9182)
        );

        message.NestStuffId.Should().Be(55);
        message.PetName.Should().Be("Pixel");
        message.PetOneId.Should().Be(4271);
        message.PetTwoId.Should().Be(9182);
    }

    /// <summary>Fifth field, `_combineUniques` (MarketPlaceLogic.as:166), never read.</summary>
    [Fact]
    public void GetMarketplaceOffersParser_ReadsTheTrailingCombineUniquesFlag()
    {
        GetMarketplaceOffersMessage message = Parse<GetMarketplaceOffersMessage>(
            GetMarketplaceOffersMessageEvent,
            sp =>
                sp.WriteInteger(10)
                    .WriteInteger(500)
                    .WriteString("throne")
                    .WriteInteger(1)
                    .WriteBoolean(false)
        );

        message.MinPrice.Should().Be(10);
        message.MaxPrice.Should().Be(500);
        message.SearchQuery.Should().Be("throne");
        message.SortOrder.Should().Be(1);
        message.CombineUniques.Should().BeFalse();
    }
}
