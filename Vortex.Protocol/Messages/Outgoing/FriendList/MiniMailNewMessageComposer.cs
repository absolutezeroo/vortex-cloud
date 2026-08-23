using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.FriendList;

[GenerateSerializer, Immutable]
public sealed record MiniMailNewMessageComposer : IComposer;
