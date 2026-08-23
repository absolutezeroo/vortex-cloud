using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Game.Snowwar.Ingame;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Snowwar.Ingame;

internal class Game2GameStatusMessageComposerSerializer(int header)
    : AbstractSerializer<Game2GameStatusMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, Game2GameStatusMessageComposer message)
    {
        //
    }
}
