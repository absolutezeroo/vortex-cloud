using System;
using System.Text.Json;

namespace Vortex.Database.Entities.Web;

/// <summary>
/// The closed block vocabulary an article body is written in, and the only place that decides whether
/// a body, an image path or a link is acceptable.
/// </summary>
/// <remarks>
/// An article body is a JSON array of typed blocks, never HTML. That is a security decision, not a
/// stylistic one: with no markup crossing the column there is no sanitiser to configure, no sanitiser
/// to get wrong, and nothing a writer can paste that turns into script on the public site.
/// <para>
/// The rules live next to the entity rather than in the host that writes today, so a second writer
/// cannot end up with a second, subtly different idea of what is valid. The public read trusts what
/// this class let through.
/// </para>
/// </remarks>
public static class WebArticleBody
{
    /// <summary>A paragraph. Requires <c>text</c>.</summary>
    public const string TypeParagraph = "p";

    /// <summary>A sub-heading inside the article. Requires <c>text</c>.</summary>
    public const string TypeHeading = "h";

    /// <summary>A full-width picture. Requires <c>src</c>; <c>caption</c> is optional.</summary>
    public const string TypeImage = "img";

    /// <summary>A button. Requires <c>label</c> and <c>href</c>.</summary>
    public const string TypeButton = "btn";

    /// <summary>A separator. Carries nothing.</summary>
    public const string TypeRule = "hr";

    public const string ErrorBody = "invalid_body";
    public const string ErrorHref = "invalid_href";
    public const string ErrorImage = "invalid_image";

    private const int MAX_BLOCKS = 200;

    /// <summary>
    /// Whether <paramref name="json"/> is an array of well-formed blocks. <paramref name="error"/>
    /// names which rule failed, using the same codes the HTTP layer returns.
    /// </summary>
    public static bool TryValidate(string? json, out string error)
    {
        error = ErrorBody;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return false;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            // A bounded body is not a policy about writing style: an unbounded array is a way to
            // make one row cost megabytes and every feed read pay for it.
            if (document.RootElement.GetArrayLength() > MAX_BLOCKS)
            {
                return false;
            }

            foreach (JsonElement block in document.RootElement.EnumerateArray())
            {
                if (!TryValidateBlock(block, out error))
                {
                    return false;
                }
            }
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Whether a path is a usable reference into the asset host's <c>c_images</c> tree — a relative
    /// path, no scheme, no traversal. Empty is allowed: an article may have no picture.
    /// </summary>
    public static bool IsAllowedImagePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return true;
        }

        // A leading slash is how the site's own mock and its `IMAGES` prefix already write these
        // ("/web_promo/x.png"); "//host/x" would be a protocol-relative URL, which is not a path.
        return path[0] == '/'
            && !path.StartsWith("//", StringComparison.Ordinal)
            && !path.Contains("..", StringComparison.Ordinal)
            && !path.Contains(':', StringComparison.Ordinal)
            && !path.Contains('\\', StringComparison.Ordinal)
            && path.Length <= 512;
    }

    /// <summary>
    /// Whether a button may point there. The site routes on the hash, so an in-site link is
    /// <c>#/…</c>; <c>/…</c> reaches the server's own routes; http(s) leaves the site. Everything
    /// else — <c>javascript:</c> above all — is refused, because a button's href is the one field an
    /// editor controls that the browser will execute.
    /// </summary>
    public static bool IsAllowedHref(string? href)
    {
        if (string.IsNullOrWhiteSpace(href) || href.Length > 2048)
        {
            return false;
        }

        if (href.StartsWith("#/", StringComparison.Ordinal))
        {
            return true;
        }

        if (href.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        return href[0] == '/'
            || href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryValidateBlock(JsonElement block, out string error)
    {
        error = ErrorBody;

        if (
            block.ValueKind != JsonValueKind.Object
            || !block.TryGetProperty("type", out JsonElement typeElement)
            || typeElement.ValueKind != JsonValueKind.String
        )
        {
            return false;
        }

        switch (typeElement.GetString())
        {
            case TypeParagraph:
            case TypeHeading:
                return RequireText(block, "text", out error);

            case TypeImage:
                if (!RequireText(block, "src", out error))
                {
                    return false;
                }

                if (!IsAllowedImagePath(ReadString(block, "src")))
                {
                    error = ErrorImage;
                    return false;
                }

                error = string.Empty;
                return true;

            case TypeButton:
                if (!RequireText(block, "label", out error))
                {
                    return false;
                }

                if (!IsAllowedHref(ReadString(block, "href")))
                {
                    error = ErrorHref;
                    return false;
                }

                error = string.Empty;
                return true;

            case TypeRule:
                error = string.Empty;
                return true;

            default:
                // An unknown type is a body the reader would silently drop. Refusing it here is what
                // stops an article being saved half-visible.
                return false;
        }
    }

    private static bool RequireText(JsonElement block, string property, out string error)
    {
        error = ErrorBody;

        string? value = ReadString(block, property);

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static string? ReadString(JsonElement block, string property) =>
        block.TryGetProperty(property, out JsonElement element)
        && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
}
