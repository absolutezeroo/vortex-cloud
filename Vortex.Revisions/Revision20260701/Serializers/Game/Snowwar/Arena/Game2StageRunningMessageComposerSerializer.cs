using Vortex.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Snowwar.Arena;

internal class Game2StageRunningMessageComposerSerializer(int header)
    : AbstractSerializer<Game2StageRunningMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2StageRunningMessageComposer message
    )
    {
        //
    }
}
