using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Messenger;
using Turbo.Database.Entities.Players;
using Turbo.Primitives.FriendList.Enums;
using Turbo.Primitives.FriendList.Grains;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Snapshots.FriendList;

namespace Turbo.Players.Grains;

internal sealed partial class MessengerGrain
{
    public async Task<InstantMessageErrorCodeType?> SendMessageAsync(
        PlayerId receiverId,
        string message,
        int chatId,
        int confirmationId,
        CancellationToken ct
    )
    {
        if (!_friends.ContainsKey(receiverId))
        {
            return InstantMessageErrorCodeType.NotFriend;
        }

        if (_blockedIds.Contains(receiverId.Value))
        {
            return InstantMessageErrorCodeType.ReceiverMuted;
        }

        DateTime now = DateTime.UtcNow;

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        PlayerEntity? selfEntity = await dbCtx.Players.FindAsync([(int)SelfId], ct);
        PlayerEntity? receiverEntity = await dbCtx.Players.FindAsync([receiverId.Value], ct);

        if (selfEntity is null || receiverEntity is null)
        {
            return InstantMessageErrorCodeType.Offline;
        }

        MessengerMessageEntity msgEntity = new()
        {
            SenderEntityId = SelfId,
            ReceiverEntityId = receiverId.Value,
            Message = message,
            Timestamp = now,
            Delivered = false,
            SenderEntity = selfEntity,
            ReceiverEntity = receiverEntity,
        };

        dbCtx.MessengerMessages.Add(msgEntity);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        // Deliver to receiver — fire-and-forget; receiver handles if online or not
        IMessengerGrain receiverGrain = grainFactory.GetMessengerGrain(receiverId);
        LogAndForget(
            receiverGrain.ReceiveMessageAsync(
                SelfId,
                selfEntity.Name,
                selfEntity.Figure,
                message,
                now,
                msgEntity.Id,
                CancellationToken.None
            )
        );

        return null;
    }

    public Task ReceiveMessageAsync(
        PlayerId senderId,
        string senderName,
        string senderFigure,
        string message,
        DateTime timestamp,
        int messageId,
        CancellationToken ct
    )
    {
        if (_ignoredIds.Contains(senderId.Value))
        {
            return Task.CompletedTask;
        }

        int secondsSinceSent = (int)(DateTime.UtcNow - timestamp).TotalSeconds;

        IPlayerPresenceGrain presence = grainFactory.GetPlayerPresenceGrain(SelfId);
        LogAndForget(
            presence.SendComposerAsync(
                new NewConsoleMessageMessageComposer
                {
                    ChatId = senderId.Value,
                    Message = message,
                    SecondsSinceSent = secondsSinceSent,
                    MessageId = messageId.ToString(),
                    ConfirmationId = 0,
                    SenderId = senderId.Value,
                    SenderName = senderName,
                    SenderFigure = senderFigure,
                }
            )
        );

        _pendingDeliveredIds.Add(messageId);

        return Task.CompletedTask;
    }

    public async Task<List<MessageHistoryEntrySnapshot>> GetMessageHistoryAsync(
        PlayerId friendId,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        int selfIdInt = SelfId;
        int friendIdInt = friendId.Value;
        DateTime now = DateTime.UtcNow;

        List<MessengerMessageEntity> messages = await dbCtx
            .MessengerMessages.AsNoTracking()
            .Include(m => m.SenderEntity)
            .Where(m =>
                m.DeletedAt == null
                && (
                    (m.SenderEntityId == selfIdInt && m.ReceiverEntityId == friendIdInt)
                    || (m.SenderEntityId == friendIdInt && m.ReceiverEntityId == selfIdInt)
                )
            )
            .OrderByDescending(m => m.Timestamp)
            .Take(_messengerConfig.MessageHistoryLimit)
            .ToListAsync(ct);

        return messages
            .Select(m => new MessageHistoryEntrySnapshot
            {
                SenderId = m.SenderEntityId,
                SenderName = m.SenderEntity.Name,
                SenderFigure = m.SenderEntity.Figure,
                Message = m.Message,
                SecondsSinceSent = (int)(now - m.Timestamp).TotalSeconds,
                MessageId = m.Id.ToString(),
            })
            .ToList();
    }
}
