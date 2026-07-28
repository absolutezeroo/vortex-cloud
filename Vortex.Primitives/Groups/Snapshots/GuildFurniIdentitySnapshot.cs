using Orleans;

namespace Vortex.Primitives.Groups.Snapshots;

/// <summary>
/// Everything guild-customized furni needs stamped into its stuff data: the badge asset to draw and
/// the two recolour slots, already resolved from palette ids to bare hex.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GuildFurniIdentitySnapshot
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required string BadgeCode { get; init; }

    [Id(2)]
    public required string ColorOneHex { get; init; }

    [Id(3)]
    public required string ColorTwoHex { get; init; }
}
