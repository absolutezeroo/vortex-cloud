using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Moderator;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class ModeratorMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.CloseIssueDefaultActionMessageEvent,
            new CloseIssueDefaultActionMessageParser()
        );
        builder.MapParser(MessageEvent.CloseIssuesMessageEvent, new CloseIssuesMessageParser());
        builder.MapParser(
            MessageEvent.DefaultSanctionMessageEvent,
            new DefaultSanctionMessageParser()
        );
        builder.MapParser(MessageEvent.GetCfhChatlogMessageEvent, new GetCfhChatlogMessageParser());
        builder.MapParser(
            MessageEvent.GetModeratorRoomInfoMessageEvent,
            new GetModeratorRoomInfoMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetModeratorUserInfoMessageEvent,
            new GetModeratorUserInfoMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetRoomChatlogMessageEvent,
            new GetRoomChatlogMessageParser()
        );
        builder.MapParser(MessageEvent.GetRoomVisitsMessageEvent, new GetRoomVisitsMessageParser());
        builder.MapParser(
            MessageEvent.GetUserChatlogMessageEvent,
            new GetUserChatlogMessageParser()
        );
        builder.MapParser(MessageEvent.ModAlertMessageEvent, new ModAlertMessageParser());
        builder.MapParser(MessageEvent.ModBanMessageEvent, new ModBanMessageParser());
        builder.MapParser(MessageEvent.ModerateRoomMessageEvent, new ModerateRoomMessageParser());
        builder.MapParser(
            MessageEvent.ModeratorActionMessageEvent,
            new ModeratorActionMessageParser()
        );
        builder.MapParser(MessageEvent.ModKickMessageEvent, new ModKickMessageParser());
        builder.MapParser(MessageEvent.ModMessageMessageEvent, new ModMessageMessageParser());
        builder.MapParser(MessageEvent.ModMuteMessageEvent, new ModMuteMessageParser());
        builder.MapParser(
            MessageEvent.ModToolPreferencesEvent,
            new ModToolPreferencesMessageParser()
        );
        builder.MapParser(MessageEvent.ModToolSanctionEvent, new ModToolSanctionMessageParser());
        builder.MapParser(
            MessageEvent.ModTradingLockMessageEvent,
            new ModTradingLockMessageParser()
        );
        builder.MapParser(MessageEvent.PickIssuesMessageEvent, new PickIssuesMessageParser());
        builder.MapParser(MessageEvent.ReleaseIssuesMessageEvent, new ReleaseIssuesMessageParser());
    }
}
