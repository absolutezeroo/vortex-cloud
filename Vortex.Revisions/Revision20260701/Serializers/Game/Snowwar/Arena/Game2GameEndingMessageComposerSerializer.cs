using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Game.Snowwar.Arena;

namespace Vortex.Revisions.Revision20260701.Serializers.Game.Snowwar.Arena;

internal class Game2GameEndingMessageComposerSerializer(int header)
    : AbstractSerializer<Game2GameEndingMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, Game2GameEndingMessageComposer message)
    {
        //
    }
}
