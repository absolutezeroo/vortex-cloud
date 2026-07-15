using System.Collections.Generic;
using Orleans;
using Turbo.Primitives.Rooms.Enums.Wired;

namespace Turbo.Primitives.Rooms.Snapshots.Wired.Variables;

[GenerateSerializer, Immutable]
public sealed record WiredPermanentVariablesSnapshot
{
    [Id(0)]
    public required WiredVariableTargetType EntityType { get; init; }

    [Id(1)]
    public required int EntityId { get; init; }

    [Id(2)]
    public required string EntityName { get; init; }

    [Id(3)]
    public required string EntityFigure { get; init; }

    [Id(4)]
    public int? OwnerId { get; init; }

    [Id(5)]
    public string? OwnerName { get; init; }

    [Id(6)]
    public string? OwnerFigure { get; init; }

    [Id(7)]
    public required List<(string VariableId, int Value)> Variables { get; init; }
}
