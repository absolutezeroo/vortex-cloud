using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Vortex.WebApi.Tests;

/// <summary>
/// Second-factor enrolment from the website.
/// </summary>
/// <remarks>
/// The rules worth a test are the two that protect an account rather than the two that make the
/// screen work: a secret is not stored until a code proves an authenticator holds it, and a session
/// that already has a factor cannot be handed a fresh one — otherwise a stolen cookie installs its
/// own factor and locks the owner out of their own account.
/// </remarks>
public sealed class TwoFactorEndpointTests
{
    [Fact]
    public async Task Status_IsFalseBeforeEnrolment()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        JsonElement status = await GetJsonAsync(client, "/api/user/twofactor");

        status.GetProperty("enabled").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Enrolment_StoresNothingUntilACodeConfirmsIt()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        JsonElement started = await PostJsonAsync(
            client,
            "/api/user/twofactor/startregistration",
            null
        );

        started.GetProperty("secret").GetString().Should().NotBeNullOrEmpty();
        started.GetProperty("uri").GetString().Should().StartWith("otpauth://");

        // The secret is out, and the account still has no factor.
        JsonElement midway = await GetJsonAsync(client, "/api/user/twofactor");
        midway.GetProperty("enabled").GetBoolean().Should().BeFalse();

        HttpResponseMessage confirmed = await client.PostAsJsonAsync(
            "/api/user/twofactor/enable",
            new
            {
                secret = started.GetProperty("secret").GetString(),
                code = FakeMfaService.ValidCode,
            }
        );

        confirmed.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement after = await GetJsonAsync(client, "/api/user/twofactor");
        after.GetProperty("enabled").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Enrolment_RefusesAWrongCodeAndStaysOff()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/twofactor/enable",
            new { secret = "SECRET", code = "000000" }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        JsonElement after = await GetJsonAsync(client, "/api/user/twofactor");
        after.GetProperty("enabled").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Start_IsRefusedWhenAFactorIsAlreadyEnrolled()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        await client.PostAsJsonAsync(
            "/api/user/twofactor/enable",
            new { secret = "SECRET", code = FakeMfaService.ValidCode }
        );

        HttpResponseMessage second = await client.PostAsync(
            "/api/user/twofactor/startregistration",
            null
        );

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Disable_NeedsACodeFromTheFactorItIsRemoving()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        await client.PostAsJsonAsync(
            "/api/user/twofactor/enable",
            new { secret = "SECRET", code = FakeMfaService.ValidCode }
        );

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/twofactor/disable",
            new { code = "000000" }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetJsonAsync(client, "/api/user/twofactor"))
            .GetProperty("enabled")
            .GetBoolean()
            .Should()
            .BeTrue();

        HttpResponseMessage removed = await client.PostAsJsonAsync(
            "/api/user/twofactor/disable",
            new { code = FakeMfaService.ValidCode }
        );

        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetJsonAsync(client, "/api/user/twofactor"))
            .GetProperty("enabled")
            .GetBoolean()
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Status_IsA401WhenSignedOut()
    {
        await using WebApiTestFactory factory = new();

        HttpResponseMessage response = await factory.Client.GetAsync("/api/user/twofactor");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url) =>
        await ReadAsync(await client.GetAsync(url));

    private static async Task<JsonElement> PostJsonAsync(
        HttpClient client,
        string url,
        HttpContent? content
    ) => await ReadAsync(await client.PostAsync(url, content));

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
