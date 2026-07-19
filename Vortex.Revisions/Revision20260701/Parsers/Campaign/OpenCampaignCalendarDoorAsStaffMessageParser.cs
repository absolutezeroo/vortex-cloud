using Vortex.Primitives.Messages.Incoming.Campaign;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Campaign;

internal class OpenCampaignCalendarDoorAsStaffMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new OpenCampaignCalendarDoorAsStaffMessage();
}
