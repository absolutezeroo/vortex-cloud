using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Turbo.Observability.Configuration;

namespace Turbo.Observability.Dashboard.Http;

internal sealed class DashboardResponseWriter(IOptions<ObservabilityConfig> options)
{
    private const string JsonContentType = "application/json; charset=utf-8";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _cspHeader = BuildCsp(options.Value.FurniIconUrlTemplate);

    public async Task WriteJsonAsync(
        HttpListenerContext ctx,
        int status,
        object payload,
        CancellationToken ct
    )
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Json);

        await WriteBytesAsync(ctx, status, bytes, JsonContentType, ct).ConfigureAwait(false);
    }

    public async Task WriteBytesAsync(
        HttpListenerContext ctx,
        int status,
        byte[] bytes,
        string contentType,
        CancellationToken ct
    )
    {
        var isHeadRequest = IsHeadRequest(ctx.Request.HttpMethod);
        var response = ctx.Response;
        response.StatusCode = status;
        ApplySecurityHeaders(response);
        response.AddHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
        response.AddHeader("Pragma", "no-cache");
        response.AddHeader("Expires", "0");
        response.ContentType = contentType;
        response.ContentLength64 = isHeadRequest ? 0 : bytes.Length;

        if (!isHeadRequest)
            await response.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);

        response.Close();
    }

    private void ApplySecurityHeaders(HttpListenerResponse response)
    {
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["X-XSS-Protection"] = "0";
        response.Headers["Content-Security-Policy"] = _cspHeader;
    }

    private static string BuildCsp(string? furniIconTemplate)
    {
        var imgSrc = "img-src 'self' data:";
        var origin = TryGetIconOrigin(furniIconTemplate);

        if (origin is not null)
            imgSrc += " " + origin;

        return "default-src 'self'; script-src 'self'; style-src 'self'; "
            + imgSrc
            + "; connect-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'none';";
    }

    private static string? TryGetIconOrigin(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
            return null;

        var probe = template.Replace("{name}", "icon", StringComparison.Ordinal);

        return
            Uri.TryCreate(probe, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.GetLeftPart(UriPartial.Authority)
            : null;
    }

    private static bool IsHeadRequest(string? method) =>
        string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase);
}
