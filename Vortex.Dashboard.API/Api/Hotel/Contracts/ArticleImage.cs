namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One picture, as a path under the asset host's <c>c_images</c> tree.
/// </summary>
/// <param name="Thumb">The <c>.thumb</c> variant when one exists, otherwise the image itself.</param>
public sealed record ArticleImage(string Path, string Thumb);
