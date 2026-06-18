using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Turbo.WebApi.Http;

internal sealed class WebApiResponseWriter
{
    private const string JsonContentType = "application/json; charset=utf-8";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task WriteJsonAsync(
        HttpListenerContext ctx,
        int status,
        object payload,
        string? sessionId,
        CancellationToken ct
    )
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Json);
        var response = ctx.Response;
        response.StatusCode = status;
        response.ContentType = JsonContentType;
        response.ContentLength64 = bytes.Length;

        ApplyCorsHeaders(ctx.Request, response);

        if (sessionId is not null)
            response.Headers["Set-Cookie"] =
                $"habbo-web-session={sessionId}; Path=/; HttpOnly; SameSite=Lax";

        await response.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);
        response.Close();
    }

    public async Task WriteErrorAsync(
        HttpListenerContext ctx,
        int status,
        string errorCode,
        CancellationToken ct
    ) =>
        await WriteJsonAsync(ctx, status, new { error = errorCode }, null, ct)
            .ConfigureAwait(false);

    private static void ApplyCorsHeaders(HttpListenerRequest req, HttpListenerResponse res)
    {
        var origin = req.Headers["Origin"];

        if (origin is not null)
        {
            res.Headers["Access-Control-Allow-Origin"] = origin;
            res.Headers["Access-Control-Allow-Credentials"] = "true";
            res.Headers["Access-Control-Allow-Headers"] =
                "Content-Type, X-Habbo-Device-ID, x-habbo-api-deviceid, X-Habbo-Device-Type";
            res.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
        }

        res.Headers["Vary"] = "Origin";
    }
}
