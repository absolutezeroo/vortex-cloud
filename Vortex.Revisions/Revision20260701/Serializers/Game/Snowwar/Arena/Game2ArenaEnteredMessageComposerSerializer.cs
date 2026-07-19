using Vortex.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Vortex.Primitives.Packets;

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
