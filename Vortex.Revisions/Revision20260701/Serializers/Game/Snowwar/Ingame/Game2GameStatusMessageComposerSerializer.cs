using Vortex.Primitives.Messages.Outgoing.Game.Snowwar.Ingame;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Snowwar.Ingame;

internal class Game2GameStatusMessageComposerSerializer(int header)
    : AbstractSerializer<Game2GameStatusMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, Game2GameStatusMessageComposer message)
    {
        //
    }
}
