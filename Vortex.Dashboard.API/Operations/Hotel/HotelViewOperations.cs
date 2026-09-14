using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Dashboard.API.Api.Hotel.Contracts;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Operations.Hotel.Contracts;
using Vortex.Primitives.Observability;

namespace Vortex.Dashboard.API.Operations.Hotel;

/// <summary>
/// The hotel view, written back into the two gamedata files it lives in.
/// </summary>
/// <remarks>
/// The words go first. If the second write is refused -- a stale timestamp, a file that will not
/// parse back -- what survives is text nobody points at yet, which is invisible. The other order
/// leaves wiring pointing at keys that do not exist, and the client renders a missing key as the
/// literal <c>${landing.view.jan26cf.header}</c> across the hotel's front page.
/// <para>
/// The texts file is only opened when a value actually changed. It is a megabyte the client
/// re-downloads whenever its hash moves, and rewriting it identically would cost every player that
/// download for nothing.
/// </para>
/// </remarks>
internal sealed class HotelViewOperations(OperationRunner runner, GamedataDocumentStore gamedata)
{
    private readonly OperationRunner _runner = runner;
    private readonly GamedataDocumentStore _gamedata = gamedata;

    public Task<OperationResult> SaveHotelViewAsync(
        HotelViewSaveRequest request,
        string actor,
        CancellationToken ct
    ) =>
        _runner.ExecuteAsync(
            "ops.hotelview.save",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                Slots = request.Slots.Count,
                Campaigns = request.Campaigns.Count,
                Scenes = request.Scenes.Count,
            },
            work: _ =>
            {
                HotelViewConfig config = new(
                    true,
                    request.ExpectedModifiedUtc,
                    request.ExpectedTextsModifiedUtc,
                    request.Common,
                    request.Slots,
                    request.Campaigns,
                    request.Scenes,
                    request.Extras,
                    new Dictionary<string, string>(),
                    LandingViewVocabulary.Describe()
                );

                if (LandingViewDocument.Validate(config) is { } error)
                {
                    throw new InvalidOperationException(error);
                }

                WriteTexts(
                    LandingViewDocument.ComposeTexts(config),
                    request.ExpectedTextsModifiedUtc
                );
                WriteVariables(LandingViewDocument.Compose(config), request.ExpectedModifiedUtc);

                return Task.CompletedTask;
            },
            ct,
            AuditCategory.System
        );

    /// <summary>
    /// Replaces every <c>landing.view.*</c> key with the ones the configuration implies.
    /// </summary>
    /// <remarks>
    /// Assignment before removal, and both against the live document, so keys that survive keep their
    /// position in the file. A hotel's external_variables is read by people in a diff; rewriting it
    /// in a new order to change one URL turns a one-line change into an unreviewable one.
    /// </remarks>
    private void WriteVariables(Dictionary<string, string> keys, DateTime? expectedModifiedUtc)
    {
        GamedataWriteResult result = _gamedata.Write(
            "variables",
            language: null,
            expectedModifiedUtc,
            root =>
            {
                if (root is not JsonObject map)
                {
                    return null;
                }

                foreach ((string key, string value) in keys)
                {
                    map[key] = value;
                }

                foreach (
                    string stale in map.Select(pair => pair.Key)
                        .Where(key => LandingViewDocument.Owns(key) && !keys.ContainsKey(key))
                        .ToList()
                )
                {
                    map.Remove(stale);
                }

                return map;
            }
        );

        Throw(result);
    }

    private void WriteTexts(Dictionary<string, string> texts, DateTime? expectedModifiedUtc)
    {
        if (texts.Count == 0)
        {
            return;
        }

        JsonNode? current = _gamedata.Read("texts", null, out DateTime _);

        if (
            current is JsonObject existing
            && texts.All(pair =>
                existing.TryGetPropertyValue(pair.Key, out JsonNode? node)
                && node is JsonValue value
                && value.TryGetValue(out string? held)
                && held == pair.Value
            )
        )
        {
            return;
        }

        GamedataWriteResult result = _gamedata.Write(
            "texts",
            language: null,
            expectedModifiedUtc,
            root =>
            {
                if (root is not JsonObject map)
                {
                    return null;
                }

                foreach ((string key, string value) in texts)
                {
                    map[key] = value;
                }

                return map;
            }
        );

        Throw(result);
    }

    private static void Throw(GamedataWriteResult result)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException(result.Error);
        }
    }
}
