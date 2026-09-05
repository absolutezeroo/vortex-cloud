using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Vortex.Dashboard.API.Infrastructure;

/// <summary>
/// The Habbicon asset pack, read the way the client reads it, so the dashboard can draw the real
/// pictures instead of listing codes.
/// </summary>
/// <remarks>
/// The pack is a spritesheet plus a <c>habbicons.json</c> naming one frame per Habbicon <b>id</b>.
/// Nothing here is invented: the constants below are the client's own
/// (<c>HabbiconAssetManager.DEFAULT_FRAME_SIZE</c> and friends), and the folder comes from
/// <c>external_variables.json</c>'s <c>habbicons.asset.root</c> rather than a hardcoded <c>dev/</c> —
/// an operator who installs a different pack moves that variable, and the dashboard has to follow it
/// or it draws the previous pack's pictures under the new pack's ids. That failure is silent, which
/// is exactly how the ids and the pack disagreed in the first place.
/// <para>
/// <b>The metadata is authored bottom-left.</b> The client computes <c>height - y - frame</c> and
/// only falls back to a plain <c>y</c> when that lands off the sheet. The same flip happens here,
/// once, so what reaches the browser is already a top-left CSS offset and no caller has to know.
/// </para>
/// <para>
/// One <c>background-image</c> and a <c>background-position</c> per icon: the sheet is a single
/// request for all 33, and cropping in CSS means no canvas, no per-icon endpoint and no second copy
/// of the pack. Animated Habbicons (<c>duck_spinning</c> and two others) draw as their preview frame,
/// like every non-animating surface in the client.
/// </para>
/// </remarks>
internal sealed class HabbiconArtwork(
    GamedataDocumentStore gamedata,
    DashboardAssetUrls assets,
    ILogger<HabbiconArtwork> logger
)
{
    /// <summary>AS3: <c>HabbiconAssetManager.DEFAULT_FRAME_SIZE</c>.</summary>
    private const int FrameSize = 40;

    /// <summary>AS3: <c>HabbiconAssetManager.DEFAULT_COLLECTION_ICON_SIZE</c>.</summary>
    private const int CollectionIconSize = 18;

    /// <summary>AS3: <c>HabbiconAssetManager._SafeStr_11096</c>.</summary>
    private const string MetadataFile = "habbicons.json";

    /// <summary>AS3: <c>HabbiconAssetManager._SafeStr_10916</c>.</summary>
    private const string SpritesheetFile = "habbicons_spritesheet.png";

    /// <summary>AS3: <c>HabbiconAssetManager.COLLECTION_ICONS_SPRITESHEET_FILE</c>.</summary>
    private const string CollectionSpritesheetFile = "collection_icons_spritesheet.png";

    /// <summary>The external variable the client resolves the pack folder from.</summary>
    private const string AssetRootVariable = "habbicons.asset.root";

    /// <summary>
    /// The folder every served asset path must start with. <c>/hotel-assets</c> enforces the same
    /// list; resolving to anything else here would only produce URLs that 404.
    /// </summary>
    private const string ServedRoot = "c_images/";

    private readonly Lock _gate = new();
    private HabbiconArtworkView? _cached;
    private DateTime _cachedAt;

    /// <summary>
    /// The pack as the browser needs it, or <c>null</c> when there is no pack to read — no root
    /// configured, the variable absent, the files missing or malformed. The page then lists codes,
    /// which is what it did before this existed; nothing is ever fabricated.
    /// </summary>
    public HabbiconArtworkView? Read()
    {
        string? folder = ResolveFolder();

        if (folder is null || string.IsNullOrWhiteSpace(assets.LocalRoot))
        {
            return null;
        }

        string metadata = Path.Combine(
            assets.LocalRoot,
            folder.Replace('/', Path.DirectorySeparatorChar),
            MetadataFile
        );

        if (!File.Exists(metadata))
        {
            return null;
        }

        // The pack is a file an operator swaps; re-reading it when it moves is the difference between
        // "install the new pack" and "install the new pack and restart the hotel".
        DateTime stamp = File.GetLastWriteTimeUtc(metadata);

        lock (_gate)
        {
            if (_cached is not null && _cachedAt == stamp)
            {
                return _cached;
            }

            HabbiconArtworkView? view = Load(folder, metadata);

            _cached = view;
            _cachedAt = stamp;

            return view;
        }
    }

    private HabbiconArtworkView? Load(string folder, string metadataPath)
    {
        try
        {
            JsonNode? root = JsonNode.Parse(File.ReadAllText(metadataPath));

            int sheetHeight = PngHeight(
                Path.Combine(
                    assets.LocalRoot,
                    folder.Replace('/', Path.DirectorySeparatorChar),
                    SpritesheetFile
                )
            );
            int collectionSheetHeight = PngHeight(
                Path.Combine(
                    assets.LocalRoot,
                    folder.Replace('/', Path.DirectorySeparatorChar),
                    CollectionSpritesheetFile
                )
            );

            if (sheetHeight <= 0)
            {
                return null;
            }

            return new HabbiconArtworkView(
                $"/hotel-assets/{folder}{SpritesheetFile}",
                collectionSheetHeight > 0
                    ? $"/hotel-assets/{folder}{CollectionSpritesheetFile}"
                    : null,
                FrameSize,
                CollectionIconSize,
                Frames(root?["habbicons"]?.AsArray(), sheetHeight, FrameSize),
                Frames(
                    root?["collectionIcons"]?.AsArray(),
                    collectionSheetHeight,
                    CollectionIconSize
                )
            );
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException
            )
        {
            // A broken pack is an operator problem, not a dashboard crash: the page falls back to
            // codes and the log says which file to look at.
            logger.LogWarning(
                ex,
                "Habbicon artwork pack at {Path} could not be read.",
                metadataPath
            );

            return null;
        }
    }

    /// <summary>
    /// One frame per id, flipped to a top-left origin. Entries without usable coordinates are
    /// dropped rather than guessed — a missing picture reads as missing, a wrong one does not.
    /// </summary>
    private static IReadOnlyDictionary<int, HabbiconFrame> Frames(
        JsonArray? entries,
        int sheetHeight,
        int size
    )
    {
        Dictionary<int, HabbiconFrame> frames = [];

        if (entries is null || sheetHeight <= 0)
        {
            return frames;
        }

        foreach (JsonNode? entry in entries)
        {
            if (
                entry?["id"]?.GetValue<int>() is not int id
                || entry["x"]?.GetValue<int>() is not int x
                || entry["y"]?.GetValue<int>() is not int y
            )
            {
                continue;
            }

            // Bottom-left origin, as the client assumes; the plain `y` is its documented fallback for
            // a sheet authored the other way up.
            int top = sheetHeight - y - size;

            frames[id] = new HabbiconFrame(x, top >= 0 && top + size <= sheetHeight ? top : y);
        }

        return frames;
    }

    /// <summary>
    /// A PNG's height, straight out of the IHDR chunk it is required to open with. Cheaper and more
    /// honest than assuming the grid is full: this pack's sheet is 252px for a 250px grid.
    /// </summary>
    private static int PngHeight(string path)
    {
        try
        {
            using FileStream stream = File.OpenRead(path);

            Span<byte> header = stackalloc byte[24];

            if (
                stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length
            )
            {
                return 0;
            }

            return System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header[20..24]);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return 0;
        }
    }

    /// <summary>
    /// The pack folder relative to the served asset root, from <c>habbicons.asset.root</c> with its
    /// <c>${...}</c> chain expanded — the variable is written in terms of other variables
    /// (<c>${image.library.url}habbicons/dev/</c>), so reading it literally resolves nothing.
    /// </summary>
    private string? ResolveFolder()
    {
        // The store is keyed by token, not by filename: it resolves the name itself so a caller can
        // never name a path.
        JsonNode? variables = gamedata.Read("variables", language: null, out DateTime _);

        if (variables?[AssetRootVariable]?.GetValue<string>() is not string root)
        {
            return null;
        }

        string expanded = Expand(root, variables, depth: 0);
        int marker = expanded.IndexOf(ServedRoot, StringComparison.OrdinalIgnoreCase);

        if (marker < 0 || expanded.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        string folder = expanded[marker..];

        return folder.EndsWith('/') ? folder : folder + "/";
    }

    /// <summary>Substitutes <c>${name}</c> against the same document, bottoming out on depth.</summary>
    private static string Expand(string value, JsonNode variables, int depth)
    {
        if (depth > 4 || !value.Contains("${", StringComparison.Ordinal))
        {
            return value;
        }

        int open = value.IndexOf("${", StringComparison.Ordinal);
        int close = value.IndexOf('}', open);

        if (close < 0)
        {
            return value;
        }

        string name = value[(open + 2)..close];
        string replacement = variables[name]?.GetValue<string>() ?? string.Empty;

        return Expand(
            string.Concat(value.AsSpan(0, open), replacement, value.AsSpan(close + 1)),
            variables,
            depth + 1
        );
    }
}

/// <summary>Where one Habbicon sits on its sheet, as a top-left CSS offset.</summary>
/// <param name="X">Distance from the sheet's left edge, in pixels.</param>
/// <param name="Y">Distance from the sheet's top edge, in pixels.</param>
internal readonly record struct HabbiconFrame(int X, int Y);

/// <summary>
/// Everything a page needs to draw Habbicons: the sheets, the frame sizes, and where each id sits.
/// </summary>
/// <param name="SpritesheetUrl">The Habbicon sheet, on this origin.</param>
/// <param name="CollectionSpritesheetUrl">The collection-icon sheet, or null when the pack has none.</param>
/// <param name="FrameSize">Habbicon frame edge, in pixels.</param>
/// <param name="CollectionIconSize">Collection-icon edge, in pixels.</param>
/// <param name="Icons">Frame by Habbicon id.</param>
/// <param name="Collections">Frame by collection id.</param>
internal sealed record HabbiconArtworkView(
    string SpritesheetUrl,
    string? CollectionSpritesheetUrl,
    int FrameSize,
    int CollectionIconSize,
    IReadOnlyDictionary<int, HabbiconFrame> Icons,
    IReadOnlyDictionary<int, HabbiconFrame> Collections
);
