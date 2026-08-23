using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Game.Directory;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Directory;

internal class Game2GameCreatedMessageComposerSerializer(int header)
    : AbstractSerializer<Game2GameCreatedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, Game2GameCreatedMessageComposer message)
    {
        //
    }
}
