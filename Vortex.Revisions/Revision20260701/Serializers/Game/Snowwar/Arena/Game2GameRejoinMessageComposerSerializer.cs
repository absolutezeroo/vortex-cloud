using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Snowwar.Arena;

internal class Game2GameRejoinMessageComposerSerializer(int header)
    : AbstractSerializer<Game2GameRejoinMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, Game2GameRejoinMessageComposer message)
    {
        //
    }
}
