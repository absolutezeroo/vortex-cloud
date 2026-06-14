using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Navigator.Enums;

namespace Turbo.Primitives.Orleans.Snapshots.Navigator;

[GenerateSerializer, Immutable]
public record NavigatorTopLevelContextSnapshot
{
    [Id(0)]
    public required string SearchCode { get; init; }

    [Id(1)]
    public required ImmutableArray<NavigatorQuickLinkSnapshot> QuickLinks { get; init; }

    [Id(2)]
    public NavigatorQueryType QueryType { get; init; } = NavigatorQueryType.AllRooms;
}
