using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Incoming.Avatar;
using Vortex.Primitives.Messages.Outgoing.Avatar;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Avatar;

/// <summary>
///     Locks the avatar-editor wardrobe byte contract against the Flash client (SaveWardrobeOutfit
///     request + the Wardrobe response consumed by WardrobeMessageParser / OutfitData). These were
///     empty stubs that silently dropped saved outfits.
/// </summary>
public sealed class WardrobeWireLayoutTests
{
    private const int SaveWardrobeOutfitEvent = 116;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket BuildClientPacket(int header, Action<ServerPacket> write)
    {
        ServerPacket sp = new(header);
        write(sp);
        return new ClientPacket(header, sp.ToArray());
    }

    private static ClientPacket SerializeAndReadBody(Type composerType, IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();
        // AbstractSerializer prepends int length (4) + short header (2).
        byte[] body = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, body, 0, body.Length);
        return new ClientPacket(0, body);
    }

    [Fact]
    public void SaveWardrobeOutfitParser_ReadsSlotFigureGender()
    {
        // SaveWardrobeOutfitMessageComposer(slotId, figure, gender).
        ClientPacket packet = BuildClientPacket(
            SaveWardrobeOutfitEvent,
            sp =>
            {
                sp.WriteInteger(3);
                sp.WriteString("hd-180-1.ch-255-66");
                sp.WriteString("M");
            }
        );

        SaveWardrobeOutfitMessage message = Revision
            .Parsers[SaveWardrobeOutfitEvent]
            .Parse(packet)
            .Should()
            .BeOfType<SaveWardrobeOutfitMessage>()
            .Subject;

        message.SlotId.Should().Be(3);
        message.Figure.Should().Be("hd-180-1.ch-255-66");
        message.Gender.Should().Be("M");
    }

    [Fact]
    public void WardrobeSerializer_WritesStateCountAndOutfits()
    {
        WardrobeMessageComposer composer = new()
        {
            Outfits = new List<PlayerWardrobeOutfitSnapshot>
            {
                new()
                {
                    SlotId = 1,
                    Figure = "hd-180-1",
                    Gender = "M",
                },
                new()
                {
                    SlotId = 4,
                    Figure = "hd-600-2",
                    Gender = "F",
                },
            },
        };

        ClientPacket body = SerializeAndReadBody(typeof(WardrobeMessageComposer), composer);

        body.PopInt().Should().Be(0); // state (unused by the client)
        body.PopInt().Should().Be(2); // count
        body.PopInt().Should().Be(1); // slot
        body.PopString().Should().Be("hd-180-1");
        body.PopString().Should().Be("M");
        body.PopInt().Should().Be(4);
        body.PopString().Should().Be("hd-600-2");
        body.PopString().Should().Be("F");
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }
}
