using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Options;
using Vortex.Observability.Configuration;

namespace Vortex.Dashboard.API.Infrastructure;

/// <summary>
/// Single source of truth for building dashboard asset URLs — furniture/catalog icons, avatar heads
/// (player inspector) and guild badges — from the configurable templates in
/// <see cref="ObservabilityConfig"/>. Every dashboard surface that renders an asset goes through here
/// instead of re-deriving URLs, and <see cref="ImgSrcOrigins"/> feeds the dashboard CSP so those
/// images are allowed to load. A template left empty yields a <c>null</c> URL (the UI shows a generic
/// fallback icon) — nothing is ever fabricated.
/// </summary>
internal sealed class DashboardAssetUrls(IOptions<ObservabilityConfig> options)
{
    private readonly ObservabilityConfig _config = options.Value;

    /// <summary>
    /// Furniture icon by definition name (<c>{name}</c>). Habbo multi-quantity items are named
    /// <c>basename*count</c> (e.g. <c>waterbowl*4</c>), but the icon asset is the base name
    /// (<c>waterbowl_icon.png</c>) — so the <c>*count</c> suffix is dropped before resolving. Clean
    /// names are unaffected.
    /// </summary>
    public string? FurniIcon(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        int star = name.IndexOf('*', StringComparison.Ordinal);
        string baseName = star >= 0 ? name[..star] : name;

        return Build(_config.FurniIconUrlTemplate, "{name}", baseName);
    }

    /// <summary>Catalog page icon by icon id (<c>{id}</c>).</summary>
    public string? CatalogIcon(int iconId) =>
        Build(
            _config.CatalogIconUrlTemplate,
            "{id}",
            iconId.ToString(CultureInfo.InvariantCulture)
        );

    /// <summary>Avatar image (head) rendered from a player's figure string (<c>{figure}</c>).</summary>
    public string? AvatarImage(string? figure) =>
        Build(_config.AvatarImageUrlTemplate, "{figure}", figure);

    /// <summary>Guild/group badge rendered from its badge code (<c>{badge}</c>).</summary>
    public string? GroupBadge(string? badge) =>
        Build(_config.GroupBadgeUrlTemplate, "{badge}", badge);

    /// <summary>
    /// The raw targeted-offer image template (or null when unset) so the admin form can show the
    /// configured base and let the operator supply just a filename with a live preview, instead of
    /// pasting a whole URL. Storage stays a full URL on the wire — this only drives the form.
    /// </summary>
    public string? TargetedOfferImageTemplate =>
        string.IsNullOrWhiteSpace(_config.TargetedOfferImageUrlTemplate)
            ? null
            : _config.TargetedOfferImageUrlTemplate;

    /// <summary>
    /// Distinct http(s) host origins of every configured template, for the dashboard CSP
    /// <c>img-src</c>. Without this the browser would block cross-origin asset images.
    /// </summary>
    public IReadOnlyList<string> ImgSrcOrigins =>
        new[]
        {
            _config.FurniIconUrlTemplate,
            _config.CatalogIconUrlTemplate,
            _config.TargetedOfferImageUrlTemplate,
            _config.AvatarImageUrlTemplate,
            _config.GroupBadgeUrlTemplate,
        }
            .Select(OriginOf)
            .Where(origin => origin is not null)
            .Select(origin => origin!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static string? Build(string? template, string placeholder, string? value)
    {
        if (string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Escape the substituted value: placeholders sit in both path segments (icon name) and query
        // values (avatar figure), and a raw '&'/space would break the URL. Figure/badge/name chars
        // (alphanumeric, '.', '-', '_') are unreserved so this is a no-op for the common case.
        return template.Replace(placeholder, Uri.EscapeDataString(value), StringComparison.Ordinal);
    }

    /// <summary>Host origin of a template, found by substituting a benign probe for every known
    /// placeholder so the URL parses, then taking its authority. Null if the template is empty or
    /// not an absolute http(s) URL.</summary>
    private static string? OriginOf(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        string probe = template
            .Replace("{name}", "x", StringComparison.Ordinal)
            .Replace("{id}", "1", StringComparison.Ordinal)
            .Replace("{file}", "x", StringComparison.Ordinal)
            .Replace("{figure}", "x", StringComparison.Ordinal)
            .Replace("{badge}", "x", StringComparison.Ordinal);

        return
            Uri.TryCreate(probe, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.GetLeftPart(UriPartial.Authority)
            : null;
    }
}
