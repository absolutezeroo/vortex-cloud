using System.Text.Json;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Reads a property out of a stored JSON blob, tolerating the two shapes the emulator has written
/// over the years: a real number or boolean, and the same value as a string.
/// </summary>
/// <remarks>
/// The tolerance is the reason this exists rather than <c>GetInt32</c> at each call site. Extra
/// params and purchase details are hand-written and machine-written JSON of several vintages, and a
/// read that throws on <c>"3"</c> where it expected <c>3</c> takes a whole admin page down over one
/// old row. Absent, wrong-typed and unparseable all answer null, and the caller decides.
/// </remarks>
internal static class JsonValues
{
    public static int? Int(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int parsed))
        {
            return parsed;
        }

        if (
            property.ValueKind == JsonValueKind.String
            && int.TryParse(property.GetString(), out parsed)
        )
        {
            return parsed;
        }

        return null;
    }

    public static bool? Bool(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
        {
            return property.GetBoolean();
        }

        if (
            property.ValueKind == JsonValueKind.String
            && bool.TryParse(property.GetString(), out bool parsed)
        )
        {
            return parsed;
        }

        return null;
    }
}
