using Orleans;

namespace Turbo.Primitives.Orleans.Snapshots.Players;

[GenerateSerializer, Immutable]
public sealed record PlayerWiredPreferencesSnapshot
{
    [Id(0)]
    public required bool WiredMenuButton { get; init; }

    [Id(1)]
    public required bool WiredInspectButton { get; init; }

    [Id(2)]
    public required bool PlayTestMode { get; init; }

    [Id(3)]
    public required bool WiredWhisperDisabled { get; init; }

    [Id(4)]
    public required bool ShowAllNotifications { get; init; }

    [Id(5)]
    public required string UiStyle { get; init; }
}
