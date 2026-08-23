using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Competition;

namespace Vortex.Revisions.Revision20260701.Serializers.Competition;

internal class SecondsUntilMessageComposerSerializer(int header)
    : AbstractSerializer<SecondsUntilMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, SecondsUntilMessageComposer message)
    {
        //
    }
}
