using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Observability;
using Vortex.WebApi.Http;
using Xunit;

namespace Vortex.WebApi.Tests;

/// <summary>
/// <c>POST /api/user/reports</c> is the only surface here that records something the server never
/// observed, so nothing else fails when it stops working: no exception is grouped, no counter
/// moves, and the reports simply stop arriving. These tests are what notices.
/// </summary>
public sealed class PlayerReportEndpointTests
{
    [Fact]
    public async Task Report_FromAuthenticatedPlayer_EmitsAuditRecord()
    {
        await using WebApiTestFactory factory = new WebApiTestFactory();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/user/reports",
            new
            {
                message = "The room stays blank after the loading bar fills.",
                page = "/client",
                clientVersion = "beta-1",
                roomId = 42,
                console = "TypeError: cannot read property of undefined",
            }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        AuditEvent emitted = factory.Audit.Events.Should().ContainSingle().Subject;
        emitted.Category.Should().Be(AuditCategory.PlayerReport);
        emitted.Action.Should().Be("player.bug_report");
        emitted.RoomId.Should().Be(42);

        // The payload is what an operator actually reads; a record that arrives without the words
        // the player wrote is worse than none, because it looks like the feature works.
        JsonElement data = JsonDocument.Parse(emitted.Data!).RootElement;
        data.GetProperty("message")
            .GetString()
            .Should()
            .Be("The room stays blank after the loading bar fills.");
        data.GetProperty("clientVersion").GetString().Should().Be("beta-1");
    }

    [Fact]
    public async Task Report_WithoutSession_IsRejectedAndEmitsNothing()
    {
        await using WebApiTestFactory factory = new WebApiTestFactory();

        HttpResponseMessage response = await factory.Client.PostAsJsonAsync(
            "/api/user/reports",
            new { message = "anonymous" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        factory.Audit.Events.Should().BeEmpty();
    }

    /// <summary>
    /// The length ceilings are enforced server-side because the audit payload is a JSON column and
    /// this route is reachable by anyone who can sign in. A client that stops trimming must not be
    /// able to write a megabyte per press.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Report_WithoutMessage_IsRejected(string message)
    {
        await using WebApiTestFactory factory = new WebApiTestFactory();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/user/reports",
            new { message }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        factory.Audit.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task Report_OverTheMessageCeiling_IsRejected()
    {
        await using WebApiTestFactory factory = new WebApiTestFactory();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/user/reports",
            new { message = new string('x', SubmitReportRequest.MAX_MESSAGE + 1) }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        factory.Audit.Events.Should().BeEmpty();
    }

    /// <summary>
    /// A report is worth having even when the client could collect nothing around it — a player on
    /// a browser that blocked the console capture still gets to say what happened.
    /// </summary>
    [Fact]
    public async Task Report_WithMessageOnly_IsAccepted()
    {
        await using WebApiTestFactory factory = new WebApiTestFactory();
        HttpClient client = factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/user/reports",
            new { message = "the catalogue will not open" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        AuditEvent emitted = factory.Audit.Events.Single();
        emitted.RoomId.Should().BeNull();
        JsonDocument
            .Parse(emitted.Data!)
            .RootElement.GetProperty("message")
            .GetString()
            .Should()
            .Be("the catalogue will not open");
    }
}
