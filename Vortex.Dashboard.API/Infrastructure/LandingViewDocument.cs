using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Vortex.Dashboard.API.Api.Hotel.Contracts;

namespace Vortex.Dashboard.API.Infrastructure;

/// <summary>
/// The hotel view's flat <c>landing.view.*</c> keys, read into a shape with names on it and written
/// back out again.
/// </summary>
/// <remarks>
/// Both directions live here, deliberately. The formats are trivial on their own -- split on
/// <c>;</c>, split on <c>,</c> -- and the bug is never the split, it is a reader and a writer that
/// disagree about one field. One type, one round-trip test, and the disagreement has nowhere to
/// happen.
/// <para>
/// <b>Composition is total.</b> <see cref="Compose"/> returns every key the configuration implies and
/// nothing else, so a caller writes exactly that set and deletes every other owned key. That is what
/// makes a removed campaign actually disappear instead of lingering in the file, and it is why
/// anything the page does not model still has to travel through <see cref="HotelViewConfig.Extras"/>
/// rather than being ignored.
/// </para>
/// </remarks>
internal static class LandingViewDocument
{
    /// <summary>
    /// <c>landing_view*</c> is not a typo for <c>landing.view.*</c>: three keys really are spelled
    /// that way, and the client reads none of them. They are owned here so the page can show them for
    /// what they are and delete them, rather than leaving them to be copied into the next dump.
    /// </summary>
    private const string DeadPrefix = "landing_view";

    private const string SlotPrefix = LandingViewVocabulary.Prefix + "dynamic.slot.";

    /// <summary>The client's own substitution syntax inside a stored URL.</summary>
    private static readonly Regex Placeholder = new(
        @"\$\{([^}]+)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    /// <summary>
    /// Keys the client reads that this page models no field for -- fixed widgets, the quest strip,
    /// the community goal. Prefixes, because most are built by concatenation in the client.
    /// </summary>
    private static readonly string[] KnownExtras =
    [
        "landing.view.bonus.rare.",
        "landing.view.catalog.",
        "landing.view.community.",
        "landing.view.community_catalog_button.",
        "landing.view.communitygoalhof.",
        "landing.view.competition.",
        "landing.view.concurrentusers.",
        "landing.view.hotel.domain",
        "landing.view.new_identity_",
        "landing.view.pageexpiry",
        "landing.view.quest.",
        "landing.view.roomhopper.",
        "landing.view.vote_one_button.",
        "landing.view.vote_two_button.",
    ];

    /// <summary>Whether a key in external_variables belongs to this page.</summary>
    public static bool Owns(string key) =>
        key.StartsWith(LandingViewVocabulary.Prefix, StringComparison.Ordinal)
        || key.StartsWith(DeadPrefix, StringComparison.Ordinal);

    /// <param name="assetFallbacks">
    /// Values for <c>${…}</c> tokens the file itself cannot answer. <c>url.prefix</c> is the one that
    /// matters: the game client substitutes it from the hotel's own configuration, so it exists in no
    /// gamedata file, and without it every stored image URL keeps a hole in it.
    /// </param>
    public static HotelViewConfig Read(
        JsonObject? variables,
        JsonObject? texts,
        DateTime modifiedUtc,
        DateTime textsModifiedUtc,
        IReadOnlyDictionary<string, string>? assetFallbacks = null
    )
    {
        Dictionary<string, string> all = Flatten(variables);
        HashSet<string> consumed = new(StringComparer.Ordinal);

        string Take(string key)
        {
            consumed.Add(key);
            return all.TryGetValue(key, out string? value) ? value : string.Empty;
        }

        HotelViewCommon common = new(
            Take("landing.view.common.textcolor"),
            Take("landing.view.common.etchingcolor"),
            Take("landing.view.common.etchingposition"),
            Take("landing.view.dynamic.leftPaneWidth"),
            Take("landing.view.dynamic.rightPaneWidth"),
            Take("landing.view.layoutxml"),
            Take("landing.view.roomcategory"),
            IsTrue(Take("landing.view.right_pane_dimmer.hidden")),
            ReadSchedule(Take("landing.view.bgtiming"))
        );

        List<HotelViewSlot> slots = [];

        for (int number = 1; number <= LandingViewVocabulary.SlotCount; number++)
        {
            string widget = Take($"{SlotPrefix}{number}.widget");
            string conf = Take($"{SlotPrefix}{number}.conf");

            slots.Add(
                new HotelViewSlot(
                    number,
                    widget,
                    IsSchedule(widget) ? ReadSchedule(conf) : [],
                    IsSchedule(widget) ? [] : ReadElements(conf, texts),
                    ReadLayout(Take($"{SlotPrefix}{number}.layout")),
                    IsTrue(Take($"{SlotPrefix}{number}.separator")),
                    Take($"{SlotPrefix}{number}.title"),
                    IsTrue(Take($"{SlotPrefix}{number}.ignore"))
                )
            );
        }

        List<HotelViewCampaign> campaigns = ReadCampaigns(all, consumed, texts, slots);
        List<HotelViewScene> scenes = ReadScenes(all, consumed);

        List<HotelViewExtra> extras =
        [
            .. all.Where(pair => !consumed.Contains(pair.Key))
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new HotelViewExtra(pair.Key, pair.Value, IsKnownExtra(pair.Key))),
        ];

        return new HotelViewConfig(
            true,
            modifiedUtc == default ? null : modifiedUtc,
            textsModifiedUtc == default ? null : textsModifiedUtc,
            common,
            slots,
            campaigns,
            scenes,
            extras,
            ReadPlaceholders(variables, all.Values, assetFallbacks),
            LandingViewVocabulary.Describe()
        );
    }

