using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Habbicons;

namespace Vortex.Revisions.Revision20260701.Parsers.Habbicons;

/// <summary>
/// (chatId, habbiconId, confirmationId), in that order -- messenger/MainView sends
/// <c>_SafeCls_2591(_conversationId, habbiconId, nextConfirmationId)</c>.
/// </summary>
internal class SendHabbiconMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SendHabbiconMessage
        {
            ChatId = packet.PopInt(),
            HabbiconId = packet.PopInt(),
            ConfirmationId = packet.PopInt(),
        };
}
