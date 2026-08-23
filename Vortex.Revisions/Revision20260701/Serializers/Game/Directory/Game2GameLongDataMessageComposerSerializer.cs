using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Game.Directory;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Directory;

internal class Game2GameLongDataMessageComposerSerializer(int header)
    : AbstractSerializer<Game2GameLongDataMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2GameLongDataMessageComposer message
    )
    {
        //
    }
}