    /// <summary>
    /// The <c>${…}</c> tokens used anywhere in this block, resolved against the rest of the file.
    /// </summary>
    /// <remarks>
    /// Resolved here rather than in the page because the values are ordinary external_variables keys
    /// -- <c>image.library.url</c>, <c>url.prefix</c> -- that the page has no other reason to fetch.
    /// A token with no key behind it is left out, so the page renders the raw text rather than a URL
    /// with a hole in it.
    /// </remarks>
    private static Dictionary<string, string> ReadPlaceholders(
        JsonObject? variables,
        IEnumerable<string> values,
        IReadOnlyDictionary<string, string>? fallbacks
    )
    {
        Dictionary<string, string> resolved = new(StringComparer.Ordinal);
        Queue<string> pending = new();

        foreach (Match match in Placeholder.Matches(string.Join('\n', values)))
        {
            pending.Enqueue(match.Groups[1].Value);
        }

        // A token's value can itself be written with tokens: `image.library.url` is literally
        // `${url.prefix}/c_images/` in a shipped dump. Resolving one level deep leaves every image
        // URL with a hole in it and no preview anywhere, so the tokens found inside a resolved value
        // go back on the queue. Bounded because a dump could point two keys at each other.
        while (pending.Count > 0 && resolved.Count < 32)
        {
            string token = pending.Dequeue();

            if (resolved.ContainsKey(token))
            {
                continue;
            }

            string? value =
                variables?.TryGetPropertyValue(token, out JsonNode? node) == true ? Scalar(node)
                : fallbacks?.TryGetValue(token, out string? fallback) == true ? fallback
                : null;

            if (value is null)
            {
                continue;
            }

            resolved[token] = value;

            foreach (Match match in Placeholder.Matches(value))
            {
                pending.Enqueue(match.Groups[1].Value);
            }
        }

        return resolved;
    }

    /// <summary>Every owned key the configuration implies, and no others.</summary>
    /// <remarks>
    /// A slot carries both a schedule and an element list while it is being edited, but only one
    /// <c>conf</c> key exists on disk. The widget type decides which of the two is written, so
    /// switching a slot away from <c>widgetcontainer</c> and saving does discard its schedule -- the
    /// file has nowhere to keep it.
    /// </remarks>
    public static Dictionary<string, string> Compose(HotelViewConfig config)
    {
        Dictionary<string, string> keys = new(StringComparer.Ordinal);

        void Set(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                keys[key] = value;
            }
        }

