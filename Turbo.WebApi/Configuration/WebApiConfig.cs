using System;

namespace Turbo.WebApi.Configuration;

/// <summary>
/// Configuration for the client-facing web API. The API is the public onboarding surface (login,
/// registration, SSO token), so the security knobs — CORS allow-list, HTTPS/HSTS and per-endpoint
/// rate limits — are all driven from here rather than hard-coded.
/// </summary>
public sealed class WebApiConfig
{
    public const string SECTION_NAME = "Turbo:WebApi";

    public bool Enabled { get; set; } = false;

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 8080;

    public int MaxAvatarsPerAccount { get; set; } = 5;

    public string DefaultFigure { get; set; } =
        "hr-115-42.hd-195-19.ch-3030-82.lg-275-1408.fa-1201.ca-1804-64";

    /// <summary>
    /// Origins permitted by CORS. Empty means "no cross-origin browser access"; a wildcard is never
    /// emitted because the API relies on credentialed (cookie) requests.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>When set, the API also listens on HTTPS and redirects HTTP traffic to it.</summary>
    public bool HttpsEnabled { get; set; } = false;

    public int HttpsPort { get; set; } = 8443;

    /// <summary>Optional PFX certificate used for the HTTPS listener; falls back to the dev certificate.</summary>
    public string? CertificatePath { get; set; }

    public string? CertificatePassword { get; set; }

    /// <summary>Emits HSTS headers (implies clients should only ever reach the API over HTTPS).</summary>
    public bool HstsEnabled { get; set; } = false;

    /// <summary>Fixed-window rate limit applied to <c>POST /api/public/authentication/login</c>.</summary>
    public RateLimitOptions LoginRateLimit { get; set; } =
        new RateLimitOptions
        {
            PermitLimit = 5,
            WindowSeconds = 60,
            QueueLimit = 0,
        };

    /// <summary>Fixed-window rate limit applied to <c>POST /api/public/registration/new</c>.</summary>
    public RateLimitOptions RegistrationRateLimit { get; set; } =
        new RateLimitOptions
        {
            PermitLimit = 3,
            WindowSeconds = 300,
            QueueLimit = 0,
        };

    /// <summary>Fixed-window rate limit applied to <c>GET /api/ssotoken</c>.</summary>
    public RateLimitOptions SsoTokenRateLimit { get; set; } =
        new RateLimitOptions
        {
            PermitLimit = 10,
            WindowSeconds = 60,
            QueueLimit = 0,
        };

    public sealed class RateLimitOptions
    {
        public int PermitLimit { get; set; } = 5;

        public int WindowSeconds { get; set; } = 60;

        public int QueueLimit { get; set; } = 0;
    }
}
