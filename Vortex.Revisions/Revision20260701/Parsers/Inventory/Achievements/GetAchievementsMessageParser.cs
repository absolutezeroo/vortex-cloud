using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Inventory.Achievements;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Achievements;

internal class GetAchievementsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetAchievementsMessage();
}
