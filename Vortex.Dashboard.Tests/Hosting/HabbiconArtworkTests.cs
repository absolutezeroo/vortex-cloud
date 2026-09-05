using System;
using System.Buffers.Binary;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Observability.Configuration;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// Reading the Habbicon asset pack the way the client reads it.
/// </summary>
/// <remarks>
/// Every failure in this class is silent in production: a wrong flip draws the wrong Habbicon, an
/// unexpanded <c>${...}</c> resolves to no pack at all, and neither errors. The seed's ids and the
/// pack's ids already disagreed once and nothing noticed, which is the whole reason these assertions
/// exist rather than a glance at the page.
/// </remarks>
public sealed class HabbiconArtworkTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "vortex-habbicons-" + Guid.NewGuid().ToString("N")
    );

    private readonly string _pack;

    public HabbiconArtworkTests()
    {
        _pack = Path.Combine(_root, "c_images", "habbicons", "dev");

        Directory.CreateDirectory(Path.Combine(_root, "gamedata"));
        Directory.CreateDirectory(_pack);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp folder is not a failed test.
        }
    }

    [Fact]
    public void ReadsThePackTheExternalVariablesPointAt()
    {
        WriteVariables("${image.library.url}habbicons/dev/");
        WriteSheet("habbicons_spritesheet.png", 252, 252);
        WriteSheet("collection_icons_spritesheet.png", 40, 40);
        WriteMetadata(
            """
            { "habbicons": [ { "id": 28, "x": 0, "y": 210, "name": "duck_duck" } ],
              "collectionIcons": [ { "id": 5, "x": 0, "y": 20 } ] }
            """
        );

        HabbiconArtworkView? view = Build().Read();

        view.Should().NotBeNull();
        // The chain is ${image.library.url} -> ${url.prefix}/c_images/, and only the part the
        // dashboard's own /hotel-assets route can serve survives.
        view!
            .SpritesheetUrl.Should()
            .Be("/hotel-assets/c_images/habbicons/dev/habbicons_spritesheet.png");
        view.CollectionSpritesheetUrl.Should()
            .Be("/hotel-assets/c_images/habbicons/dev/collection_icons_spritesheet.png");
        view.FrameSize.Should().Be(40);
    }

    [Fact]
    public void FlipsFrameCoordinatesToATopLeftOrigin()
    {
        WriteVariables("${image.library.url}habbicons/dev/");
        WriteSheet("habbicons_spritesheet.png", 252, 252);
        WriteSheet("collection_icons_spritesheet.png", 40, 40);
        WriteMetadata(
            """
            { "habbicons": [ { "id": 28, "x": 0, "y": 210 }, { "id": 34, "x": 0, "y": 168 } ],
              "collectionIcons": [ { "id": 7, "x": 0, "y": 0 } ] }
            """
        );

        HabbiconArtworkView view = Build().Read()!;

        // The pack is authored bottom-left: 252 - 210 - 40 = 2. Reading `y` straight would put
        // duck_duck five rows down the sheet, drawing a Frankicon under a duck's id.
        view.Icons[28].Should().Be(new HabbiconFrame(0, 2));
        view.Icons[34].Should().Be(new HabbiconFrame(0, 44));
        // 40 - 0 - 18 for the smaller collection icons, which use their own frame size.
        view.Collections[7].Should().Be(new HabbiconFrame(0, 22));
    }

    [Fact]
    public void DropsFramesTheMetadataDoesNotDescribe()
    {
        WriteVariables("${image.library.url}habbicons/dev/");
        WriteSheet("habbicons_spritesheet.png", 252, 252);
        WriteSheet("collection_icons_spritesheet.png", 40, 40);
        WriteMetadata(
            """
            { "habbicons": [ { "id": 28, "x": 0, "y": 210 }, { "id": 99, "name": "no_coordinates" } ],
              "collectionIcons": [] }
            """
        );

        HabbiconArtworkView view = Build().Read()!;

        // Not defaulted to (0,0): that is a real frame, so id 99 would silently draw duck_duck.
        view.Icons.Should().ContainKey(28).And.NotContainKey(99);
    }

    [Fact]
    public void ReturnsNothingWhenNoPackIsInstalled()
    {
        WriteVariables("${image.library.url}habbicons/dev/");

        // The variable points somewhere real, the files are simply not there. The page falls back to
        // listing codes rather than rendering broken tiles.
        Build().Read().Should().BeNull();
    }

    [Fact]
    public void ReturnsNothingWhenTheVariableIsAbsent()
    {
        File.WriteAllText(Path.Combine(_root, "gamedata", "external_variables.json"), "{}");
        WriteSheet("habbicons_spritesheet.png", 252, 252);
        WriteMetadata("""{ "habbicons": [], "collectionIcons": [] }""");

        Build().Read().Should().BeNull();
    }

    private HabbiconArtwork Build()
    {
        DashboardAssetUrls assets = new(
            Options.Create(new ObservabilityConfig { AssetsLocalRoot = _root })
        );

        return new HabbiconArtwork(
            new GamedataDocumentStore(assets, NullLogger<GamedataDocumentStore>.Instance),
            assets,
            NullLogger<HabbiconArtwork>.Instance
        );
    }

    private void WriteVariables(string assetRoot) =>
        File.WriteAllText(
            Path.Combine(_root, "gamedata", "external_variables.json"),
            $$"""
            {
              "url.prefix": "https://hotel.example",
              "image.library.url": "${url.prefix}/c_images/",
              "habbicons.asset.root": "{{assetRoot}}"
            }
            """
        );

    private void WriteMetadata(string json) =>
        File.WriteAllText(Path.Combine(_pack, "habbicons.json"), json);

    /// <summary>
    /// A PNG that is nothing but a valid IHDR — the height is all this reader takes from the file,
    /// and a real sheet would only make the test slower to read.
    /// </summary>
    private void WriteSheet(string name, int width, int height)
    {
        byte[] png = new byte[24];

        // Signature, then the IHDR length/type a PNG is required to open with.
        ReadOnlySpan<byte> signature =
        [
            0x89,
            (byte)'P',
            (byte)'N',
            (byte)'G',
            0x0D,
            0x0A,
            0x1A,
            0x0A,
        ];
        signature.CopyTo(png);
        BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(8), 13);
        "IHDR"u8.CopyTo(png.AsSpan(12));
        BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(16), width);
        BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(20), height);

        File.WriteAllBytes(Path.Combine(_pack, name), png);
    }
}
