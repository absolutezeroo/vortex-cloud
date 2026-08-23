using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Poll;

/// <summary>
/// Tells the client the poll it asked for is not available, so it closes the dialog. The body is
/// deliberately empty: the client's parser reads nothing at all and its handler substitutes its own
/// localized "???" strings, so any field written here would be silently ignored.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record PollErrorEventMessageComposer : IComposer { }
