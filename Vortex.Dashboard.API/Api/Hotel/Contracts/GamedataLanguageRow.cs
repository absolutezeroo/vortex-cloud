namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One declared language.
/// </summary>
/// <param name="HasFile">Whether the texts file it points at exists. A language declared with no
/// file is a language that loads nothing.</param>
/// <param name="Command">What a player types to switch to it. Shown because a feature nobody can
/// name is a feature nobody uses.</param>
public sealed record GamedataLanguageRow(
    string Id,
    string Code,
    string Name,
    string Url,
    bool HasFile,
    string Command
);
