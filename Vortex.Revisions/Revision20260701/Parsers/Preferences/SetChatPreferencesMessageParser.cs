using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Preferences;

namespace Vortex.Revisions.Revision20260701.Parsers.Preferences;

internal class SetChatPreferencesMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new SetChatPreferencesMessage { FreeFlowChatDisabled = packet.PopBoolean() };
}
