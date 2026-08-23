using Vortex.Primitives.Moderation;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class CallForHelpPendingCallsMessageComposerSerializer(int header)
    : AbstractSerializer<CallForHelpPendingCallsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CallForHelpPendingCallsMessageComposer message
    )
    {
        packet.WriteInteger(message.Calls.Length);

        foreach (CfhPendingCallSnapshot call in message.Calls)
        {
            packet.WriteString(call.CallId).WriteString(call.TimeStamp).WriteString(call.Message);
        }
    }
}
