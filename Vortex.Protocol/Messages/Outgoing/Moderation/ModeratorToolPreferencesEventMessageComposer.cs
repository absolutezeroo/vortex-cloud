using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Moderation;

/// <summary>
/// Restores where the moderator last left their mod-tool window. Echoed back at login from whatever
/// the client last reported via ModToolPreferencesMessage.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ModeratorToolPreferencesEventMessageComposer : IComposer
{
    [Id(0)]
    public required int WindowX { get; init; }

    [Id(1)]
    public required int WindowY { get; init; }

    [Id(2)]
    public required int WindowWidth { get; init; }

    [Id(3)]
    public required int WindowHeight { get; init; }
}
