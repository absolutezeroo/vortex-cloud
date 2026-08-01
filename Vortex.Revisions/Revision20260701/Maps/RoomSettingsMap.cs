using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Configuration;
using Vortex.Revisions.Revision20260701.Parsers.RoomSettings;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class RoomSettingsMap : IRevisionMap
{
    private readonly ProtocolLimitsConfig _protocolLimits;

    public RoomSettingsMap(ProtocolLimitsConfig protocolLimits)
    {
        _protocolLimits = protocolLimits;
    }

    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(MessageEvent.DeleteRoomMessageEvent, new DeleteRoomMessageParser());
        builder.MapParser(
            MessageEvent.GetBannedUsersFromRoomMessageEvent,
            new GetBannedUsersFromRoomMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetCustomRoomFilterMessageEvent,
            new GetCustomRoomFilterMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetFlatControllersMessageEvent,
            new GetFlatControllersMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetRoomSettingsMessageEvent,
            new GetRoomSettingsMessageParser()
        );
        builder.MapParser(
            MessageEvent.SaveRoomSettingsMessageEvent,
            new SaveRoomSettingsMessageParser(_protocolLimits.MaxRoomTags)
        );
        builder.MapParser(
            MessageEvent.UpdateRoomCategoryAndTradeSettingsEvent,
            new UpdateRoomCategoryAndTradeSettingsMessageParser()
        );
        builder.MapParser(
            MessageEvent.UpdateRoomFilterMessageEvent,
            new UpdateRoomFilterMessageParser()
        );
    }
}
