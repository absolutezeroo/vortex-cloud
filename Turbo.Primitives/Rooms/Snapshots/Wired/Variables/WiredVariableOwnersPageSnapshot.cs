using System.Collections.Generic;
using Orleans;

namespace Turbo.Primitives.Rooms.Snapshots.Wired.Variables;

[GenerateSerializer, Immutable]
public sealed record WiredVariableOwnerEntry
{
    [Id(0)]
    public required int EntityId { get; init; }

    [Id(1)]
    public required string EntityName { get; init; }

    [Id(2)]
    public required int Value { get; init; }
}

[GenerateSerializer, Immutable]
public sealed record WiredVariableOwnersPageSnapshot
{
    [Id(0)]
    public required string VariableId { get; init; }

    [Id(1)]
    public required int TotalEntries { get; init; }

    [Id(2)]
    public required int CurrentPage { get; init; }

    [Id(3)]
    public required int Amount { get; init; }

    [Id(4)]
    public required List<WiredVariableOwnerEntry> Elements { get; init; }

    [Id(5)]
    public required int UserTypeFilter { get; init; }

    [Id(6)]
    public required int SortTypeFilter { get; init; }
}
