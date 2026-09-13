using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Vortex.WebApi.Tests;

/// <summary>
/// habbo.com's "Protection du compte", from the website: the two security questions, the challenge
/// that lifts the lock, and the trusted places that stop it being put again.
/// </summary>
/// <remarks>
/// The line these routes are built on is what each demands before it acts. Changing the questions is
/// a change to the recovery path and demands the PASSWORD; answering the challenge demands the
/// ANSWERS and nothing else, because someone who could produce the password would not be standing in
/// front of it. Every test here is one side of that line.
/// </remarks>
public sealed class SafetyQuestionsEndpointTests
{
    private const string Password = FakeAuthService.ValidPassword;

    [Fact]
    public async Task Status_SaysUnconfiguredAndTrustedToStartWith()
    {
        // An account with no questions is trusted everywhere: there is nothing to challenge with,
        // and claiming otherwise would lock every account out of its own settings.
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        JsonElement status = await GetJsonAsync(client, "/api/user/safetyquestions");

        status.GetProperty("configured").GetBoolean().Should().BeFalse();
        status.GetProperty("trusted").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Save_NeedsTheCurrentPassword()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/safetyquestions",
            new
            {
                question1 = 1,
                answer1 = "whiskers",
                question2 = 4,
                answer2 = "paris",
                currentPassword = "wrong",
            }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetJsonAsync(client, "/api/user/safetyquestions"))
            .GetProperty("configured")
            .GetBoolean()
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Save_StoresTheQuestionNumbersAndNotTheirWording()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage saved = await client.PostAsJsonAsync(
            "/api/user/safetyquestions",
            new
            {
                question1 = 3,
                answer1 = "whiskers",
                question2 = 7,
                answer2 = "paris",
                currentPassword = Password,
            }
        );

        saved.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement status = await GetJsonAsync(client, "/api/user/safetyquestions");

