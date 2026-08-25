using System.Text.Json;

namespace Vortex.Furniture;

internal sealed class ExtraDataReader
{
    private readonly JsonElement _root;

    public ExtraDataReader(string? extraData)
    {
        if (string.IsNullOrWhiteSpace(extraData))
        {
            _root = default;

            return;
        }

        try
        {
            _root = JsonDocument.Parse(extraData).RootElement;
        }
        catch (JsonException)
        {
            // `extra_data` is a free string column, written by imports, admin edits and one-off SQL
            // as well as by us. A row that is not valid JSON used to throw here, and here is inside
            // a room's activation -- so one malformed value stopped a whole room from opening. An
            // item with no readable state is a defaulted item, which is bad; a room nobody can enter
            // is worse.
            _root = default;
        }
    }

    public bool TryGet(string name, out JsonElement element)
    {
        if (_root.ValueKind != JsonValueKind.Object)
        {
            element = default;

            return false;
        }

        if (!_root.TryGetProperty(name, out element))
        {
            return false;
        }

        // A section present but null reads as found, and every caller then deserializes it into a
        // null it does not expect. `{"stuff":null}` is a real shape -- a section deleted by writing
        // null rather than by removing the key -- so absent is the honest answer.
        if (element.ValueKind == JsonValueKind.Null)
        {
            element = default;

            return false;
        }

        return true;
    }
}
