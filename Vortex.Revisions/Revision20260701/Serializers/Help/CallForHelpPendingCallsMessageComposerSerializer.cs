using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class CallForHelpPendingCallsMessageComposerSerializer(int header)
    : AbstractSerializer<CallForHelpPendingCallsMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CallForHelpPendingCallsMessageComposer message
    )
    {
        //
    }
}
