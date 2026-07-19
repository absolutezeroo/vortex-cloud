using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.WebApi.Configuration;
using Vortex.WebApi.Hosting;
using Vortex.WebApi.Services;
using Vortex.WebApi.Session;

namespace Vortex.WebApi.Tests;

/// <summary>
/// Spins up the web API's isolated ASP.NET Core app on an in-memory <see cref="TestServer"/>, built
/// through the very same <see cref="WebApiAppConfigurator"/> and <see cref="WebApiEndpoints"/> the
/// production host uses, so the tests exercise the real routing, validation, CORS and rate-limiting
/// pipeline against in-memory fakes instead of the database.
/// </summary>
internal sealed class WebApiTestFactory : IAsyncDisposable
{
    public const int LoginPermitLimit = 3;

    private readonly WebApplication _app;

    public WebApiTestFactory()
    {
        WebApiConfig config = new WebApiConfig
        {
            Enabled = true,
            AllowedOrigins = new[] { "https://client.test" },
        };
        config.LoginRateLimit = new WebApiConfig.RateLimitOptions
        {
            PermitLimit = LoginPermitLimit,
            WindowSeconds = 60,
            QueueLimit = 0,
        };

        Sessions = new WebApiSessionStore();

        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        builder.Services.AddSingleton(Sessions);
        builder.Services.AddSingleton<IWebApiAuthService>(new FakeAuthService(Sessions));
        builder.Services.AddSingleton<IWebApiPlayerService>(new FakePlayerService());
        builder.Services.AddSingleton<IOptions<WebApiConfig>>(Options.Create(config));

        WebApiAppConfigurator.ConfigureServices(builder.Services, config);

        _app = builder.Build();

        WebApiAppConfigurator.ConfigurePipeline(_app, config);
        WebApiEndpoints.Map(_app);

        _app.Start();

        Client = _app.GetTestClient();
    }

    public WebApiSessionStore Sessions { get; }

    public HttpClient Client { get; }

    /// <summary>A client carrying a valid session cookie for an authenticated account.</summary>
    public HttpClient CreateAuthenticatedClient()
    {
        HttpClient client = _app.GetTestClient();
        string sessionId = Sessions.CreateSession(FakeAuthService.AccountId);
        client.DefaultRequestHeaders.Add(
            "Cookie",
            $"{WebApiHttpContextExtensions.SessionCookieName}={sessionId}"
        );

        return client;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync().ConfigureAwait(false);
    }
}
