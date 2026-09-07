using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>
/// One language's text, saved in the same request as the article it belongs to.
/// </summary>
/// <remarks>
/// Together rather than as a second call because they are one act: somebody writes an article. Two
/// endpoints meant two buttons both labelled "Save", and no way for a writer to know which one kept
/// their paragraph.
/// <para>
/// <paramref name="Body"/> is the block array as JSON; it is validated against the closed block
/// vocabulary before storage, so an unknown block type or a <c>javascript:</c> link is refused here
/// rather than rendered on the public site.
/// </para>
/// </remarks>
public sealed record ArticleTranslationPayload(
    string Lang,
    string Title,
    string Summary,
    string Body,
    string HeaderImage,
    string Thumbnail
);
