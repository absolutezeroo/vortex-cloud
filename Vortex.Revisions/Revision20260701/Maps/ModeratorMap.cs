using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Moderator;
using Vortex.Revisions.Revision20260701.Serializers.Moderation;

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
        // The client has a single "load moderator user info" request; ModeratorActionMessageEvent
        // used to claim the same header, which is why only one of the two can be mapped here.
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
        builder.MapParser(
            MessageEvent.ModToolRoomAlertMessageEvent,
            new ModToolRoomAlertMessageParser()
        );
        builder.MapParser(MessageEvent.ModerateRoomMessageEvent, new ModerateRoomMessageParser());
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

        builder.MapSerializer(
            typeof(CfhChatlogEventMessageComposer),
            new CfhChatlogEventMessageComposerSerializer(MessageComposer.CfhChatlogComposer)
        );
        builder.MapSerializer(
            typeof(IssueDeletedMessageComposer),
            new IssueDeletedMessageComposerSerializer(MessageComposer.IssueDeletedMessageComposer)
        );
        builder.MapSerializer(
            typeof(IssueInfoMessageComposer),
            new IssueInfoMessageComposerSerializer(MessageComposer.IssueInfoMessageComposer)
        );
        builder.MapSerializer(
            typeof(IssuePickFailedMessageComposer),
            new IssuePickFailedMessageComposerSerializer(
                MessageComposer.IssuePickFailedMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(ModeratorActionResultMessageComposer),
            new ModeratorActionResultMessageComposerSerializer(
                MessageComposer.ModeratorActionResultMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(ModeratorCautionEventMessageComposer),
            new ModeratorCautionEventMessageComposerSerializer(
                MessageComposer.ModeratorCautionComposer
            )
        );
        builder.MapSerializer(
            typeof(ModeratorInitMessageComposer),
            new ModeratorInitMessageComposerSerializer(MessageComposer.ModeratorInitMessageComposer)
        );
        builder.MapSerializer(
            typeof(ModeratorMessageComposer),
            new ModeratorMessageComposerSerializer(MessageComposer.ModeratorMessageComposer)
        );
        builder.MapSerializer(
            typeof(ModeratorRoomInfoEventMessageComposer),
            new ModeratorRoomInfoEventMessageComposerSerializer(
                MessageComposer.ModeratorRoomInfoComposer
            )
        );
        builder.MapSerializer(
            typeof(ModeratorToolPreferencesEventMessageComposer),
            new ModeratorToolPreferencesEventMessageComposerSerializer(
                MessageComposer.ModeratorToolPreferencesComposer
            )
        );
        builder.MapSerializer(
            typeof(ModeratorUserInfoEventMessageComposer),
            new ModeratorUserInfoEventMessageComposerSerializer(
                MessageComposer.ModeratorUserInfoComposer
            )
        );
        builder.MapSerializer(
            typeof(RoomChatlogEventMessageComposer),
            new RoomChatlogEventMessageComposerSerializer(MessageComposer.RoomChatlogComposer)
        );
        builder.MapSerializer(
            typeof(RoomVisitsEventMessageComposer),
            new RoomVisitsEventMessageComposerSerializer(MessageComposer.RoomVisitsComposer)
        );
        builder.MapSerializer(
            typeof(SanctionInfoMessageComposer),
            new SanctionInfoMessageComposerSerializer(MessageComposer.SanctionInfoMessageComposer)
        );
        builder.MapSerializer(
            typeof(UserBannedMessageComposer),
            new UserBannedMessageComposerSerializer(MessageComposer.UserBannedMessageComposer)
        );
        builder.MapSerializer(
            typeof(UserChatlogEventMessageComposer),
            new UserChatlogEventMessageComposerSerializer(MessageComposer.UserChatlogComposer)
        );
    }
}
