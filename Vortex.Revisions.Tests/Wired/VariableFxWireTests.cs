using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Wired;

/// <summary>
/// The Variable FX wire, read out of the AIR 1.0.31 client's own parsers rather than inferred.
/// </summary>
/// <remarks>
/// Worth pinning field by field because none of it is verifiable against the build this revision
/// otherwise targets: WIN63-202607011411 carries no Variable FX at all. These tests are the record of
/// what the later client's <c>parse()</c> actually reads, in the order it reads it — one field out of
/// place and every field after it is garbage, with no error anywhere.
/// </remarks>
public sealed class VariableFxWireTests
{
    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    private static ClientPacket Body(Type composerType, IComposer composer)
    {
        byte[] bytes = Revision.Serializers[composerType].Serialize(composer).ToArray();

        // AbstractSerializer prepends int length (4) + short header (2).
        byte[] body = new byte[bytes.Length - 6];

        Array.Copy(bytes, 6, body, 0, body.Length);

        return new ClientPacket(0, body);
    }

    [Fact]
    public void ConfigUpdate_WritesTheThirteenScalarsThenTheExtraMap()
    {
        VariableFxConfigUpdateMessageComposer composer = new()
        {
            Configs =
            [
                new WiredVariableFxConfigSnapshot
                {
                    ConfigId = 11,
                    IsUserFx = true,
                    ShowMode = 2,
                    Field4 = 3,
                    ShowOnMouseHover = true,
                    ShowDuration = 4000,
                    CategoryId = 0,
                    Field8 = 5,
                    ColorId = 6,
                    Field10 = 7,
                    RendererId = 8,
                    DefaultMinValue = 0,
                    DefaultMaxValue = 100,
                    Extra = [new WiredVariableFxExtra("skin", "boss")],
                },
            ],
        };

        ClientPacket packet = Body(typeof(VariableFxConfigUpdateMessageComposer), composer);

        packet.PopInt().Should().Be(1, "the count comes first, and nothing precedes it");
        packet.PopInt().Should().Be(11);
        packet.PopBoolean().Should().BeTrue();
        packet.PopInt().Should().Be(2);
        packet.PopInt().Should().Be(3);
        packet.PopBoolean().Should().BeTrue();
        packet.PopInt().Should().Be(4000);
        packet.PopInt().Should().Be(0, "category id is wire field 7, not a style id");
        packet.PopInt().Should().Be(5);
        packet.PopInt().Should().Be(6);
        packet.PopInt().Should().Be(7);
        packet.PopInt().Should().Be(8);
        packet.PopLong().Should().Be(0);
        packet.PopLong().Should().Be(100);
        packet.PopInt().Should().Be(1);
        packet.PopString().Should().Be("skin");
        packet.PopString().Should().Be("boss");
    }

    [Fact]
    public void StatusUpdate_WritesTheOverrideBoundsOnlyBehindTheirFlag()
    {
        VariableFxStatusUpdateMessageComposer composer = new()
        {
            ForceInitialize = true,
            Statuses =
            [
                new WiredVariableFxStatusSnapshot
                {
                    StatusKey = WiredVariableFxKey.Build(11, "hp", true, 42),
                    IsInitialize = false,
                    IsUserEntity = true,
                    EntityId = 42,
                    Value = 75,
                    Extra = [],
                },
            ],
        };

        ClientPacket packet = Body(typeof(VariableFxStatusUpdateMessageComposer), composer);

        packet.PopBoolean().Should().BeTrue("the force flag leads the message, before the count");
        packet.PopInt().Should().Be(1);
        packet.PopString().Should().Be("11|hp|u|42");
        packet.PopBoolean().Should().BeFalse();
        packet.PopBoolean().Should().BeTrue();
        packet.PopInt().Should().Be(42);
        packet.PopLong().Should().Be(75);

        // No bounds: the flag is false and the two longs are simply absent. Writing one of them
        // would shift the extra-map count into their place and the client would read a length out of
        // a value.
        packet.PopBoolean().Should().BeFalse();
        packet.PopInt().Should().Be(0);
    }

    [Fact]
    public void StatusUpdate_WritesBothBoundsWhenBothArePresent()
    {
        VariableFxStatusUpdateMessageComposer composer = new()
        {
            Statuses =
            [
                new WiredVariableFxStatusSnapshot
                {
                    StatusKey = WiredVariableFxKey.Build(1, "mana", false, 9),
                    IsInitialize = true,
                    IsUserEntity = false,
                    EntityId = 9,
                    Value = 5,
                    OverrideMinValue = -20,
                    OverrideMaxValue = 20,
                },
            ],
        };

        ClientPacket packet = Body(typeof(VariableFxStatusUpdateMessageComposer), composer);

        packet.PopBoolean().Should().BeFalse();
        packet.PopInt().Should().Be(1);
        packet.PopString().Should().Be("1|mana|f|9", "a furni entity is 'f', not 'u'");
        packet.PopBoolean().Should().BeTrue();
        packet.PopBoolean().Should().BeFalse();
        packet.PopInt().Should().Be(9);
        packet.PopLong().Should().Be(5);
        packet.PopBoolean().Should().BeTrue();
        packet.PopLong().Should().Be(-20);
        packet.PopLong().Should().Be(20);
        packet.PopInt().Should().Be(0);
    }

    [Fact]
    public void StatusUpdate_OneBoundWithoutTheOtherIsWrittenAsNeither()
    {
        // The client reads them behind a single flag, so "a minimum but no maximum" has no wire
        // form. Writing the one we have would desynchronise everything after it.
        VariableFxStatusUpdateMessageComposer composer = new()
        {
            Statuses =
            [
                new WiredVariableFxStatusSnapshot
                {
                    StatusKey = "1|hp|u|1",
                    IsInitialize = true,
                    IsUserEntity = true,
                    EntityId = 1,
                    Value = 1,
                    OverrideMinValue = 3,
                },
            ],
        };

        ClientPacket packet = Body(typeof(VariableFxStatusUpdateMessageComposer), composer);

        packet.PopBoolean();
        packet.PopInt();
        packet.PopString();
        packet.PopBoolean();
        packet.PopBoolean();
        packet.PopInt();
        packet.PopLong();

        packet.PopBoolean().Should().BeFalse();
        packet.PopInt().Should().Be(0, "the extra count follows immediately, not a stray bound");
    }

    [Fact]
    public void StatusRemove_IsACountAndTheKeys()
    {
        VariableFxStatusRemoveMessageComposer composer = new()
        {
            StatusKeys = ["11|hp|u|42", "11|hp|u|43"],
        };

        ClientPacket packet = Body(typeof(VariableFxStatusRemoveMessageComposer), composer);

        packet.PopInt().Should().Be(2);
        packet.PopString().Should().Be("11|hp|u|42");
        packet.PopString().Should().Be("11|hp|u|43");
    }

    [Fact]
    public void TheStatusKeyIsTheFormatTheClientSplitsByHand()
    {
        // configId | variableId | u-or-f | entityId. The client finds the config id at the first
        // '|', the entity id after the last, and the entity kind between the last two — so the
        // variable id is the only part allowed to be anything.
        WiredVariableFxKey.Build(7, "player.hp", true, 1234).Should().Be("7|player.hp|u|1234");
        WiredVariableFxKey.Build(7, "player.hp", false, 1234).Should().Be("7|player.hp|f|1234");
    }
}
