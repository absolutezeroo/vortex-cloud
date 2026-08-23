using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Snowwar.Arena;

internal class Game2ArenaEnteredMessageComposerSerializer(int header)
    : AbstractSerializer<Game2ArenaEnteredMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2ArenaEnteredMessageComposer message
    )
    {
        //
    }
}
