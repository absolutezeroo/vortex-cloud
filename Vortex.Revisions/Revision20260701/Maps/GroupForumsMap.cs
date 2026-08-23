using Vortex.Primitives.Networking.Revisions;
using Vortex.Protocol.Messages.Outgoing.Groupforums;
using Vortex.Revisions.Revision20260701.Parsers.GroupForums;
using Vortex.Revisions.Revision20260701.Serializers.GroupForums;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class GroupForumsMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(MessageEvent.GetForumsListMessageEvent, new GetForumsListMessageParser());
        builder.MapParser(MessageEvent.GetForumStatsMessageEvent, new GetForumStatsMessageParser());
        builder.MapParser(MessageEvent.GetMessagesMessageEvent, new GetMessagesMessageParser());
        builder.MapParser(MessageEvent.GetThreadMessageEvent, new GetThreadMessageParser());
        builder.MapParser(MessageEvent.GetThreadsMessageEvent, new GetThreadsMessageParser());
        builder.MapParser(
            MessageEvent.GetUnreadForumsCountMessageEvent,
            new GetUnreadForumsCountMessageParser()
        );
        builder.MapParser(
            MessageEvent.ModerateMessageMessageEvent,
            new ModerateMessageMessageParser()
        );
        builder.MapParser(
            MessageEvent.ModerateThreadMessageEvent,
            new ModerateThreadMessageParser()
        );
        builder.MapParser(MessageEvent.PostMessageMessageEvent, new PostMessageMessageParser());
        builder.MapParser(
            MessageEvent.UpdateForumReadMarkerMessageEvent,
            new UpdateForumReadMarkerMessageParser()
        );
        builder.MapParser(
            MessageEvent.UpdateForumSettingsMessageEvent,
            new UpdateForumSettingsMessageParser()
        );
        builder.MapParser(MessageEvent.UpdateThreadMessageEvent, new UpdateThreadMessageParser());

        builder.MapSerializer(
            typeof(UnreadForumsCountMessageComposer),
            new UnreadForumsCountMessageComposerSerializer(
                MessageComposer.UnreadForumsCountMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(ForumsListMessageComposer),
            new ForumsListMessageComposerSerializer(MessageComposer.ForumsListMessageComposer)
        );
        builder.MapSerializer(
            typeof(ForumDataMessageComposer),
            new ForumDataMessageComposerSerializer(MessageComposer.ForumDataMessageComposer)
        );
        builder.MapSerializer(
            typeof(ForumThreadsMessageComposer),
            new ForumThreadsMessageComposerSerializer(MessageComposer.ForumThreadsMessageComposer)
        );
        builder.MapSerializer(
            typeof(ThreadMessagesMessageComposer),
            new ThreadMessagesMessageComposerSerializer(
                MessageComposer.ThreadMessagesMessageComposer
            )
        );
        builder.MapSerializer(
            typeof(PostThreadMessageComposer),
            new PostThreadMessageComposerSerializer(MessageComposer.PostThreadMessageComposer)
        );
        builder.MapSerializer(
            typeof(PostMessageMessageComposer),
            new PostMessageMessageComposerSerializer(MessageComposer.PostMessageMessageComposer)
        );
        builder.MapSerializer(
            typeof(UpdateThreadMessageComposer),
            new UpdateThreadMessageComposerSerializer(MessageComposer.UpdateThreadMessageComposer)
        );
        builder.MapSerializer(
            typeof(UpdateMessageMessageComposer),
            new UpdateMessageMessageComposerSerializer(MessageComposer.UpdateMessageMessageComposer)
        );
    }
}
