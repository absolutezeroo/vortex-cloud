using System;
using System.IO;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Observability.Configuration;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// The write path for the files the game client downloads.
/// </summary>
/// <remarks>
/// These are not rows. A half-written furnidata is a hotel that will not start, and nothing between
/// the save button and the client would notice. So what is asserted here is not that a value lands —
/// it is that a refused write leaves the previous file byte-for-byte intact, and that a copy of it
/// exists somewhere the public route cannot serve.
/// </remarks>
public sealed class GamedataDocumentStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "vortex-gamedata-" + Guid.NewGuid().ToString("N")
    );

    private readonly GamedataDocumentStore _store;

    public GamedataDocumentStoreTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "gamedata"));

        _store = new GamedataDocumentStore(
            new DashboardAssetUrls(
                Options.Create(new ObservabilityConfig { AssetsLocalRoot = _root })
            ),
            NullLogger<GamedataDocumentStore>.Instance
        );
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a test run over.
        }
    }

    private string WriteVariables(string json)
    {
        string path = Path.Combine(_root, "gamedata", "external_variables.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Write_ReplacesTheValueAndKeepsADatedCopy()
    {
        string path = WriteVariables("""{"imager.prefix":"http://old"}""");

        GamedataWriteResult result = _store.Write(
            "variables",
            null,
            null,
            root =>
            {
                ((JsonObject)root)["imager.prefix"] = "http://new";
                return root;
            }
        );

        result.Success.Should().BeTrue();
        File.ReadAllText(path).Should().Contain("http://new");

        // The backup is a sibling of gamedata/, never inside it: HotelAssets serves that folder to
        // anyone, so a copy placed under it would be downloadable by the same anonymous route.
        File.Exists(result.Backup).Should().BeTrue();
        result.Backup.Should().Contain("gamedata_backups");
        Path.GetDirectoryName(result.Backup).Should().NotBe(Path.GetDirectoryName(path));
        File.ReadAllText(result.Backup).Should().Contain("http://old");
    }

    [Fact]
    public void Write_RefusesWhenTheFileMovedUnderneath()
    {
        WriteVariables("""{"a":"1"}""");

        // What the page believed when it loaded, an hour and somebody else's save ago.
        DateTime stale = DateTime.UtcNow.AddHours(-1);

        GamedataWriteResult result = _store.Write("variables", null, stale, root => root);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("stale_file");
    }

    [Fact]
    public void Write_LeavesTheFileUntouchedWhenTheMutationDeclines()
    {
        string path = WriteVariables("""{"a":"1"}""");
        string before = File.ReadAllText(path);

        GamedataWriteResult result = _store.Write("variables", null, null, _ => null);

        result.Success.Should().BeFalse();
        File.ReadAllText(path).Should().Be(before);
    }

    [Fact]
    public void Write_RefusesAnUnknownFileRatherThanBuildingAPath()
    {
        // The token comes off a URL. Anything not in the closed map must be refused here, not
        // concatenated into a filename.
        _store
            .Write("../../etc/passwd", null, null, root => root)
            .Error.Should()
            .Be("unknown_file");
        _store.Write("gamedata", null, null, root => root).Error.Should().Be("unknown_file");
    }

    [Fact]
    public void Read_ReturnsNothingForAFileThatDoesNotParse()
    {
        WriteVariables("{ this is not json");

        _store.Read("variables", null, out _).Should().BeNull();
    }

    [Theory]
    [InlineData("fr", true)]
    [InlineData("pt-br", true)]
    [InlineData("../fr", false)]
    [InlineData("fr/../..", false)]
    [InlineData("", false)]
    public void LanguageCodesThatCouldClimbOutOfGamedataAreRefused(string code, bool allowed)
    {
        GamedataDocumentStore.IsLanguageCode(code).Should().Be(allowed);
    }

    [Fact]
    public void OnlyTheTextsFileExistsPerLanguage()
    {
        // The client's registry gives a per-language URL to the texts and nothing else; furnidata is
        // loaded from one property, once. Resolving a language for it would promise something the
        // client cannot do.
        _store.TryResolve("texts", "fr", out string texts).Should().BeTrue();
        texts.Should().Contain(Path.Combine("gamedata", "fr"));

        _store.TryResolve("furnidata", "fr", out _).Should().BeFalse();
    }
}
