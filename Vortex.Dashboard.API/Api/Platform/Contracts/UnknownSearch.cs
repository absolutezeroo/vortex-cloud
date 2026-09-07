namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// The term was neither a number nor a correlation id.
/// </summary>
/// <remarks>
/// An answer rather than an empty result: on an investigation screen, "this is not something I can
/// look up" is information, and a guess would be worse than silence.
/// </remarks>
public sealed record UnknownSearch(string Term, string Hint) : DirectorySearch;
