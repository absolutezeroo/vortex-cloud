using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Vortex.Specs.Persistence;

/// <summary>
/// Hands out one file path per spec, and refuses to give two specs the same one.
/// </summary>
/// <remarks>
/// Habbo's own vocabulary contains names that differ only in case, and Windows file systems do not.
/// Left alone, two specs write to one file, the second silently overwrites the first, and a run that
/// changed nothing still reports files as rewritten because the pair keeps trading places. The
/// second claimant gets a suffixed path and the collision is reported rather than absorbed.
/// </remarks>
public sealed class SpecPathAllocator
{
    private readonly Dictionary<string, string> _claimed = new(StringComparer.Ordinal);
    private readonly List<string> _collisions = [];

    public IReadOnlyList<string> Collisions => _collisions;

    public string Allocate(params string[] segments)
    {
        string path = Path.Combine(segments);
        string key = path.Replace('\\', '/').ToLowerInvariant();

        if (!_claimed.TryGetValue(key, out string? owner))
        {
            _claimed[key] = path;
            return path;
        }

        if (string.Equals(owner, path, StringComparison.Ordinal))
        {
            // The same spec asking twice. Nothing to disambiguate.
            return path;
        }

        string directory = Path.GetDirectoryName(path) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        string disambiguated = Path.Combine(directory, $"{name}~{ShortHash(path)}{extension}");

        _collisions.Add(
            $"'{path.Replace('\\', '/')}' collides with '{owner.Replace('\\', '/')}' on a "
                + $"case-insensitive file system; written as '{disambiguated.Replace('\\', '/')}'"
        );

        _claimed[disambiguated.Replace('\\', '/').ToLowerInvariant()] = disambiguated;

        return disambiguated;
    }

    private static string ShortHash(string value)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        StringBuilder builder = new(6);

        for (int i = 0; i < 3; i++)
        {
            builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
