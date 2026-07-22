using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Messages.Incoming.Preferences;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
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

    private static readonly Rev Revision = new(Options.Create(new ProtocolLimitsConfig()));

    /// <summary>Writes packet fields the same way the client composer does, for parser input.</summary>
    private static ClientPacket BuildClientPacket(int header, Action<ServerPacket> write)
    {
        ServerPacket sp = new(header);
        write(sp);
        return new ClientPacket(header, sp.ToArray());
    }

    [Fact]
    public void SetSoundSettingsParser_ReadsTraxFurniGenericOrder()
    {
        // HabboSoundManagerFlash10::storeVolumeSetting() -> (trax, furni, generic).
        ClientPacket packet = BuildClientPacket(
            SetSoundSettingsEvent,
            sp =>
            {
                sp.WriteInteger(10); // trax
                sp.WriteInteger(20); // furni
                sp.WriteInteger(30); // generic
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
}
