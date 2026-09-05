using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Preferences;
using Vortex.Protocol.Messages.Outgoing.Preferences;
using Vortex.Revisions.Configuration;
using Xunit;
using Rev = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Tests.Preferences;

/// <summary>
///     Locks the byte contract of the account-preference setters against the AS3-verified client
///     wire layout (header noted per test). These parsers were empty stubs that silently dropped
///     whatever the player changed in the settings dialog.
/// </summary>
public sealed class PreferencesWireLayoutTests
{
    // Incoming MessageEvent ids from Revision20260701 Headers.cs.
    private const int SetSoundSettingsEvent = 3662;
    private const int SetChatPreferencesEvent = 1149;
    private const int SetIgnoreRoomInvitesEvent = 1332;
    private const int SetRoomCameraPreferencesEvent = 3917;
    private const int SetUIFlagsEvent = 3653;
    private const int GetDiscordPreferencesEvent = 2883;
    private const int SetDiscordPreferencesEvent = 2304;

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    /// <summary>Writes packet fields the same way the client composer does, for parser input.</summary>
    private static ClientPacket BuildClientPacket(int header, Action<ServerPacket> write)
    {
        ServerPacket sp = new(header);
        write(sp);
        return new ClientPacket(header, sp.ToArray());
    }

    [Fact]
    public void SetSoundSettingsParser_ReadsGenericFurniTraxOrder()
    {
        // The wire order is (generic, furni, trax), not the (trax, furni, generic) the AS3
        // constructor signature suggests: _SafeCls_2171 takes (trax, furni, generic) but pushes
        // them back to front, so generic goes out first. This test asserted the signature order
        // until 2026-08-02, which is the same mistake the parser itself had -- the two agreed with
        // each other and both were wrong, so the suite stayed green while music and interface
        // volumes were stored swapped.
        ClientPacket packet = BuildClientPacket(
            SetSoundSettingsEvent,
            sp =>
            {
                sp.WriteInteger(30); // generic
                sp.WriteInteger(20); // furni
                sp.WriteInteger(10); // trax
            }
        );

        SetSoundSettingsMessage message = Revision
            .Parsers[SetSoundSettingsEvent]
            .Parse(packet)
            .Should()
            .BeOfType<SetSoundSettingsMessage>()
            .Subject;

        message.Trax.Should().Be(10);
        message.Furni.Should().Be(20);
        message.Generic.Should().Be(30);
    }

    [Fact]
    public void SetChatPreferencesParser_ReadsFreeFlowDisabledBool()
    {
        ClientPacket packet = BuildClientPacket(
            SetChatPreferencesEvent,
            sp => sp.WriteBoolean(true)
        );

        SetChatPreferencesMessage message = Revision
            .Parsers[SetChatPreferencesEvent]
            .Parse(packet)
            .Should()
            .BeOfType<SetChatPreferencesMessage>()
            .Subject;

        message.FreeFlowChatDisabled.Should().BeTrue();
    }

    [Fact]
    public void SetIgnoreRoomInvitesParser_ReadsIgnoredBool()
    {
        ClientPacket packet = BuildClientPacket(
            SetIgnoreRoomInvitesEvent,
            sp => sp.WriteBoolean(true)
        );

        SetIgnoreRoomInvitesMessage message = Revision
            .Parsers[SetIgnoreRoomInvitesEvent]
            .Parse(packet)
            .Should()
            .BeOfType<SetIgnoreRoomInvitesMessage>()
            .Subject;

        message.Ignored.Should().BeTrue();
    }

    [Fact]
    public void SetRoomCameraPreferencesParser_ReadsDisabledBool()
    {
        ClientPacket packet = BuildClientPacket(
            SetRoomCameraPreferencesEvent,
            sp => sp.WriteBoolean(true)
        );

        SetRoomCameraPreferencesMessage message = Revision
            .Parsers[SetRoomCameraPreferencesEvent]
            .Parse(packet)
            .Should()
            .BeOfType<SetRoomCameraPreferencesMessage>()
            .Subject;

        message.Disabled.Should().BeTrue();
    }

    [Fact]
    public void SetUIFlagsParser_ReadsFlagsInt()
    {
        ClientPacket packet = BuildClientPacket(SetUIFlagsEvent, sp => sp.WriteInteger(3));

        SetUIFlagsMessage message = Revision
            .Parsers[SetUIFlagsEvent]
            .Parse(packet)
            .Should()
            .BeOfType<SetUIFlagsMessage>()
            .Subject;

        message.Flags.Should().Be(3);
    }

    [Fact]
    public void SetDiscordPreferencesParser_ReadsVersionThenFourOneByteBools()
    {
        // _SafeCls_3638 pushes (version, showHabbo, shareActivity, hideInHiddenRooms, allowJoining)
        // in that order, and the client's encoder writes a real Boolean with writeBoolean -- one
        // byte, not the four-byte int several of its other composers use for a flag.
        ClientPacket packet = BuildClientPacket(
            SetDiscordPreferencesEvent,
            sp =>
            {
                sp.WriteInteger(2);
                sp.WriteBoolean(true);
                sp.WriteBoolean(false);
                sp.WriteBoolean(true);
                sp.WriteBoolean(false);
            }
        );

        SetDiscordPreferencesMessage message = Revision
            .Parsers[SetDiscordPreferencesEvent]
            .Parse(packet)
            .Should()
            .BeOfType<SetDiscordPreferencesMessage>()
            .Subject;

        message.Version.Should().Be(2);
        message.ShowHabbo.Should().BeTrue();
        message.ShareActivity.Should().BeFalse();
        message.HideInHiddenRooms.Should().BeTrue();
        message.AllowJoining.Should().BeFalse();
    }

    [Fact]
    public void GetDiscordPreferencesParser_IsMappedAndTakesNoBody()
    {
        ClientPacket packet = BuildClientPacket(GetDiscordPreferencesEvent, _ => { });

        Revision
            .Parsers[GetDiscordPreferencesEvent]
            .Parse(packet)
            .Should()
            .BeOfType<GetDiscordPreferencesMessage>();
    }

    [Fact]
    public void DiscordPreferencesSerializer_WritesVersionThenFourBools()
    {
        // DiscordPreferences.readFromData(): readInteger, then four readBoolean.
        DiscordPreferencesEventMessageComposer composer = new()
        {
            Version = 1,
            ShowHabbo = true,
            ShareActivity = false,
            HideInHiddenRooms = true,
            AllowJoining = false,
        };

        byte[] bytes = Revision
            .Serializers[typeof(DiscordPreferencesEventMessageComposer)]
            .Serialize(composer)
            .ToArray();

        // AbstractSerializer prepends int length (4) + short header (2).
        byte[] payload = new byte[bytes.Length - 6];
        Array.Copy(bytes, 6, payload, 0, payload.Length);
        ClientPacket body = new(0, payload);

        body.PopInt().Should().Be(1);
        body.PopBoolean().Should().BeTrue();
        body.PopBoolean().Should().BeFalse();
        body.PopBoolean().Should().BeTrue();
        body.PopBoolean().Should().BeFalse();
        body.End.Should().BeTrue("the layout must consume the whole packet");
    }
}
