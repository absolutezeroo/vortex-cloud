using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Help;

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
    }
}
