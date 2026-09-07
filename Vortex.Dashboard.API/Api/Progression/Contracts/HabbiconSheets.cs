namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// The two spritesheets the client draws Habbicons from, and their frame sizes.
/// </summary>
/// <remarks>
/// Named for the sheets rather than "artwork" because Infrastructure already owns a HabbiconArtwork:
/// that one is the installed pack as the server reads it, this is the four values the browser needs.
/// </remarks>
/// <param name="CollectionSpritesheetUrl">Null for a pack that ships icons but no collection
/// sheet, which is a pack the page falls back to codes for on the collection row only.</param>
public sealed record HabbiconSheets(
    string SpritesheetUrl,
    string? CollectionSpritesheetUrl,
    int FrameSize,
    int CollectionIconSize
);
