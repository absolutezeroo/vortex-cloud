namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>One bundle of mint stamps, priced in silver.</summary>
public sealed record MintTokenOfferRow(
    int Id,
    string ProductCode,
    int SilverPrice,
    int AmountTokens,
    bool Enabled,
    int SortOrder
);