        HotelViewCommon common = config.Common;
        Set("landing.view.common.textcolor", common.TextColor);
        Set("landing.view.common.etchingcolor", common.EtchingColor);
        Set("landing.view.common.etchingposition", common.EtchingPosition);
        Set("landing.view.dynamic.leftPaneWidth", common.LeftPaneWidth);
        Set("landing.view.dynamic.rightPaneWidth", common.RightPaneWidth);
        Set("landing.view.layoutxml", common.LayoutXml);
        Set("landing.view.roomcategory", common.RoomCategory);
        Set("landing.view.bgtiming", WriteSchedule(common.SceneSchedule));

        if (common.RightPaneDimmerHidden)
        {
            Set("landing.view.right_pane_dimmer.hidden", "true");
        }

        foreach (HotelViewSlot slot in config.Slots)
        {
            Set($"{SlotPrefix}{slot.Number}.widget", slot.Widget);
            Set(
                $"{SlotPrefix}{slot.Number}.conf",
                IsSchedule(slot.Widget)
                    ? WriteSchedule(slot.Schedule)
                    : WriteElements(slot.Elements)
            );
            Set($"{SlotPrefix}{slot.Number}.layout", WriteLayout(slot.Layout));

            if (slot.Separator)
            {
                Set($"{SlotPrefix}{slot.Number}.separator", "true");
                Set($"{SlotPrefix}{slot.Number}.title", slot.SeparatorTitle);
            }

            if (slot.Ignore)
            {
                Set($"{SlotPrefix}{slot.Number}.ignore", "true");
            }
        }

        foreach (HotelViewCampaign campaign in config.Campaigns)
        {
            string prefix = LandingViewVocabulary.Prefix + campaign.Code + ".";
            Set(prefix + "widget", campaign.Widget);
            Set(prefix + "conf", WriteElements(campaign.Elements));
            Set(prefix + "layout", WriteLayout(campaign.Layout));
        }

        foreach (HotelViewScene scene in config.Scenes)
        {
            string prefix =
                LandingViewVocabulary.Prefix
                + (string.IsNullOrEmpty(scene.Code) ? string.Empty : scene.Code + ".");

            foreach (HotelViewBackground layer in scene.Layers)
            {
                Set($"{prefix}{layer.Layer}.uri", layer.Uri);

                if (!layer.Visible)
                {
                    Set($"{prefix}{layer.Layer}.visible", "false");
                }
            }

            foreach (HotelViewObject item in scene.Objects)
            {
                Set($"{prefix}bgobject.{item.Index}", WriteObject(item));
            }
        }

        foreach (HotelViewExtra extra in config.Extras)
        {
            Set(extra.Key, extra.Value);
        }

