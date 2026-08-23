using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

/// <summary>
/// A chest settings screen saved.
/// </summary>
/// <remarks>
/// The flag says which of the two screens is being answered — the settings or the notification
/// preferences — because both save to the same chest and each closes only on its own reply.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record WiredChestUpdateSuccessMessageComposer : IComposer
{
    [Id(0)]
    public required int ChestId { get; init; }

    [Id(1)]
    public required bool IsNotificationPreferences { get; init; }
}
