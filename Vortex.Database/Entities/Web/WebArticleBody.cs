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
/// Bold, italics and links do not weaken that. A formatted <c>text</c> is an array of runs carrying
/// flags — <c>{ "t": "gras", "b": true }</c> — which the reader turns into elements. The writer gets
/// a real editor; the column still holds no markup.
/// </para>
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

    /// <summary>
    /// A bulleted or numbered list. Requires a non-empty <c>items</c>, each one a <c>text</c>;
    /// <c>ordered</c> chooses the marker.
    /// </summary>
    public const string TypeList = "list";

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
    private const int MAX_RUNS = 400;
    private const int MAX_LIST_ITEMS = 100;

    // The inline marks a `text` may carry, and nothing else. The writing surface in the dashboard
    // can only produce these; the list is repeated here because the server, not the editor, is what
    // decides what the column holds.
    private static readonly string[] MARK_KEYS = ["b", "i", "u", "s"];

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
                return block.TryGetProperty("text", out JsonElement text)
                    && TryValidateText(text, out error);

            case TypeList:
                return TryValidateList(block, out error);

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

    /// <summary>
    /// A <c>text</c> field, in either of its two shapes: a plain string, or the array of formatted
    /// runs the dashboard's writing surface produces — <c>{ "t": "…", "b": true, "href": "…" }</c>.
    /// </summary>
    /// <remarks>
    /// Runs are how the body carries bold, italics and links WITHOUT carrying markup. The formatting
    /// is data the reader turns into elements, so nothing a writer can type or paste arrives as
    /// something a browser will execute — the property that made this column typed JSON in the first
    /// place, kept while the writer gained a real editor.
    /// </remarks>
    private static bool TryValidateText(JsonElement text, out string error)
    {
        error = ErrorBody;

        if (text.ValueKind == JsonValueKind.String)
        {
            if (string.IsNullOrWhiteSpace(text.GetString()))
            {
                return false;
            }

            error = string.Empty;
            return true;
        }

        if (text.ValueKind != JsonValueKind.Array || text.GetArrayLength() > MAX_RUNS)
        {
            return false;
        }

        bool anyContent = false;

        foreach (JsonElement run in text.EnumerateArray())
        {
            if (run.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            string? value = ReadString(run, "t");

            if (value is null)
            {
                return false;
            }

            anyContent |= !string.IsNullOrWhiteSpace(value);

            foreach (string key in MARK_KEYS)
            {
                if (
                    run.TryGetProperty(key, out JsonElement mark)
                    && mark.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                )
                {
                    return false;
                }
            }

            // A run's href reaches the reader as an anchor, so it is the same decision a button's
            // href is and gets the same answer. Absent is fine — most runs are not links.
            if (
                run.TryGetProperty("href", out JsonElement href)
                && href.ValueKind != JsonValueKind.Null
            )
            {
                if (href.ValueKind != JsonValueKind.String || !IsAllowedHref(href.GetString()))
                {
                    error = ErrorHref;
                    return false;
                }
            }
        }

        if (!anyContent)
        {
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateList(JsonElement block, out string error)
    {
        error = ErrorBody;

        if (
            !block.TryGetProperty("items", out JsonElement items)
            || items.ValueKind != JsonValueKind.Array
            || items.GetArrayLength() == 0
            || items.GetArrayLength() > MAX_LIST_ITEMS
        )
        {
            return false;
        }

        if (
            block.TryGetProperty("ordered", out JsonElement ordered)
            && ordered.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
        )
        {
            return false;
        }

        foreach (JsonElement item in items.EnumerateArray())
        {
            if (!TryValidateText(item, out error))
            {
                return false;
            }
        }

        error = string.Empty;
        return true;
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