        return keys;
    }

    /// <summary>
    /// The external_flash_texts entries the configuration carries words for, keyed by the text key
    /// the element points at.
    /// </summary>
    /// <remarks>
    /// Only elements whose first argument is a text key can produce one, and only when the page
    /// actually sent words back. A key with no text is left exactly as the texts file has it: the
    /// absence of a string here means "not edited", never "clear it".
    /// </remarks>
    public static Dictionary<string, string> ComposeTexts(HotelViewConfig config)
    {
        Dictionary<string, string> texts = new(StringComparer.Ordinal);

        foreach (
            HotelViewElement element in config
                .Slots.SelectMany(slot => slot.Elements)
                .Concat(config.Campaigns.SelectMany(campaign => campaign.Elements))
        )
        {
            if (
                element.Text is null
                || !LandingViewVocabulary.CarriesText(element.Type)
                || element.Args.Count == 0
                || string.IsNullOrWhiteSpace(element.Args[0])
            )
            {
                continue;
            }

            texts[element.Args[0]] = element.Text;
        }

        return texts;
    }

    /// <summary>
    /// The first thing wrong with the configuration, or <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Every one of these is a value the client accepts without complaint and then does nothing with:
    /// a mistyped widget leaves a blank slot, a mistyped element type makes the client abandon the
    /// entire content column, a mistyped layer is never looked up. Refusing the save is the only
    /// place the mistake can still be seen.
    /// </remarks>
    public static string? Validate(HotelViewConfig config)
    {
        foreach (HotelViewSlot slot in config.Slots)
        {
            if (slot.Widget.Length > 0 && !LandingViewVocabulary.IsWidget(slot.Widget))
            {
                return $"unknown_widget:{slot.Widget}";
            }

            if (Invalid(slot.Elements, slot.Layout) is { } bad)
            {
                return bad;
            }
        }

        foreach (HotelViewCampaign campaign in config.Campaigns)
        {
            if (campaign.Code.Length == 0 || campaign.Code.Contains('.', StringComparison.Ordinal))
            {
                return $"invalid_campaign_code:{campaign.Code}";
            }

            if (campaign.Widget.Length > 0 && !LandingViewVocabulary.IsWidget(campaign.Widget))
            {
                return $"unknown_widget:{campaign.Widget}";
            }

            if (Invalid(campaign.Elements, campaign.Layout) is { } bad)
            {
                return bad;
            }
        }

        foreach (HotelViewScene scene in config.Scenes)
        {
            foreach (HotelViewBackground layer in scene.Layers)
            {
                if (!LandingViewVocabulary.IsBackgroundLayer(layer.Layer))
                {
                    return $"unknown_layer:{layer.Layer}";
                }
            }

            foreach (HotelViewObject item in scene.Objects)
            {
                if (item.Index < 1 || item.Index > LandingViewVocabulary.MaxObjects)
                {
                    return $"object_out_of_range:{item.Index}";
                }

                if (LandingViewVocabulary.Motion(item.Motion) is null)
                {
                    return $"unknown_motion:{item.Motion}";
                }
            }
        }

        foreach (HotelViewExtra extra in config.Extras)
        {
            if (!Owns(extra.Key))
            {
                return $"foreign_key:{extra.Key}";
            }
        }

        return null;

        static string? Invalid(
            IReadOnlyList<HotelViewElement> elements,
            IReadOnlyList<HotelViewLayoutValue> layout
        )
        {
            foreach (HotelViewElement element in elements)
            {
                if (LandingViewVocabulary.Element(element.Type) is null)
                {
                    return $"unknown_element:{element.Type}";
                }
            }

            foreach (HotelViewLayoutValue value in layout)
            {
                if (!LandingViewVocabulary.IsLayoutKey(value.Key))
                {
                    return $"unknown_layout_key:{value.Key}";
                }
            }

            return null;
        }
    }

    private static Dictionary<string, string> Flatten(JsonObject? variables)
    {
        Dictionary<string, string> all = new(StringComparer.Ordinal);

        if (variables is null)
        {
            return all;
        }

        foreach ((string key, JsonNode? value) in variables)
        {
            if (Owns(key))
            {
                all[key] = Scalar(value);
            }
        }

        return all;
    }

    private static string Scalar(JsonNode? node) =>
        node is null ? string.Empty
        : node.GetValueKind() == System.Text.Json.JsonValueKind.String ? node.GetValue<string>()
        : node.ToJsonString();

    private static bool IsTrue(string value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether this widget reads its <c>conf</c> as a schedule rather than an element list.</summary>
    private static bool IsSchedule(string widget) =>
        string.Equals(widget, "widgetcontainer", StringComparison.Ordinal);

    private static List<HotelViewScheduleEntry> ReadSchedule(string value)
    {
        List<HotelViewScheduleEntry> entries = [];

        foreach (string entry in value.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int comma = entry.IndexOf(',', StringComparison.Ordinal);

            entries.Add(
                comma < 0
                    ? new HotelViewScheduleEntry(entry.Trim(), string.Empty)
                    : new HotelViewScheduleEntry(entry[..comma].Trim(), entry[(comma + 1)..].Trim())
            );
        }

        return entries;
    }

    private static string WriteSchedule(IReadOnlyList<HotelViewScheduleEntry> entries) =>
        string.Join(
            ';',
            entries
                .Where(entry => entry.StartsAt.Length > 0)
                .Select(entry => $"{entry.StartsAt},{entry.Code}")
        );

    private static List<HotelViewElement> ReadElements(string value, JsonObject? texts)
    {
        List<HotelViewElement> elements = [];

        foreach (string entry in value.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = entry.Split(',');
            string type = parts[0].Trim();
            string[] args = [.. parts.Skip(1)];

            elements.Add(
                new HotelViewElement(
                    type,
                    args,
                    LandingViewVocabulary.CarriesText(type) && args.Length > 0
                        ? LookupText(texts, args[0])
                        : null
                )
            );
        }

        return elements;
    }

    private static string WriteElements(IReadOnlyList<HotelViewElement> elements) =>
        string.Join(
            ';',
            elements
                .Where(element => element.Type.Length > 0)
                .Select(element => string.Join(',', new[] { element.Type }.Concat(element.Args)))
        );

    private static List<HotelViewLayoutValue> ReadLayout(string value)
    {
        List<HotelViewLayoutValue> layout = [];

        foreach (string entry in value.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int comma = entry.IndexOf(',', StringComparison.Ordinal);

            if (comma > 0)
            {
                layout.Add(
                    new HotelViewLayoutValue(entry[..comma].Trim(), entry[(comma + 1)..].Trim())
                );
            }
        }

        return layout;
    }

    private static string WriteLayout(IReadOnlyList<HotelViewLayoutValue> layout) =>
        string.Join(
            ';',
            layout
                .Where(value => value.Key.Length > 0)
                .Select(value => $"{value.Key},{value.Value}")
        );

    private static string WriteObject(HotelViewObject item) =>
        string.Join(';', new[] { item.Asset, item.Motion }.Concat(item.Fields));

    private static string? LookupText(JsonObject? texts, string key) =>
        texts is not null && texts.TryGetPropertyValue(key, out JsonNode? node)
            ? Scalar(node)
            : null;

    private static List<HotelViewCampaign> ReadCampaigns(
        Dictionary<string, string> all,
        HashSet<string> consumed,
        JsonObject? texts,
        IReadOnlyList<HotelViewSlot> slots
    )
    {
        Dictionary<string, Dictionary<string, string>> byCode = new(StringComparer.Ordinal);

        foreach ((string key, string value) in all)
        {
            if (
                consumed.Contains(key)
                || !key.StartsWith(LandingViewVocabulary.Prefix, StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] parts = key[LandingViewVocabulary.Prefix.Length..].Split('.');

            if (parts.Length != 2 || parts[0].Length == 0)
            {
                continue;
            }

            if (parts[1] is not ("widget" or "conf" or "layout"))
            {
                continue;
            }

            if (!byCode.TryGetValue(parts[0], out Dictionary<string, string>? fields))
            {
                fields = new Dictionary<string, string>(StringComparer.Ordinal);
                byCode[parts[0]] = fields;
            }

            fields[parts[1]] = value;
            consumed.Add(key);
        }

        return
        [
            .. byCode
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new HotelViewCampaign(
                    pair.Key,
                    Field(pair.Value, "widget"),
                    ReadElements(Field(pair.Value, "conf"), texts),
                    ReadLayout(Field(pair.Value, "layout")),
                    [
                        .. slots
                            .Where(slot =>
                                slot.Schedule.Any(entry =>
                                    string.Equals(entry.Code, pair.Key, StringComparison.Ordinal)
                                )
                            )
                            .Select(slot => slot.Number),
                    ]
                )),
        ];

        static string Field(Dictionary<string, string> fields, string name) =>
            fields.TryGetValue(name, out string? value) ? value : string.Empty;
    }

    /// <summary>
    /// The scenery sets, keyed by the timing code that selects them; the empty code is the default.
    /// </summary>
    /// <remarks>
    /// Every set is returned with all nine layers present whether the file names them or not, so the
    /// page shows the layers a hotel has not set yet instead of hiding them. A layer with an empty
    /// URI composes back to no key at all, which is exactly the state it came from.
    /// </remarks>
    private static List<HotelViewScene> ReadScenes(
        Dictionary<string, string> all,
        HashSet<string> consumed
    )
    {
        Dictionary<string, Dictionary<string, HotelViewBackground>> layers = new(
            StringComparer.Ordinal
        );
        Dictionary<string, Dictionary<int, HotelViewObject>> objects = new(StringComparer.Ordinal);

        foreach ((string key, string value) in all)
        {
            if (
                consumed.Contains(key)
                || !key.StartsWith(LandingViewVocabulary.Prefix, StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] parts = key[LandingViewVocabulary.Prefix.Length..].Split('.');

            if (
                parts.Length is 2 or 3
                && parts[^1] is "uri" or "visible"
                && LandingViewVocabulary.IsBackgroundLayer(parts[^2])
            )
            {
                string code = parts.Length == 3 ? parts[0] : string.Empty;
                Dictionary<string, HotelViewBackground> set = Bucket(layers, code);

                HotelViewBackground current = set.TryGetValue(
                    parts[^2],
                    out HotelViewBackground? existing
                )
                    ? existing
                    : new HotelViewBackground(parts[^2], string.Empty, true);

                set[parts[^2]] =
                    parts[^1] == "uri"
                        ? current with
                        {
                            Uri = value,
                        }
                        : current with
                        {
                            Visible = !string.Equals(value, "false", StringComparison.Ordinal),
                        };

                consumed.Add(key);
                continue;
            }

            if (
                parts.Length is 2 or 3
                && parts[^2] == "bgobject"
                && int.TryParse(
                    parts[^1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int index
                )
            )
            {
                string code = parts.Length == 3 ? parts[0] : string.Empty;
                string[] fields = value.Split(';');

                Bucket(objects, code)[index] = new HotelViewObject(
                    index,
                    fields.Length > 0 ? fields[0] : string.Empty,
                    fields.Length > 1 ? fields[1] : string.Empty,
                    [.. fields.Skip(2)]
                );

                consumed.Add(key);
            }
        }

        IEnumerable<string> codes = layers
            .Keys.Concat(objects.Keys)
            .Distinct(StringComparer.Ordinal);

        return
        [
            .. codes
                .OrderBy(code => code.Length == 0 ? 0 : 1)
                .ThenBy(code => code, StringComparer.Ordinal)
                .Select(code => new HotelViewScene(
                    code,
                    [
                        .. LandingViewVocabulary.BackgroundLayers.Select(layer =>
                            layers.TryGetValue(
                                code,
                                out Dictionary<string, HotelViewBackground>? set
                            ) && set.TryGetValue(layer, out HotelViewBackground? found)
                                ? found
                                : new HotelViewBackground(layer, string.Empty, true)
                        ),
                    ],
                    objects.TryGetValue(code, out Dictionary<int, HotelViewObject>? items)
                        ? [.. items.Values.OrderBy(item => item.Index)]
                        : []
                )),
        ];

        static Dictionary<TKey, TValue> Bucket<TKey, TValue>(
            Dictionary<string, Dictionary<TKey, TValue>> buckets,
            string code
        )
            where TKey : notnull
        {
            if (!buckets.TryGetValue(code, out Dictionary<TKey, TValue>? bucket))
            {
                bucket = [];
                buckets[code] = bucket;
            }

            return bucket;
        }
    }

    private static bool IsKnownExtra(string key) =>
        KnownExtras.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal));
}
