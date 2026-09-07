using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// The colours the client can actually render a mystery box in.
/// </summary>
/// <remarks>
/// Sent as a list so the admin picks one instead of typing a string, which would silently make a
/// box unpairable with its key.
/// </remarks>
public sealed record MysteryBoxColorOptions(int Count, IReadOnlyList<string> Items);
