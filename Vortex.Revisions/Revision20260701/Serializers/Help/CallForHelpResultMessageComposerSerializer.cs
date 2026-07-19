using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class CallForHelpResultMessageComposerSerializer(int header)
    : AbstractSerializer<CallForHelpResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CallForHelpResultMessageComposer message
    )
    {
        packet.WriteInteger(message.ResultType).WriteString(message.MessageText);
    }
}
