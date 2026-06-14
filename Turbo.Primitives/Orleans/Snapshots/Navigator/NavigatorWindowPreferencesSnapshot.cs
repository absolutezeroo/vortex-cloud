using Orleans;

namespace Turbo.Primitives.Orleans.Snapshots.Navigator;

[GenerateSerializer, Immutable]
public record NavigatorWindowPreferencesSnapshot
{
    [Id(0)]
    public int WindowX { get; init; } = 427;

    [Id(1)]
    public int WindowY { get; init; } = 41;

    [Id(2)]
    public int WindowWidth { get; init; } = 425;

    [Id(3)]
    public int WindowHeight { get; init; } = 400;

    [Id(4)]
    public bool LeftPaneHidden { get; init; } = false;

    [Id(5)]
    public int ResultsMode { get; init; } = 1;
}
