using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Vortex.Dashboard.API;

internal static class DashboardPageResources
{
    private const string HtmlResource = "index.html";

    private static readonly Assembly AssetAssembly = typeof(DashboardPageResources).Assembly;
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false
    );

    private static readonly ConcurrentDictionary<string, string> TextCache = new(
        StringComparer.OrdinalIgnoreCase
    );
    private static readonly ConcurrentDictionary<string, byte[]> BytesCache = new(
        StringComparer.OrdinalIgnoreCase
    );

    public static byte[] PageBytes => HtmlContentBytes.Value;
    public static byte[] OverviewHtmlBytes => GetBytes(HtmlResource);
    public static byte[] InvestigationHtmlBytes => GetBytes(HtmlResource);
    public static byte[] EconomyHtmlBytes => GetBytes(HtmlResource);
    public static byte[] RoomsHtmlBytes => GetBytes(HtmlResource);
    public static byte[] PacketsHtmlBytes => GetBytes(HtmlResource);
    public static byte[] IncidentsHtmlBytes => GetBytes(HtmlResource);
    public static byte[] AuditHtmlBytes => GetBytes(HtmlResource);

    public static byte[] HtmlBytes(string fileName) => GetBytes(fileName);

    public static string ScriptText(string fileName) => GetText(fileName);

    public static bool TryGetResourceName(string fileName, out string resourceName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            resourceName = string.Empty;
            return false;
        }

        resourceName = FindResourceName(fileName) ?? string.Empty;

        return !string.IsNullOrWhiteSpace(resourceName);
    }

    public static string GetText(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "fileName must not be null or whitespace.",
                nameof(fileName)
            );
        }

        return TextCache.GetOrAdd(fileName, LoadText);
    }

    public static byte[] GetBytes(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "fileName must not be null or whitespace.",
                nameof(fileName)
            );
        }

        return BytesCache.GetOrAdd(fileName, name => Utf8NoBom.GetBytes(GetText(name)));
    }

    private static string LoadText(string fileName)
    {
        string resourceName = GetResourceName(fileName);

        using Stream stream =
            AssetAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' was not found in '{AssetAssembly.FullName}'."
            );
        using StreamReader reader = new StreamReader(stream, Utf8NoBom);
        return reader.ReadToEnd();
    }

    private static string GetResourceName(string fileName)
    {
        string suffix = $".{fileName}";
        string? resourceName = FindResourceName(fileName);

        return resourceName
            ?? throw new InvalidOperationException(
                $"Could not locate embedded dashboard resource with suffix '{suffix}' in assembly '{AssetAssembly.GetName().Name}'."
            );
    }

    private static string? FindResourceName(string fileName)
    {
        string suffix = $".{fileName}";

        return AssetAssembly
            .GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly Lazy<string> HtmlContent = new(() => GetText(HtmlResource), true);
    private static readonly Lazy<byte[]> HtmlContentBytes = new(
        () => Utf8NoBom.GetBytes(HtmlContent.Value),
        true
    );
}
