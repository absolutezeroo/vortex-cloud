using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Preferences;

namespace Vortex.Revisions.Revision20260701.Parsers.Preferences;

/// <summary>
/// Wire order is the composer's push order: version, then the four toggles as one-byte booleans —
/// the client pushes real <c>Boolean</c>s, which its encoder writes with <c>writeBoolean</c>, not as
/// the four-byte int some of its other composers use.
/// </summary>
internal class SetDiscordPreferencesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SetDiscordPreferencesMessage
        {
            Version = packet.PopInt(),
            ShowHabbo = packet.PopBoolean(),
            ShareActivity = packet.PopBoolean(),
            HideInHiddenRooms = packet.PopBoolean(),
            AllowJoining = packet.PopBoolean(),
        };
}
