using Vortex.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Snowwar.Arena;

internal class Game2StageStartingMessageComposerSerializer(int header)
    : AbstractSerializer<Game2StageStartingMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2StageStartingMessageComposer message
    )
    {
        //
    }
}
