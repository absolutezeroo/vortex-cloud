using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Vortex.Dashboard.API.Api.Hotel.Contracts;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Database.Context;

namespace Vortex.Dashboard.API.Api.Hotel;

/// <summary>
/// The gamedata files an operator edits, read for the page that edits them.
/// </summary>
/// <remarks>
/// Searching and paging happen here, against the parsed document held by
/// <see cref="GamedataDocumentStore"/>. furnidata alone is 38 MB and 55 836 entries: sending it to a
/// browser is not a slow page, it is a page that never renders.
/// </remarks>
internal sealed class GamedataReads(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    DashboardAssetUrls assetUrls,
    GamedataDocumentStore gamedata
) : DashboardReads(dbContextFactory)
{
    private readonly DashboardAssetUrls _assetUrls = assetUrls;
    private readonly GamedataDocumentStore _gamedata = gamedata;

    private const int GamedataPageSize = 50;

    /// <summary>The four files, with what a page needs to show and to write safely.</summary>
    /// <remarks>
    /// <c>modifiedUtc</c> travels back on every write as the caller's expected value: it is what
    /// turns two operators editing at once into a refusal instead of a silently dropped edit.
    /// </remarks>
    public GamedataFileList GamedataFiles()
    {
        if (!_gamedata.Available)
        {
            return new GamedataFileList(false, []);
        }

        List<GamedataFileRow> files = [];

        foreach ((string token, string name) in GamedataDocumentStore.Files)
        {
            JsonNode? root = _gamedata.Read(token, null, out DateTime modified);

            files.Add(
                new GamedataFileRow(
                    token,
                    name,
                    GamedataDocumentStore.IsLocalised(token),
                    CountEntries(root),
                    root is not null,
                    modified == default ? null : modified,
                    token == "furnidata" ? FurniCategories(root) : []
                )
            );
        }

        return new GamedataFileList(true, files);
    }

    /// <summary>One page of a file's entries, filtered by <c>search</c>.</summary>
    public GamedataEntryPage GamedataEntries(NameValueCollection query)
    {
        string file = query["file"] ?? string.Empty;
        string? language = NullIfBlank(query["lang"]);
        string search = (query["search"] ?? string.Empty).Trim();
        int page = Math.Max(1, QueryValues.Int(query["page"], 1));

        if (!GamedataDocumentStore.Files.ContainsKey(file))
        {
            return new GamedataEntryPage("unknown_file", null, 0, page, GamedataPageSize, []);
        }

        JsonNode? root = _gamedata.Read(file, language, out DateTime modified);

        if (root is null)
        {
            return new GamedataEntryPage("unreadable", null, 0, page, GamedataPageSize, []);
        }

        List<GamedataEntry> matches = file switch
        {
            "furnidata" => FurnidataEntries(
                root,
                search,
                NullIfBlank(query["kind"]),
                NullIfBlank(query["category"])
            ),
            "productdata" => ProductdataEntries(root, search),
            _ => FlatEntries(root, search),
        };

        return new GamedataEntryPage(
            null,
            modified,
            matches.Count,
            page,
            GamedataPageSize,
            [.. matches.Skip((page - 1) * GamedataPageSize).Take(GamedataPageSize)]
        );
    }

    /// <summary>
    /// The languages the client is told about, and the state of each one's texts file.
    /// </summary>
    /// <remarks>
    /// The declared list comes from <c>external_variables.json</c> — the block IS the state, so
    /// there is no flag in a database to drift from it. The hotel's own language rows are offered
    /// alongside as candidates, because that list already exists for the website and two lists of
    /// languages always end up disagreeing.
    /// </remarks>
    public GamedataLanguageList GamedataLanguages()
    {
        JsonNode? root = _gamedata.Read("variables", null, out DateTime modified);

        if (root is not JsonObject variables)
        {
            return new GamedataLanguageList(false, null, []);
        }

        List<GamedataLanguageRow> languages = [];

        foreach (GamedataLanguage language in GamedataLanguageRegistry.Read(variables))
        {
            bool hasFile =
                _gamedata.TryResolve("texts", language.Code, out string path) && File.Exists(path);

            languages.Add(
                new GamedataLanguageRow(
                    language.Id,
                    language.Code,
                    language.Name,
                    language.Url,
                    hasFile,
                    ":lang " + language.Id
                )
            );
        }

        return new GamedataLanguageList(true, modified, languages);
    }

    private static List<GamedataEntry> FlatEntries(JsonNode root, string search)
    {
        if (root is not JsonObject map)
        {
            return [];
        }

        List<GamedataEntry> entries = [];

        foreach ((string key, JsonNode? value) in map)
        {
            string text = value?.ToString() ?? string.Empty;

            // Key and value both: an operator hunting a wording remembers the sentence, not the key
            // it lives under.
            if (search.Length > 0 && !Contains(key, search) && !Contains(text, search))
            {
                continue;
            }

            entries.Add(
                new GamedataEntry(
                    key,
                    text,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                )
            );
        }

        return entries;
    }

    private List<GamedataEntry> FurnidataEntries(
        JsonNode root,
        string search,
        string? kindFilter,
        string? categoryFilter
    )
    {
        List<GamedataEntry> entries = [];

        foreach (string kind in (string[])["roomitemtypes", "wallitemtypes"])
        {
            if (
                kindFilter is not null
                && !string.Equals(kind, kindFilter, StringComparison.Ordinal)
            )
            {
                continue;
            }

            if (root[kind]?["furnitype"] is not JsonArray list)
            {
                continue;
            }

            for (int index = 0; index < list.Count; index++)
            {
                if (list[index] is not JsonObject entry)
                {
                    continue;
                }

                string classname = entry["classname"]?.ToString() ?? string.Empty;
                string name = entry["name"]?.ToString() ?? string.Empty;
                string id = entry["id"]?.ToString() ?? string.Empty;
                string category = entry["category"]?.ToString() ?? string.Empty;

                if (
                    categoryFilter is not null
                    && !string.Equals(category, categoryFilter, StringComparison.OrdinalIgnoreCase)
                )
                {
                    continue;
                }

                if (
                    search.Length > 0
                    && !Contains(classname, search)
                    && !Contains(name, search)
                    && !Contains(id, search)
                )
                {
                    continue;
                }

                entries.Add(
                    new GamedataEntry(
                        null,
                        null,
                        kind,
                        index,
                        id,
                        classname,
                        name,
                        entry["description"]?.ToString() ?? string.Empty,
                        category,
                        entry["xdim"]?.ToString() ?? string.Empty,
                        entry["ydim"]?.ToString() ?? string.Empty,
                        // Editing a furniture by class name alone means editing a name in a list of
                        // 55 836. The icon is how an operator knows they have the right one.
                        _assetUrls.FurniIcon(classname),
                        null
                    )
                );
            }
        }

        return entries;
    }

    private static List<GamedataEntry> ProductdataEntries(JsonNode root, string search)
    {
        if (root["productdata"]?["product"] is not JsonArray list)
        {
            return [];
        }

        List<GamedataEntry> entries = [];

        for (int index = 0; index < list.Count; index++)
        {
            if (list[index] is not JsonObject entry)
            {
                continue;
            }

            string code = entry["code"]?.ToString() ?? string.Empty;
            string name = entry["name"]?.ToString() ?? string.Empty;

            if (search.Length > 0 && !Contains(code, search) && !Contains(name, search))
            {
                continue;
            }

            entries.Add(
                new GamedataEntry(
                    null,
                    null,
                    null,
                    index,
                    null,
                    null,
                    name,
                    entry["description"]?.ToString() ?? string.Empty,
                    null,
                    null,
                    null,
                    null,
                    code
                )
            );
        }

        return entries;
    }

    /// <summary>Every category present in furnidata, sorted, for the filter's list.</summary>
    private static List<string> FurniCategories(JsonNode? root)
    {
        SortedSet<string> categories = new(StringComparer.OrdinalIgnoreCase);

        foreach (string kind in (string[])["roomitemtypes", "wallitemtypes"])
        {
            if (root?[kind]?["furnitype"] is not JsonArray list)
            {
                continue;
            }

            foreach (JsonNode? node in list)
            {
                string category = node?["category"]?.ToString() ?? string.Empty;

                if (category.Length > 0)
                {
                    categories.Add(category);
                }
            }
        }

        return [.. categories];
    }

    private static int CountEntries(JsonNode? root) =>
        root switch
        {
            null => 0,
            JsonObject map when map["roomitemtypes"]?["furnitype"] is JsonArray floors =>
                floors.Count + ((map["wallitemtypes"]?["furnitype"] as JsonArray)?.Count ?? 0),
            JsonObject map when map["productdata"]?["product"] is JsonArray products =>
                products.Count,
            JsonObject map => map.Count,
            _ => 0,
        };

    private static bool Contains(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
