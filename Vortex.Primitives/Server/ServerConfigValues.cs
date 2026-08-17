using System.Collections.Generic;
using System.Globalization;

namespace Vortex.Primitives.Server;

/// <summary>
/// Typed readers over a config snapshot returned by <c>IServerConfigGrain.GetManyAsync</c> /
/// <c>GetAllAsync</c>. The parsing rules are byte-for-byte those of the grain's own typed getters
/// (invariant-culture doubles, <c>bool.TryParse</c>, fallback on missing or unparseable), so
/// resolving a settings group through one batched call reads identically to N single calls.
/// </summary>
public static class ServerConfigValues
{
    public static int GetInt(
        IReadOnlyDictionary<string, string> values,
        string key,
        int fallback
    ) =>
        values.TryGetValue(key, out string? value) && int.TryParse(value, out int parsed)
            ? parsed
            : fallback;

    public static bool GetBool(
        IReadOnlyDictionary<string, string> values,
        string key,
        bool fallback
    ) =>
        values.TryGetValue(key, out string? value) && bool.TryParse(value, out bool parsed)
            ? parsed
            : fallback;

    public static double GetDouble(
        IReadOnlyDictionary<string, string> values,
        string key,
        double fallback
    ) =>
        values.TryGetValue(key, out string? value)
        && double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double parsed
        )
            ? parsed
            : fallback;
}
