using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Turbo.Contracts.Plugins;
using Turbo.WebApi.Configuration;
using Turbo.WebApi.Http;
using Turbo.WebApi.Services;
using Turbo.WebApi.Session;

namespace Turbo.WebApi;

public sealed class WebApiModule : IHostPluginModule
{
    public string Key => "turbo-webapi";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<WebApiConfig>(
            builder.Configuration.GetSection(WebApiConfig.SECTION_NAME)
        );

        services.AddSingleton<WebApiSessionStore>();
        services.AddSingleton<WebApiResponseWriter>();
        services.AddSingleton<IWebApiAuthService, WebApiAuthService>();
        services.AddSingleton<IWebApiPlayerService, WebApiPlayerService>();
        services.AddHostedService<WebApiService>();
    }
}
