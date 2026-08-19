using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Vortex.Supervisor;

/// <summary>
///     The supervisor's whole authentication story: one shared secret, presented either as a bearer
///     header (scripts, curl) or as an HttpOnly cookie set by exchanging that header once (the
///     browser, whose <c>EventSource</c> cannot send headers on the console stream).
///     <para>
///     There is deliberately no login against the staff table: this surface has to answer while the
///     emulator — and therefore the database — is down, which is precisely when an operator needs it.
///     </para>
/// </summary>
public static class SupervisorAuth
{
    public const string CookieName = "vortex_supervisor";

    /// <summary>
    ///     Compares in constant time. A byte-by-byte comparison leaks the length of the matching
    ///     prefix through timing, which turns guessing the secret into a per-character search.
    /// </summary>
    public static bool TokenMatches(string? presented, string expected)
    {
        if (string.IsNullOrEmpty(presented))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(presented),
            Encoding.UTF8.GetBytes(expected)
        );
    }

    /// <summary>Pulls the presented secret out of the bearer header, falling back to the cookie.</summary>
    public static string? ExtractToken(HttpRequest request)
    {
        string? header = request.Headers.Authorization.ToString();

        if (
            !string.IsNullOrEmpty(header)
            && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        )
        {
            return header["Bearer ".Length..].Trim();
        }

        return request.Cookies.TryGetValue(CookieName, out string? cookie) ? cookie : null;
    }
}
