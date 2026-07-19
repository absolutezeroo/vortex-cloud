using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class CallForHelpDisabledNotifyMessageComposerSerializer(int header)
    : AbstractSerializer<CallForHelpDisabledNotifyMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        CallForHelpDisabledNotifyMessageComposer message
    )
    {
        //
    }
}