        status.GetProperty("configured").GetBoolean().Should().BeTrue();
        status.GetProperty("question1").GetInt32().Should().Be(3);
        status.GetProperty("question2").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task Save_RefusesTheSameQuestionTwice()
    {
        await using WebApiTestFactory factory = new();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/safetyquestions",
            new
            {
                question1 = 2,
                answer1 = "a",
                question2 = 2,
                answer2 = "b",
                currentPassword = Password,
            }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unlock_NeedsTheRightAnswers()
    {
        await using WebApiTestFactory factory = new();
        factory.SafetyQuestions.Configure();

        HttpClient client = factory.CreateAuthenticatedClient(trusted: false);

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/safetyquestions/unlock",
            new { answer1 = "wrong", answer2 = "wrong" }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await refused.Content.ReadAsStringAsync()).Should().Contain("safetylock.invalid_answer");

        (await GetJsonAsync(client, "/api/user/safetyquestions"))
            .GetProperty("trusted")
            .GetBoolean()
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Unlock_TrustsTheSessionAndLiftsTheLock()
    {
        await using WebApiTestFactory factory = new();
        factory.SafetyQuestions.Configure();
        await factory.SafetyLock.ArmAsync(FakeAuthService.AccountId);

        HttpClient client = factory.CreateAuthenticatedClient(trusted: false);

        HttpResponseMessage unlocked = await client.PostAsJsonAsync(
            "/api/user/safetyquestions/unlock",
            new
            {
                answer1 = FakeSafetyQuestionsService.Answer1,
                answer2 = FakeSafetyQuestionsService.Answer2,
            }
        );

        unlocked.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement status = await GetJsonAsync(client, "/api/user/safetyquestions");

        status.GetProperty("trusted").GetBoolean().Should().BeTrue();
        status.GetProperty("locked").GetBoolean().Should().BeFalse();

        // "Débloquer juste pour cette fois": the session was trusted, the PLACE was not.
        factory.TrustedLocations.Trusted.Should().BeEmpty();
    }

    [Fact]
    public async Task Unlock_RemembersThePlaceOnlyWhenAsked()
    {
        await using WebApiTestFactory factory = new();
        factory.SafetyQuestions.Configure();

        HttpClient client = factory.CreateAuthenticatedClient(trusted: false);

        await client.PostAsJsonAsync(
            "/api/user/safetyquestions/unlock",
            new
            {
                answer1 = FakeSafetyQuestionsService.Answer1,
                answer2 = FakeSafetyQuestionsService.Answer2,
                trust = true,
            }
        );

        factory.TrustedLocations.Trusted.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAvatar_IsRefusedByAnUntrustedSession()
    {
        // habbo.com's own gate, and the only one it puts anywhere:
        //   avatar-create.html -> `Session.isTrusted() ? create() : safetyLockModal.open()...`
        await using WebApiTestFactory factory = new();
        factory.SafetyQuestions.Configure();

        HttpClient client = factory.CreateAuthenticatedClient(trusted: false);

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/avatars",
            new
            {
                name = "Newcomer",
                figure = "hd-180-1",
                gender = "M",
            }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await refused.Content.ReadAsStringAsync()).Should().Contain("safetylock.required");
    }

    [Fact]
    public async Task CreateAvatar_IsAllowedOnceTheChallengeIsAnswered()
    {
        await using WebApiTestFactory factory = new();
        factory.SafetyQuestions.Configure();

        HttpClient client = factory.CreateAuthenticatedClient(trusted: false);

        await client.PostAsJsonAsync(
            "/api/user/safetyquestions/unlock",
            new
            {
                answer1 = FakeSafetyQuestionsService.Answer1,
                answer2 = FakeSafetyQuestionsService.Answer2,
            }
        );

        HttpResponseMessage created = await client.PostAsJsonAsync(
            "/api/user/avatars",
            new
            {
                name = "Newcomer",
                figure = "hd-180-1",
                gender = "M",
            }
        );

        created.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TrustedLocationsReset_IsRefusedByAnUntrustedSession()
    {
        // Forgetting every place is how a thief would make sure the OWNER is challenged on their
        // next sign-in, from their own home.
        await using WebApiTestFactory factory = new();
        factory.SafetyQuestions.Configure();
        await factory.TrustedLocations.TrustAsync(FakeAuthService.AccountId, "somewhere");

        HttpClient client = factory.CreateAuthenticatedClient(trusted: false);

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/trustedlocations/reset",
            new { }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        factory.TrustedLocations.Trusted.Should().ContainSingle();
    }

    [Fact]
    public async Task TrustedLocationsReset_ForgetsThemAllAndSaysHowMany()
    {
        await using WebApiTestFactory factory = new();
        await factory.TrustedLocations.TrustAsync(FakeAuthService.AccountId, "home");
        await factory.TrustedLocations.TrustAsync(FakeAuthService.AccountId, "the library");

        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage reset = await client.PostAsJsonAsync(
            "/api/user/trustedlocations/reset",
            new { }
        );

        reset.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument
            .Parse(await reset.Content.ReadAsStringAsync())
            .RootElement.GetProperty("forgotten")
            .GetInt32()
            .Should()
            .Be(2);

        factory.TrustedLocations.Trusted.Should().BeEmpty();
    }

    [Fact]
    public async Task Disable_NeedsThePassword_AndThenClearsTheQuestions()
    {
        await using WebApiTestFactory factory = new();
        factory.SafetyQuestions.Configure();

        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage refused = await client.PostAsJsonAsync(
            "/api/user/safetyquestions/disable",
            new { currentPassword = "wrong" }
        );

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        HttpResponseMessage cleared = await client.PostAsJsonAsync(
            "/api/user/safetyquestions/disable",
            new { currentPassword = Password }
        );

        cleared.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetJsonAsync(client, "/api/user/safetyquestions"))
            .GetProperty("configured")
            .GetBoolean()
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Status_IsA401WhenSignedOut()
    {
        await using WebApiTestFactory factory = new();

        HttpResponseMessage response = await factory.Client.GetAsync("/api/user/safetyquestions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);

        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
