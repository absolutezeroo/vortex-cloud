namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One promo image.
/// </summary>
/// <param name="ThumbUrl">The .thumb.png beside it when there is one, otherwise the image itself --
/// the variants are folded in here rather than listed as images of their own.</param>
public sealed record TargetedOfferImage(string File, string Url, string ThumbUrl);
