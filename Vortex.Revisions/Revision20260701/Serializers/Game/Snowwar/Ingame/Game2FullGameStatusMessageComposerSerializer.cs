using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Game.Snowwar.Ingame;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Snowwar.Ingame;

internal class Game2FullGameStatusMessageComposerSerializer(int header)
    : AbstractSerializer<Game2FullGameStatusMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2FullGameStatusMessageComposer message
    )
    {
        //
    }
}
