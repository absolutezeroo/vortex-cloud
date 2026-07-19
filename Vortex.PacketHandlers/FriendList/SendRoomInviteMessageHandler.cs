using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Logging.Extensions;
using Vortex.Messages.Registry;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Incoming.FriendList;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.PacketHandlers.FriendList;

public class SendRoomInviteMessageHandler(
    IGrainFactory grainFactory,
    ILogger<SendRoomInviteMessageHandler> logger
) : IMessageHandler<SendRoomInviteMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<SendRoomInviteMessageHandler> _logger = logger;

    public async ValueTask HandleAsync(
        SendRoomInviteMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.FriendIds.Count == 0)
        {
            return;
        }

        // Fire-and-forget room invites to each friend's grain, in parallel
        List<Task> pending = new(message.FriendIds.Count);

        foreach (int friendId in message.FriendIds)
        {
            IMessengerGrain friendGrain = _grainFactory.GetMessengerGrain(PlayerId.Parse(friendId));
            pending.Add(
                friendGrain.ReceiveRoomInviteAsync(
                    ctx.PlayerId,
                    message.Message,
                    CancellationToken.None
                )
            );
        }

        Task.WhenAll(pending)
            .LogAndForget(
                _logger,
                "Failed to deliver one or more room invites from player {PlayerId}",
                ctx.PlayerId
            );

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
