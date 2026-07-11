using Turbo.Primitives.Messages.Incoming.Room.Chat;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Parsers.Room.Chat;

internal class WhisperMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet)
    {
        string[] parts = packet.PopString().Split(' ', 2);
        string recipientName = parts[0];
        string text = parts[1];

        return new WhisperMessage
        {
            RecipientName = recipientName,
            Text = text,
            StyleId = packet.PopInt(),
        };
    }
}
