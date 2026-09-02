using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Vortex.Primitives.Hosting;

/// <summary>
/// Validates the certificate half of an "HTTPS is enabled" claim at startup.
/// <para>
/// The hosts used to skip HTTPS configuration silently when the certificate path or password was
/// blank, and to fail only when Kestrel first tried to load a bad file. Both cases produced a
/// listener that was advertised as HTTPS while not actually serving TLS with the intended
/// certificate. This resolves the claim eagerly so a misconfiguration is a startup error with a
/// precise message instead of a runtime surprise.
/// </para>
/// </summary>
public static class HttpsCertificateValidator
{
    /// <summary>
    /// Checks the certificate configuration for a service that declares HTTPS enabled.
    /// </summary>
    /// <param name="serviceName">Human-readable service name used in messages.</param>
    /// <param name="httpsEnabled">Whether the service claims to serve HTTPS.</param>
    /// <param name="certificatePath">Configured PFX path; empty means "use the dev certificate".</param>
    /// <param name="certificatePassword">Optional PFX password.</param>
    /// <param name="isDevelopment">
    /// Whether the host runs in the Development environment. Only there is falling back to the
    /// ASP.NET Core development certificate acceptable.
    /// </param>
    /// <param name="certificatePathOptionPath">Exact configuration key of the path, quoted in messages.</param>
    public static ListenerSecurityResult ValidateCertificate(
        string serviceName,
        bool httpsEnabled,
        string? certificatePath,
        string? certificatePassword,
        bool isDevelopment,
        string certificatePathOptionPath
    )
    {
        if (!httpsEnabled)
        {
            return ListenerSecurityResult.Allowed();
        }

        if (string.IsNullOrWhiteSpace(certificatePath))
        {
            if (isDevelopment)
            {
                return ListenerSecurityResult.AllowedWithWarning(
                    $"{serviceName} has HTTPS enabled with no certificate configured; falling back "
                        + "to the ASP.NET Core development certificate. This is only valid locally."
                );
            }

            return ListenerSecurityResult.Refused(
                $"Refusing to start {serviceName}: HTTPS is enabled but no certificate is configured "
                    + $"in '{certificatePathOptionPath}'. Outside Development there is no development "
                    + "certificate to fall back on, so the listener would either fail to serve TLS or "
                    + "come up as plain HTTP while being advertised as HTTPS. Configure a PFX "
                    + "certificate, or disable HTTPS for this service and terminate TLS in a reverse "
                    + "proxy in front of it."
            );
        }

        if (!File.Exists(certificatePath))
        {
            return ListenerSecurityResult.Refused(
                $"Refusing to start {serviceName}: HTTPS is enabled and "
                    + $"'{certificatePathOptionPath}' points at '{certificatePath}', which does not "
                    + "exist. Fix the path or disable HTTPS for this service."
            );
        }

        try
        {
            // Load exactly the way the host will, so a wrong password or an unreadable/corrupt PFX
            // is caught here rather than when the first TLS handshake arrives.
            using X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(
                certificatePath,
                certificatePassword
            );

            // DateTime.Now on purpose, and the only place in the server where that is true:
            // X509Certificate2.NotAfter is returned in local time, so UtcNow would compare against
            // the wrong clock and call a certificate expired (or not) by the host's UTC offset.
            if (certificate.NotAfter < DateTime.Now)
            {
                return ListenerSecurityResult.AllowedWithWarning(
                    $"{serviceName}'s HTTPS certificate '{certificatePath}' expired on "
                        + $"{certificate.NotAfter:u}. Clients will reject the connection until it is "
                        + "renewed."
                );
            }

            return ListenerSecurityResult.Allowed();
        }
        catch (Exception ex)
        {
            return ListenerSecurityResult.Refused(
                $"Refusing to start {serviceName}: HTTPS is enabled but the certificate at "
                    + $"'{certificatePath}' could not be loaded ({ex.GetType().Name}: {ex.Message}). "
                    + "Check the file and its password, or disable HTTPS for this service."
            );
        }
    }
}
