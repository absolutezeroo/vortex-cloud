using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Vortex.WebApi.Tests;

/// <summary>
/// Changing the address the account signs in with.
/// </summary>
/// <remarks>
/// The address IS the login identifier, so moving it is how an account is taken for good — which is
/// why the current password is required from a caller who already holds a session cookie, and why
/// each refusal has to reach the page as something it can say. That mapping is what these cover: a
/// wrong password and an address someone else holds are different answers, not one generic failure.
/// </remarks>
public sealed class EmailEndpointTests
{
    [Fact]
    public async Task Get_AnswersTheCurrentAddress()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        JsonElement email = await GetJsonAsync(client, "/api/user/email");

        email.GetProperty("email").GetString().Should().Be(FakeEmailService.StartingEmail);

        // Always false: nothing in this server can send to an address, so none was ever confirmed.
        email.GetProperty("verified").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Change_NeedsTheCurrentPassword()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/email/change",
            new { currentPassword = "wrong", email = "new@vortex.test" }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetJsonAsync(client, "/api/user/email"))
            .GetProperty("email")
            .GetString()
            .Should()
            .Be(FakeEmailService.StartingEmail);
    }

    [Fact]
    public async Task Change_IsA409WhenSomebodyElseHoldsTheAddress()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/email/change",
            new
            {
                currentPassword = FakeAuthService.ValidPassword,
                email = FakeEmailService.TakenEmail,
            }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Change_AsksForTheSecondFactorWhenTheAccountHasOne()
    {
        await using WebApiTestFactory factory = new();
        factory.Emails.MfaEnrolled = true;
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/email/change",
            new { currentPassword = FakeAuthService.ValidPassword, email = "new@vortex.test" }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadAsync(refused))
            .GetProperty("error")
            .GetString()
            .Should()
            .Be("pocket.auth.mfa_required");
    }

    [Fact]
    public async Task Change_MovesTheAddress()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage changed = await client.PostAsJsonAsync(
            "/api/user/email/change",
            new { currentPassword = FakeAuthService.ValidPassword, email = "new@vortex.test" }
        );

        changed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetJsonAsync(client, "/api/user/email"))
            .GetProperty("email")
            .GetString()
            .Should()
            .Be("new@vortex.test");
    }

    [Fact]
    public async Task Change_IsA401WhenSignedOut()
    {
        await using WebApiTestFactory factory = new();

        HttpResponseMessage response = await factory.Client.PostAsJsonAsync(
            "/api/user/email/change",
            new { currentPassword = FakeAuthService.ValidPassword, email = "new@vortex.test" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url) =>
        await ReadAsync(await client.GetAsync(url));

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
}
