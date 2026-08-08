using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Messages.Outgoing.Inventory.Bots;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Inventory;

/// <summary>
///     The bot block hides a field swap: the client's class_3143 reads id, name, motto, gender,
///     figure, but its own getters are declared id, name, motto, FIGURE, gender. Writing them in
///     getter order compiles, serialises, and puts every bot's sex where its look belongs — the sort
///     of thing that only shows up as "all my bots render wrong" in game.
/// </summary>
public sealed class BotInventoryWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket SerializeAndReadBody(Type composerType, IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    private static BotSnapshot NewBot(int id, AvatarGenderType gender) =>
        new()
        {
            BotId = id,
            OwnerId = (PlayerId)1,
            Name = "Bartender",
            Motto = "what will it be",
            Figure = "hd-180-1.ch-255-66",
            Gender = gender,
        };

    [Fact]
    public void BotBlock_WritesGenderBeforeFigure()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(BotInventoryEventMessageComposer),
            new BotInventoryEventMessageComposer { Bots = [NewBot(7, AvatarGenderType.Female)] }
        );

        body.PopInt().Should().Be(1);
        body.PopInt().Should().Be(7);
        body.PopString().Should().Be("Bartender");
        body.PopString().Should().Be("what will it be");
        body.PopString().Should().Be("f", "the client reads gender fourth, ahead of the figure");
        body.PopString().Should().Be("hd-180-1.ch-255-66");
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }

    [Fact]
    public void BotInventory_WritesTheCountThenEveryBot()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(BotInventoryEventMessageComposer),
            new BotInventoryEventMessageComposer
            {
                Bots = [NewBot(7, AvatarGenderType.Male), NewBot(8, AvatarGenderType.Male)],
            }
        );

        body.PopInt().Should().Be(2);

        for (int i = 0; i < 2; i++)
        {
            body.PopInt();
            body.PopString();
            body.PopString();
            body.PopString().Should().Be("m");
            body.PopString();
        }

        body.End.Should().BeTrue("the layout must consume the whole packet");
    }

    [Fact]
    public void BotInventory_IsAnEmptyListWhenThePlayerOwnsNoBots()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(BotInventoryEventMessageComposer),
            new BotInventoryEventMessageComposer()
        );

        body.PopInt().Should().Be(0);
        body.End.Should().BeTrue("an empty inventory is a count of zero and nothing else");
    }

    [Fact]
    public void BotAddedToInventory_AppendsTheOpenInventoryFlagAfterTheBlock()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(BotAddedToInventoryEventMessageComposer),
            new BotAddedToInventoryEventMessageComposer
            {
                Bot = NewBot(7, AvatarGenderType.Male),
                OpenInventory = true,
            }
        );

        body.PopInt().Should().Be(7);
        body.PopString();
        body.PopString();
        body.PopString();
        body.PopString();
        body.PopBoolean().Should().BeTrue();
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }

    [Fact]
    public void BotRemovedFromInventory_WritesOnlyTheId()
    {
        ClientPacket body = SerializeAndReadBody(
            typeof(BotRemovedFromInventoryEventMessageComposer),
            new BotRemovedFromInventoryEventMessageComposer { BotId = 7 }
        );

        body.PopInt().Should().Be(7);
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }
}
