using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class CallForHelpReplyMessageComposerSerializer(int header)
    : AbstractSerializer<CallForHelpReplyMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CallForHelpReplyMessageComposer message)
    {
        packet.WriteString(message.Message);
    }
}
