using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Vortex.WebApi.Tests;

/// <summary>
/// Keeps <c>Vortex.WebApi/openapi.json</c> equal to the document this API actually serves.
/// </summary>
/// <remarks>
/// <para>
/// The website generates its TypeScript types from that file
/// (<c>pnpm --filter vortex-web api:types</c>), so the file IS the contract between the two repos —
/// and a committed one rather than a fetch from a running hotel, because the site is worked on from
/// machines that have no emulator, and because a contract change then shows up as a diff in review
/// instead of as a silent <c>undefined</c> in a page.
/// </para>
/// <para>
/// It is a test and not a build step for the reason every generated file should be: something has to
/// fail when the two drift. Add a route, change a record, and this goes red until the file is
/// regenerated — <c>UPDATE_OPENAPI=1 dotnet test --filter OpenApiDocumentTests</c> rewrites it.
/// </para>
/// <para>
/// The test server is the real pipeline (<c>WebApiAppConfigurator.ConfigurePipeline</c> is what puts
/// <c>UseSwagger</c> in it) with every endpoint mapped, so the document here is the document a
/// deployed hotel serves.
/// </para>
/// </remarks>
public sealed class OpenApiDocumentTests
{
    private const string UpdateVariable = "UPDATE_OPENAPI";

    [Fact]
    public async Task Document_MatchesTheCommittedContract()
    {
        await using WebApiTestFactory factory = new();

        HttpResponseMessage response = await factory.Client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();

        string served = Normalise(await response.Content.ReadAsStringAsync());
        string path = ContractPath();

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            await File.WriteAllTextAsync(path, served);

            return;
        }

        File.Exists(path)
            .Should()
            .BeTrue($"{path} is the site's source of types — run UPDATE_OPENAPI=1 to write it");

        string committed = Normalise(await File.ReadAllTextAsync(path));

        committed
            .Should()
            .Be(
                served,
                "the API's shape changed and openapi.json did not — rerun this test with "
                    + $"{UpdateVariable}=1 and commit the result"
            );
    }

    /// <summary>
    /// Re-serialised rather than compared as raw text: property ORDER in Swashbuckle's output is not
    /// a contract, and a comparison that treats it as one fails for a reordering nobody made.
    /// </summary>
    /// <remarks>
    /// The line endings go with it. Git stores this file with LF and hands it back as CRLF or LF
    /// depending on the checkout's <c>core.autocrlf</c>, so a raw comparison would fail on a clone
    /// configured differently from the machine that last wrote it — a red test about nothing.
    /// </remarks>
    private static string Normalise(string json) =>
        JsonSerializer
            .Serialize(
                JsonDocument.Parse(json).RootElement,
                new JsonSerializerOptions { WriteIndented = true }
            )
            .ReplaceLineEndings("\n");

    /// <summary>
    /// bin/Debug/net10.0 → the test project → the solution → Vortex.WebApi. Walked rather than
    /// hard-coded from a working directory, which differs between `dotnet test` and an IDE runner.
    /// </summary>
    private static string ContractPath() =>
        Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "Vortex.WebApi",
                "openapi.json"
            )
        );
}
