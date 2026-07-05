using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Moderator;
using Turbo.Primitives.Messages.Outgoing.Moderation;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Players;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Moderator;

public class GetCfhChatlogMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    ICfhTicketService tickets
) : IMessageHandler<GetCfhChatlogMessage>
{
    public async ValueTask HandleAsync(
        GetCfhChatlogMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.CallId <= 0)
        {
            return;
        }

        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.HasAny(Capabilities.Moderation.Chatlogs, Capabilities.Moderation.Cfh))
        {
            return;
        }

        CfhTicketSummary? ticket = await tickets
            .GetTicketAsync(message.CallId, ct)
            .ConfigureAwait(false);

        if (ticket is null)
        {
            return;
        }

        CfhTicketEvidenceSnapshot? evidence = await tickets
            .GetTicketEvidenceAsync(message.CallId, ct)
            .ConfigureAwait(false);

        if (evidence is null)
        {
            return;
        }

        string roomName = string.Empty;

        if (evidence.Value.RoomId is int roomId && roomId > 0)
        {
            RoomSnapshot roomSnapshot = await grainFactory
                .GetRoomGrain(roomId)
                .GetSnapshotAsync()
                .ConfigureAwait(false);
            roomName = roomSnapshot.Name;
        }

        Dictionary<int, string> chatterNames = [];

        foreach (CfhEvidenceLine line in evidence.Value.Evidence)
        {
            if (chatterNames.ContainsKey(line.UserId))
            {
                continue;
            }

            PlayerSummarySnapshot summary = await grainFactory
                .GetPlayerGrain(line.UserId)
                .GetSummaryAsync(ct)
                .ConfigureAwait(false);
            chatterNames[line.UserId] = summary.Name;
        }

        ImmutableArray<ChatlogRecordSnapshot> records = evidence
            .Value.Evidence.Select(l => new ChatlogRecordSnapshot
            {
                TimeStampUtc = evidence.Value.ReportedAtUtc,
                ChatterId = l.UserId,
                ChatterName = chatterNames.GetValueOrDefault(l.UserId, string.Empty),
                Message = l.Text,
            })
            .ToImmutableArray();

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new CfhChatlogEventMessageComposer
                {
                    CallId = message.CallId,
                    CallerUserId = ticket.Value.ReporterPlayerId,
                    ReportedUserId = ticket.Value.ReportedPlayerId,
                    ChatRecordId = message.CallId,
                    ChatRecord = new ChatlogBlockSnapshot
                    {
                        RoomId = evidence.Value.RoomId ?? 0,
                        RoomName = roomName,
                        Records = records,
                    },
                }
            )
            .ConfigureAwait(false);
    }
}
