using Orleans;

namespace Turbo.Primitives.Orleans.Snapshots.Navigator;

[GenerateSerializer, Immutable]
public record NavigatorFlatCategorySnapshot
{
    [Id(0)] public required int Id { get; init; }
    [Id(1)] public required string Name { get; init; }
    [Id(2)] public required int MinRank { get; init; }
    [Id(3)] public required bool Visible { get; init; }
    [Id(4)] public required bool Automatic { get; init; }
    [Id(5)] public required string AutomaticCategory { get; init; }
    [Id(6)] public required string GlobalCategory { get; init; }
    [Id(7)] public required bool StaffOnly { get; init; }
}
