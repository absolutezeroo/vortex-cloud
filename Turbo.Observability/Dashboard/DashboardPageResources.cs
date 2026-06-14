using System;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text;

namespace Turbo.Observability.Dashboard;

internal static class DashboardPageResources
{
    private const string HtmlResource = "DashboardPage.html";
    private const string CssResource = "DashboardPage.css";
    private const string ScriptResource = "DashboardPage.js";

    private static readonly Assembly AssetAssembly = typeof(DashboardPageResources).Assembly;
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static readonly Lazy<string> HtmlContent = new(() => LoadText(HtmlResource), true);
    private static readonly Lazy<string> CssContent = new(() => LoadText(CssResource), true);
    private static readonly Lazy<string> ScriptContent = new(() => LoadText(ScriptResource), true);

    private static readonly Lazy<byte[]> HtmlContentBytes = new(() => Utf8NoBom.GetBytes(HtmlContent.Value), true);
    private static readonly Lazy<byte[]> CssContentBytes = new(() => Utf8NoBom.GetBytes(CssContent.Value), true);
    private static readonly Lazy<byte[]> ScriptContentBytes = new(() => Utf8NoBom.GetBytes(ScriptContent.Value), true);

    public static byte[] PageBytes => HtmlContentBytes.Value;
    public static byte[] CssBytes => CssContentBytes.Value;
    public static byte[] ScriptBytes => ScriptContentBytes.Value;

    private static string LoadText(string fileName)
    {
        var resourceName = GetResourceName(fileName);
        using var stream = AssetAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' was not found in '{AssetAssembly.FullName}'."
            );
        using var reader = new StreamReader(stream, Utf8NoBom);
        return reader.ReadToEnd();
    }

    private static string GetResourceName(string fileName)
    {
        var suffix = $".{fileName}";
        return AssetAssembly
            .GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Could not locate embedded dashboard resource with suffix '{suffix}' in assembly '{AssetAssembly.GetName().Name}'."
            );
    }
}
