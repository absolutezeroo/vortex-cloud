using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Snowwar.Arena;

internal class Game2EnterArenaMessageComposerSerializer(int header)
    : AbstractSerializer<Game2EnterArenaMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, Game2EnterArenaMessageComposer message)
    {
        //
    }
}
