using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Vortex.Specs.Yaml;

/// <summary>
/// The document model the spec files are written from and read back into.
/// </summary>
/// <remarks>
/// Deliberately an ordered tree rather than a dictionary graph. Spec files are reviewed as diffs, so
/// key order has to be a property of the document and not of whatever the runtime hash bucket
/// happened to be — otherwise an unchanged scan produces a churning diff and nobody reads it.
/// </remarks>
public abstract class YamlNode
{
    public static YamlScalar Scalar(string? value) => new(value);

    public static YamlScalar Scalar(int value) =>
        new(value.ToString(CultureInfo.InvariantCulture)) { IsPlain = true };

    public static YamlScalar Scalar(bool value) => new(value ? "true" : "false") { IsPlain = true };

    public static YamlScalar Null() => new(null) { IsPlain = true };

    public static YamlSequence Sequence() => new();

    public static YamlSequence Sequence(IEnumerable<YamlNode> items) => new(items);

    public static YamlMapping Mapping() => new();
}

public sealed class YamlScalar(string? value) : YamlNode
{
    public string? Value { get; } = value;

    /// <summary>
    /// Set for values that are already valid unquoted YAML of the right type (numbers, booleans,
    /// null). Everything else is quoted, because a bare <c>y</c> or <c>1.0</c> is a type change.
    /// </summary>
    public bool IsPlain { get; init; }
}

public sealed class YamlSequence : YamlNode
{
    private readonly List<YamlNode> _items = [];

    public YamlSequence() { }

    public YamlSequence(IEnumerable<YamlNode> items) => _items.AddRange(items);

    public IReadOnlyList<YamlNode> Items => _items;

    public YamlSequence Add(YamlNode node)
    {
        _items.Add(node);
        return this;
    }

    public YamlSequence AddRange(IEnumerable<YamlNode> nodes)
    {
        _items.AddRange(nodes);
        return this;
    }
}

public sealed class YamlMapping : YamlNode
{
    private readonly List<KeyValuePair<string, YamlNode>> _entries = [];

    public IReadOnlyList<KeyValuePair<string, YamlNode>> Entries => _entries;

    public YamlNode? this[string key] =>
        _entries.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.Ordinal)).Value;

    public YamlMapping Set(string key, YamlNode value)
    {
        int existing = _entries.FindIndex(e => string.Equals(e.Key, key, StringComparison.Ordinal));

        if (existing >= 0)
        {
            _entries[existing] = new KeyValuePair<string, YamlNode>(key, value);
        }
        else
        {
            _entries.Add(new KeyValuePair<string, YamlNode>(key, value));
        }

        return this;
    }

    public YamlMapping Set(string key, string? value) => Set(key, YamlNode.Scalar(value));

    public YamlMapping Set(string key, int value) => Set(key, YamlNode.Scalar(value));

    public YamlMapping Set(string key, bool value) => Set(key, YamlNode.Scalar(value));

    /// <summary>Adds the entry only when there is something to add. Keeps empty keys out of files.</summary>
    public YamlMapping SetIfPresent(string key, string? value) =>
        string.IsNullOrEmpty(value) ? this : Set(key, YamlNode.Scalar(value));

    public YamlMapping SetIfAny(string key, YamlSequence sequence) =>
        sequence.Items.Count == 0 ? this : Set(key, sequence);

    public bool ContainsKey(string key) =>
        _entries.Exists(e => string.Equals(e.Key, key, StringComparison.Ordinal));

    public YamlMapping? Mapping(string key) => this[key] as YamlMapping;

    public YamlSequence? SequenceAt(string key) => this[key] as YamlSequence;

    public string? String(string key) => (this[key] as YamlScalar)?.Value;

    public int? Int(string key) =>
        int.TryParse(
            String(key),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int parsed
        )
            ? parsed
            : null;

    public bool Bool(string key, bool fallback = false) =>
        String(key) switch
        {
            "true" => true,
            "false" => false,
            _ => fallback,
        };
}
