using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class CallForHelpReplyMessageComposerSerializer(int header)
    : AbstractSerializer<CallForHelpReplyMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CallForHelpReplyMessageComposer message)
    {
        packet.WriteString(message.Message);
    }
}
