using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.UserDefinedRoomEvents;

/// <summary>
///     A contract's terms are read by position, so one field written in the wrong place — or under
///     the wrong condition — takes the whole message with it, and a trade screen reading past its
///     own terms shows the player something nobody offered.
/// </summary>
/// <remarks>
///     The reads below walk the payload in the order the client's parsers do
///     (<c>TradeRequirement</c> -> <c>TradeRequirementRules</c> -> <c>TradeRequirementRulesDefinition</c>
///     -> <c>TradeRequirementRule</c> -> <c>TradeRequirementNode</c> -> <c>ChestItemType</c>), so
///     the assertions are the client's own reading rather than a restatement of the serializer.
/// </remarks>
public sealed class WiredTradeContractTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    /// <summary>What a deposit sends: any tradeable furni, and no rules at all.</summary>
    private const int AnyFurniType = 2;

    /// <summary>The one type whose payload is followed by a rules block.</summary>
    private const int CustomType = 4;

    [Fact]
    public void ACustomContract_ReadsBackTermForTerm()
    {
        TradeContract contract = new()
        {
            YouGiveRules =
            [
                new TradeContractRule
                {
                    Nodes =
                    [
                        new TradeContractNode { IsFurni = false, Amount = 50 },
                        new TradeContractNode
                        {
                            IsFurni = true,
                            Amount = 3,
                            ItemType = new TradeContractItemType
                            {
                                IsWallItem = false,
                                SpriteId = 1234,
                                LegacyPosterId = string.Empty,
                            },
                        },
                    ],
                },
            ],
            YouGetRule = new TradeContractRule
            {
                Nodes = [new TradeContractNode { IsFurni = false, Amount = 10 }],
            },
            Mode = 1,
            Multiplier = 5,
        };

        ClientPacket packet = Serialize(CustomType, contract);

        packet.PopInt().Should().Be(CustomType);
        packet.PopString().Should().BeEmpty("youGetText");
        packet
            .PopString()
            .Should()
            .Be("generic", "an empty layoutType would name an asset that does not exist");

        // --- the rules definition
        packet.PopBoolean().Should().BeTrue("the give side is announced by a flag");
        packet.PopInt().Should().Be(1, "one alternative");
        packet.PopInt().Should().Be(2, "two terms in it");

        packet.PopByte().Should().Be(0, "a coin term is 0");
        packet.PopInt().Should().Be(50);

        packet.PopByte().Should().Be(1, "a furni term is 1");
        packet.PopInt().Should().Be(3);
        packet.PopBoolean().Should().BeFalse("isWallItem");
        packet.PopInt().Should().Be(1234);
        packet.PopString().Should().BeEmpty("legacy poster id");

        packet.PopBoolean().Should().BeTrue("there is a receive side");
        packet.PopInt().Should().Be(1);
        packet.PopByte().Should().Be(0);
        packet.PopInt().Should().Be(10);

        // --- the mode, and the one int it pulls in
        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(5);

        // --- the three fields that must still land after all of that
        packet.PopBoolean().Should().BeFalse("showRequirementsImmediate");
        packet.PopBoolean().Should().BeTrue("overridePreviousTrade");
        packet.PopInt().Should().Be(300);
        packet.End.Should().BeTrue("the client reads exactly this much and no more");
    }

    /// <summary>A payment-only contract announces its missing receive side rather than omitting it.</summary>
    [Fact]
    public void APaymentOnlyContract_StillWritesTheReceiveFlag()
    {
        TradeContract contract = new()
        {
            YouGiveRules =
            [
                new TradeContractRule
                {
                    Nodes = [new TradeContractNode { IsFurni = false, Amount = 1 }],
                },
            ],
            Mode = 0,
        };

        ClientPacket packet = SkipHeader(Serialize(CustomType, contract));

        packet.PopBoolean().Should().BeTrue();
        packet.PopInt().Should().Be(1);
        packet.PopInt().Should().Be(1);
        packet.PopByte().Should().Be(0);
        packet.PopInt().Should().Be(1);

        packet.PopBoolean().Should().BeFalse("no receive side, but the flag is still there");
        packet.PopInt().Should().Be(0, "mode 0 pulls in no trailing int");

        packet.PopBoolean().Should().BeFalse();
        packet.PopBoolean().Should().BeTrue();
        packet.PopInt().Should().Be(300);
        packet.End.Should().BeTrue();
    }

    /// <summary>The deposit's own message: no rules block at all, because its type is not the custom one.</summary>
    [Fact]
    public void AnyFurni_WritesNoRulesBlock()
    {
        ClientPacket packet = SkipHeader(Serialize(AnyFurniType, contract: null));

        // Straight to the three trailing fields — no flags, no rules.
        packet.PopBoolean().Should().BeFalse();
        packet.PopBoolean().Should().BeTrue();
        packet.PopInt().Should().Be(300);
        packet.End.Should().BeTrue();
    }

    /// <summary>
    ///     A contract handed in under a non-custom type is not written, because the client would not
    ///     read it — and every field after it would land at the wrong offset.
    /// </summary>
    [Fact]
    public void AContractUnderTheWrongType_IsNotWritten()
    {
        TradeContract contract = new()
        {
            YouGiveRules =
            [
                new TradeContractRule
                {
                    Nodes = [new TradeContractNode { IsFurni = false, Amount = 1 }],
                },
            ],
            Mode = 0,
        };

        ClientPacket packet = SkipHeader(Serialize(AnyFurniType, contract));

        packet.PopBoolean().Should().BeFalse("this is showRequirementsImmediate, not a rules flag");
        packet.PopBoolean().Should().BeTrue();
        packet.PopInt().Should().Be(300);
        packet.End.Should().BeTrue();
    }

    /// <summary>Past the type and the two strings, which every case above reads the same way.</summary>
    private static ClientPacket SkipHeader(ClientPacket packet)
    {
        packet.PopInt();
        packet.PopString();
        packet.PopString();

        return packet;
    }

    /// <summary>Goes through the registered serializer, so the test reads what a client receives.</summary>
    private static ClientPacket Serialize(int requirementType, TradeContract? contract)
    {
        WiredTradeInitiateMessageComposer composer = new()
        {
            RequirementType = requirementType,
            YouGetText = string.Empty,
            LayoutType = string.Empty,
            ShowRequirementsImmediate = false,
            OverridePreviousTrade = true,
            TimeoutSeconds = 300,
            Contract = contract,
        };

        byte[] bytes = Revision
            .Serializers[typeof(WiredTradeInitiateMessageComposer)]
            .Serialize(composer)
            .ToArray();

        // Past the length prefix and the header, which is what a client drops before parsing.
        byte[] body = new byte[bytes.Length - 6];

        Array.Copy(bytes, 6, body, 0, body.Length);

        return new ClientPacket(0, body);
    }
}
