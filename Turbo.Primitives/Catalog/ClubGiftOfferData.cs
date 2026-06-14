using Orleans;

namespace Turbo.Primitives.Catalog;

[GenerateSerializer, Immutable]
public sealed record ClubGiftOfferData
{
    [Id(0)] public required int OfferId { get; init; }
    [Id(1)] public required bool IsVip { get; init; }
    [Id(2)] public required int DaysRequired { get; init; }
    [Id(3)] public required bool IsSelectable { get; init; }
}
