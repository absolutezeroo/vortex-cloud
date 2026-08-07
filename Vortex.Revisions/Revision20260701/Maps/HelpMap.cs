using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Help;
using Vortex.Revisions.Revision20260701.Serializers.Help;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class HelpMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(
            MessageEvent.CallForHelpFromForumMessageMessageEvent,
            new CallForHelpFromForumMessageMessageParser()
        );
        builder.MapParser(
            MessageEvent.CallForHelpFromForumThreadMessageEvent,
            new CallForHelpFromForumThreadMessageParser()
        );
        builder.MapParser(
            MessageEvent.CallForHelpFromIMMessageEvent,
            new CallForHelpFromIMMessageParser()
        );
        builder.MapParser(
            MessageEvent.CallForHelpFromPhotoMessageEvent,
            new CallForHelpFromPhotoMessageParser()
        );
        builder.MapParser(
            MessageEvent.CallForHelpFromSelfieMessageEvent,
            new CallForHelpFromSelfieMessageParser()
        );
        builder.MapParser(MessageEvent.CallForHelpMessageEvent, new CallForHelpMessageParser());
        builder.MapParser(
            MessageEvent.ChatReviewGuideDecidesOnOfferMessageEvent,
            new ChatReviewGuideDecidesOnOfferMessageParser()
        );
        builder.MapParser(
            MessageEvent.ChatReviewGuideDetachedMessageEvent,
            new ChatReviewGuideDetachedMessageParser()
        );
        builder.MapParser(
            MessageEvent.ChatReviewGuideVoteMessageEvent,
            new ChatReviewGuideVoteMessageParser()
        );
        builder.MapParser(
            MessageEvent.ChatReviewSessionCreateMessageEvent,
            new ChatReviewSessionCreateMessageParser()
        );
        builder.MapParser(
            MessageEvent.DeletePendingCallsForHelpMessageEvent,
            new DeletePendingCallsForHelpMessageParser()
        );
        builder.MapParser(MessageEvent.GetCfhStatusMessageEvent, new GetCfhStatusMessageParser());
        builder.MapParser(
            MessageEvent.GetGuideReportingStatusMessageEvent,
            new GetGuideReportingStatusMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetPendingCallsForHelpMessageEvent,
            new GetPendingCallsForHelpMessageParser()
        );
        builder.MapParser(MessageEvent.GetQuizQuestionsEvent, new GetQuizQuestionsMessageParser());
        builder.MapParser(
            MessageEvent.GuideSessionCreateMessageEvent,
            new GuideSessionCreateMessageParser()
        );
        builder.MapParser(
            MessageEvent.GuideSessionFeedbackMessageEvent,
            new GuideSessionFeedbackMessageParser()
        );
        builder.MapParser(
            MessageEvent.GuideSessionGetRequesterRoomMessageEvent,
            new GuideSessionGetRequesterRoomMessageParser()
        );
        builder.MapParser(
            MessageEvent.GuideSessionGuideDecidesMessageEvent,
            new GuideSessionGuideDecidesMessageParser()
        );
        builder.MapParser(
            MessageEvent.GuideSessionInviteRequesterMessageEvent,
            new GuideSessionInviteRequesterMessageParser()
        );
        builder.MapParser(
            MessageEvent.GuideSessionIsTypingMessageEvent,
            new GuideSessionIsTypingMessageParser()
        );
        builder.MapParser(
            MessageEvent.GuideSessionMessageMessageEvent,
            new GuideSessionMessageMessageParser()
        );
        builder.MapParser(
            MessageEvent.GuideSessionOnDutyUpdateMessageEvent,
            new GuideSessionOnDutyUpdateMessageParser()
        );
        builder.MapParser(
            MessageEvent.GuideSessionReportMessageEvent,
            new GuideSessionReportMessageParser()
        );
        builder.MapParser(
            MessageEvent.GuideSessionRequesterCancelsMessageEvent,
            new GuideSessionRequesterCancelsMessageParser()
        );
        builder.MapParser(
            MessageEvent.GuideSessionResolvedMessageEvent,
            new GuideSessionResolvedMessageParser()
        );
        builder.MapParser(MessageEvent.PostQuizAnswersEvent, new PostQuizAnswersMessageParser());

        builder.MapSerializer(
            typeof(CallForHelpReplyMessageComposer),
            new CallForHelpReplyMessageComposerSerializer(
                MessageComposer.CallForHelpReplyMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(IssueCloseNotificationMessageComposer),
            new IssueCloseNotificationMessageComposerSerializer(
                MessageComposer.IssueCloseNotificationMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(GuideSessionAttachedMessageComposer),
            new GuideSessionAttachedMessageComposerSerializer(
                MessageComposer.GuideSessionAttachedMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(GuideSessionStartedMessageComposer),
            new GuideSessionStartedMessageComposerSerializer(
                MessageComposer.GuideSessionStartedMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(GuideSessionErrorMessageComposer),
            new GuideSessionErrorMessageComposerSerializer(
                MessageComposer.GuideSessionErrorMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(GuideSessionMessageMessageComposer),
            new GuideSessionMessageMessageComposerSerializer(
                MessageComposer.GuideSessionMessageMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(GuideSessionPartnerIsTypingMessageComposer),
            new GuideSessionPartnerIsTypingMessageComposerSerializer(
                MessageComposer.GuideSessionPartnerIsTypingMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(GuideSessionEndedMessageComposer),
            new GuideSessionEndedMessageComposerSerializer(
                MessageComposer.GuideSessionEndedMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(GuideOnDutyStatusMessageComposer),
            new GuideOnDutyStatusMessageComposerSerializer(
                MessageComposer.GuideOnDutyStatusMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(CallForHelpPendingCallsMessageComposer),
            new CallForHelpPendingCallsMessageComposerSerializer(
                MessageComposer.CallForHelpPendingCallsMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(CallForHelpPendingCallsDeletedMessageComposer),
            new CallForHelpPendingCallsDeletedMessageComposerSerializer(
                MessageComposer.CallForHelpPendingCallsDeletedMessageComposer
            )
        );
    }
}
