using System.Text.Json;
using System.Text.Json.Nodes;

namespace Vortex.Furniture;

internal sealed class ExtraDataWriter
{
    private readonly JsonObject _root;

    public ExtraDataWriter(string? extraData)
    {
        if (string.IsNullOrWhiteSpace(extraData))
        {
            _root = [];

            return;
        }

        try
        {
            _root = JsonNode.Parse(extraData) as JsonObject ?? [];
        }
        catch (JsonException)
        {
            // Same reasoning as ExtraDataReader: a malformed row is started over rather than thrown
            // on. What was in it was unreadable anyway.
            _root = [];
        }
    }

    public string UpdateSection<TSection>(string name, TSection section)
    {
        _root[name] = JsonSerializer.SerializeToNode(section, OPTIONS);

        return _root.ToJsonString(OPTIONS);
    }

    public string DeleteSection(string name)
    {
        _root.Remove(name);

        return _root.ToJsonString(OPTIONS);
    }

    public string ToJsonString() => _root.ToJsonString(OPTIONS);

    private static readonly JsonSerializerOptions OPTIONS = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };
}
