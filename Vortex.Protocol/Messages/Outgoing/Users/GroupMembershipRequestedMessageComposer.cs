using Orleans;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Users;

/// <summary>
/// Tells a guild's admins that somebody asked to join. AS3-verified against
/// <c>GroupMembershipRequestedMessageParser</c>: <c>groupId</c> followed by one <c>MemberData</c>
/// block. <c>GuildMembersWindowCtrl.onMembershipRequested</c> reloads the current member page when
/// the open window is showing this guild.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GroupMembershipRequestedMessageComposer : IComposer
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required GroupMemberSnapshot Requester { get; init; }
}
