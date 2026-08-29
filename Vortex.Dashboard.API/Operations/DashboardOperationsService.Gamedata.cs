using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Primitives.Observability;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// The four gamedata files, written.
/// </summary>
/// <remarks>
/// Everything here goes through <see cref="GamedataDocumentStore"/>, which keeps a dated copy, writes
/// beside the file, parses the bytes back and only then moves them into place. These files are what
/// the game client fetches at boot: a bad save is not a failed save, it is a hotel that will not
/// start.
/// <para>
/// Audited like every other dashboard write, and worth it here more than most — these files have no
/// row history, so the audit line plus the dated backup is the whole record of who changed what.
/// </para>
/// </remarks>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> SaveGamedataEntryAsync(
        GamedataEntryRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.gamedata.entry.save",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.File,
                request.Language,
                request.Key,
            },
            work: _ =>
            {
                GamedataWriteResult result = _gamedata.Write(
                    request.File,
                    request.Language,
                    request.ExpectedModifiedUtc,
                    root =>
                    {
                        if (root is not JsonObject map)
                        {
                            return null;
                        }

                        map[request.Key] = request.Value;
                        return map;
                    }
                );

                ThrowOnGamedata(result);
                return Task.CompletedTask;
            },
            ct,
            AuditCategory.System
        );

    public Task<OperationResult> DeleteGamedataEntryAsync(
        GamedataEntryDeleteRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.gamedata.entry.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.File,
                request.Language,
                request.Key,
            },
            work: _ =>
            {
                GamedataWriteResult result = _gamedata.Write(
                    request.File,
                    request.Language,
                    request.ExpectedModifiedUtc,
                    root => root is JsonObject map && map.Remove(request.Key) ? map : null
                );

                ThrowOnGamedata(result);
                return Task.CompletedTask;
            },
            ct,
            AuditCategory.System
        );

    /// <summary>Changes one field of one furnidata entry, addressed by its position.</summary>
    public Task<OperationResult> SaveGamedataFurniAsync(
        GamedataFurniRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.gamedata.furni.save",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.Kind,
                request.Index,
                request.Field,
            },
            work: _ =>
            {
                GamedataWriteResult result = _gamedata.Write(
                    "furnidata",
                    language: null,
                    request.ExpectedModifiedUtc,
                    root =>
                    {
                        if (
                            root is not JsonObject document
                            || document[request.Kind] is not JsonObject list
                            || list["furnitype"] is not JsonArray entries
                            || request.Index < 0
                            || request.Index >= entries.Count
                            || entries[request.Index] is not JsonObject entry
                            || !entry.ContainsKey(request.Field)
                        )
                        {
                            // An unknown field is refused rather than added: furnidata's shape is the
                            // client's parser, and a key it does not expect is at best ignored.
                            return null;
                        }

                        entry[request.Field] = Retype(entry[request.Field], request.Value);
                        return document;
                    }
                );

                ThrowOnGamedata(result);
                return Task.CompletedTask;
            },
            ct,
            AuditCategory.System
        );

    /// <summary>Declares a language to the client and gives it a texts file to load.</summary>
    public Task<OperationResult> EnableGamedataLanguageAsync(
        GamedataLanguageRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.gamedata.language.enable",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.Code, request.Name },
            work: _ =>
            {
                if (!GamedataDocumentStore.IsLanguageCode(request.Code))
                {
                    throw new InvalidOperationException("invalid_language_code");
                }

                SeedLanguageTexts(request.Code);

                GamedataWriteResult result = _gamedata.Write(
                    "variables",
                    language: null,
                    expectedModifiedUtc: null,
                    root =>
                    {
                        if (root is not JsonObject variables)
                        {
                            return null;
                        }

                        List<GamedataLanguage> languages =
                        [
                            .. GamedataLanguageRegistry.Read(variables),
                        ];

                        languages.RemoveAll(l =>
                            string.Equals(l.Code, request.Code, StringComparison.OrdinalIgnoreCase)
                        );

                        // The id IS the code: `:lang <id>` is how a player switches, and an opaque
                        // number would leave them nothing to type.
                        languages.Add(
                            new GamedataLanguage(
                                request.Code,
                                request.Code,
                                string.IsNullOrWhiteSpace(request.Name)
                                    ? request.Code
                                    : request.Name,
                                GamedataLanguageRegistry.BuildTextsUrl(variables, request.Code)
                            )
                        );

                        GamedataLanguageRegistry.Write(variables, languages);
                        return variables;
                    }
                );

                ThrowOnGamedata(result);
                return Task.CompletedTask;
            },
            ct,
            AuditCategory.System
        );

    /// <summary>Withdraws a language from the client. Its texts file is left on disk.</summary>
    public Task<OperationResult> DisableGamedataLanguageAsync(
        GamedataLanguageRemoveRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.gamedata.language.disable",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.Code },
            work: _ =>
            {
                GamedataWriteResult result = _gamedata.Write(
                    "variables",
                    language: null,
                    expectedModifiedUtc: null,
                    root =>
                    {
                        if (root is not JsonObject variables)
                        {
                            return null;
                        }

                        List<GamedataLanguage> languages =
                        [
                            .. GamedataLanguageRegistry.Read(variables),
                        ];

                        if (
                            languages.RemoveAll(l =>
                                string.Equals(
                                    l.Code,
                                    request.Code,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            ) == 0
                        )
                        {
                            return null;
                        }

                        GamedataLanguageRegistry.Write(variables, languages);
                        return variables;
                    }
                );

                ThrowOnGamedata(result);
                return Task.CompletedTask;
            },
            ct,
            AuditCategory.System
        );

    /// <summary>
    /// Gives a newly declared language a texts file, copied from the default one.
    /// </summary>
    /// <remarks>
    /// Copied rather than left empty: the client falls back to nothing for a key the active language
    /// lacks, so an empty file would show a hotel of blank labels. Starting from the default means
    /// the language is immediately usable and translating is a matter of replacing values one at a
    /// time. An existing file is never overwritten — that would be the translation work gone.
    /// </remarks>
    private void SeedLanguageTexts(string code)
    {
        if (
            !_gamedata.TryResolve("texts", language: null, out string source)
            || !_gamedata.TryResolve("texts", code, out string target)
        )
        {
            throw new InvalidOperationException("assets_root_not_configured");
        }

        if (File.Exists(target))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target);
    }

    /// <summary>
    /// Keeps a JSON value's type across an edit.
    /// </summary>
    /// <remarks>
    /// Every value arrives from the browser as a string, but furnidata is read by a typed parser:
    /// <c>xdim</c> is a number and <c>cansiton</c> is a boolean. Writing <c>"1"</c> where the client
    /// expects <c>1</c> is exactly the kind of change that saves cleanly and breaks a furniture at
    /// load time.
    /// </remarks>
    private static JsonNode? Retype(JsonNode? existing, string value)
    {
        if (existing is null)
        {
            return JsonValue.Create(value);
        }

        return existing.GetValueKind() switch
        {
            System.Text.Json.JsonValueKind.Number
                when double.TryParse(
                    value,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double number
                ) => JsonValue.Create(number),
            System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False =>
                JsonValue.Create(
                    value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1"
                ),
            _ => JsonValue.Create(value),
        };
    }

    /// <summary>
    /// Turns a refused write into the failure the operation layer reports.
    /// </summary>
    /// <remarks>
    /// Never swallowed. A gamedata write that quietly did nothing leaves the operator believing the
    /// hotel changed, and the next person debugging why it did not starts from the wrong end.
    /// </remarks>
    private static void ThrowOnGamedata(GamedataWriteResult result)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException(result.Error);
        }
    }
}
