using Vortex.Primitives.Messages.Incoming.Inventory.Achievements;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Achievements;

internal class GetAchievementsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetAchievementsMessage();
}
