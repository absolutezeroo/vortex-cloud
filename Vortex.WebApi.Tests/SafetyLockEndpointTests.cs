using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Vortex.WebApi.Tests;

/// <summary>
/// The account safety lock, from the website.
/// </summary>
/// <remarks>
/// The rule worth holding is that the password is demanded in BOTH directions. Requiring it only to
/// unlock reads like the safe choice and is the wrong one: a thief holding the session could then
/// throw the lock and leave the owner unable to spend in their own hotel.
/// </remarks>
public sealed class SafetyLockEndpointTests
{
    [Fact]
    public async Task Status_IsOffToStartWith()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        JsonElement status = await GetJsonAsync(client, "/api/user/safetylock");

        status.GetProperty("locked").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Lock_NeedsThePasswordToGoON()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/safetylock",
            new { locked = true, currentPassword = "wrong" }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetJsonAsync(client, "/api/user/safetylock"))
            .GetProperty("locked")
            .GetBoolean()
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Lock_AndUnlock_BothTakeThePassword()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage locked = await client.PostAsJsonAsync(
            "/api/user/safetylock",
            new { locked = true, currentPassword = FakeAuthService.ValidPassword }
        );

        locked.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetJsonAsync(client, "/api/user/safetylock"))
            .GetProperty("locked")
            .GetBoolean()
            .Should()
            .BeTrue();

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/safetylock",
            new { locked = false, currentPassword = "wrong" }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetJsonAsync(client, "/api/user/safetylock"))
            .GetProperty("locked")
            .GetBoolean()
            .Should()
            .BeTrue();

        HttpResponseMessage lifted = await client.PostAsJsonAsync(
            "/api/user/safetylock",
            new { locked = false, currentPassword = FakeAuthService.ValidPassword }
        );

        lifted.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetJsonAsync(client, "/api/user/safetylock"))
            .GetProperty("locked")
            .GetBoolean()
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Status_IsA401WhenSignedOut()
    {
        await using WebApiTestFactory factory = new();

        HttpResponseMessage response = await factory.Client.GetAsync("/api/user/safetylock");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);

        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
