namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// The raw {id} URL pattern for a catalogue icon.
/// </summary>
/// <remarks>
/// Sent rather than a list: there is no manifest of which icon ids exist on the asset host, so
/// "does this id have a real icon" can only be answered by letting the browser try to load it.
/// Null when no template is configured, and the picker then has nothing to probe.
/// </remarks>
public sealed record CatalogIconTemplate(string? Template);
