using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// The promo images on disk, so the form shows a gallery instead of asking for a filename nobody
/// can know.
/// </summary>
/// <remarks>Empty when no asset root or image template is configured.</remarks>
public sealed record TargetedOfferImageList(int Count, IReadOnlyList<TargetedOfferImage> Items);
