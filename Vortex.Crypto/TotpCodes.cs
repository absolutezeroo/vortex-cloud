using System;
using System.Security.Cryptography;
using System.Text;

namespace Vortex.Crypto;

/// <summary>
/// Time-based one-time passwords, RFC 6238 over RFC 4226 (HMAC-SHA1, 30-second steps, 6 digits) --
/// the parameters every authenticator app assumes when the <c>otpauth://</c> URI does not say
/// otherwise, so they are fixed here rather than made configurable.
///
/// <para>
/// Written out rather than taken from a package: the algorithm is thirty lines of a frozen
/// specification, and the Base32 alphabet it needs is another twenty. A dependency here would be one
/// more thing to keep current for code that cannot change.
/// </para>
/// </summary>
public static class TotpCodes
{
    private const int DIGITS = 6;
    private const int STEP_SECONDS = 30;

    /// <summary>
    /// How many steps either side of the current one are accepted. One covers the clock skew between
    /// the server and a phone; more would widen the window a stolen code stays usable in.
    /// </summary>
    private const int TOLERANCE_STEPS = 1;

    private const string BASE32_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>A fresh 160-bit secret, Base32-encoded -- the key length HMAC-SHA1 is defined for.</summary>
    public static string GenerateSecret() => ToBase32(RandomNumberGenerator.GetBytes(20));

    /// <summary>
    /// The <c>otpauth://</c> URI an authenticator app reads from a QR code. <paramref name="issuer" />
    /// and <paramref name="account" /> are what the app shows in its list, so they are what tells an
    /// operator which of their entries belongs to this hotel.
    /// </summary>
    public static string BuildUri(string secret, string issuer, string account) =>
        $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}"
        + $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={DIGITS}&period={STEP_SECONDS}";

    /// <summary>
    /// Whether <paramref name="code" /> is valid for <paramref name="secret" /> at
    /// <paramref name="utcNow" />, within <see cref="TOLERANCE_STEPS" /> either side. Comparison is
    /// fixed-time: a code is a secret-derived value, and comparing it with string equality leaks how
    /// much of a guess was right.
    /// </summary>
    public static bool Verify(string? secret, string? code, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        string normalized = code.Trim().Replace(" ", string.Empty);

        if (normalized.Length != DIGITS)
        {
            return false;
        }

        byte[] key;

        try
        {
            key = FromBase32(secret);
        }
        catch (FormatException)
        {
            return false;
        }

        long step = ToUnixSeconds(utcNow) / STEP_SECONDS;
        bool matched = false;

        // Every candidate is computed even after a match: returning early would make the response
        // time say which step matched, and with it the drift between the server and the phone.
        for (long offset = -TOLERANCE_STEPS; offset <= TOLERANCE_STEPS; offset++)
        {
            matched |= FixedTimeEquals(Compute(key, step + offset), normalized);
        }

        return matched;
    }

    /// <summary>The code for a given step -- exposed so tests can assert against RFC 4226's vectors.</summary>
    public static string Compute(byte[] key, long step)
    {
        byte[] counter = BitConverter.GetBytes(step);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }

        byte[] hash = HMACSHA1.HashData(key, counter);

        // RFC 4226 dynamic truncation: the low nibble of the last byte picks the 4-byte window.
        int offset = hash[^1] & 0x0F;
        int binary =
            ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString("D" + DIGITS);
    }

    public static string ToBase32(byte[] data)
    {
        StringBuilder builder = new((data.Length * 8 / 5) + 1);
        int buffer = 0;
        int bits = 0;

        foreach (byte value in data)
        {
            buffer = (buffer << 8) | value;
            bits += 8;

            while (bits >= 5)
            {
                bits -= 5;
                builder.Append(BASE32_ALPHABET[(buffer >> bits) & 0x1F]);
            }
        }

        if (bits > 0)
        {
            builder.Append(BASE32_ALPHABET[(buffer << (5 - bits)) & 0x1F]);
        }

        return builder.ToString();
    }

    public static byte[] FromBase32(string value)
    {
        string normalized = value.Trim().TrimEnd('=').ToUpperInvariant();
        int buffer = 0;
        int bits = 0;
        byte[] output = new byte[normalized.Length * 5 / 8];
        int written = 0;

        foreach (char c in normalized)
        {
            int index = BASE32_ALPHABET.IndexOf(c);

            if (index < 0)
            {
                throw new FormatException($"'{c}' is not a Base32 character.");
            }

            buffer = (buffer << 5) | index;
            bits += 5;

            if (bits >= 8)
            {
                bits -= 8;
                output[written++] = (byte)((buffer >> bits) & 0xFF);
            }
        }

        return written == output.Length ? output : output[..written];
    }

    private static long ToUnixSeconds(DateTime utcNow) =>
        new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc)).ToUnixTimeSeconds();

    private static bool FixedTimeEquals(string a, string b) =>
        a.Length == b.Length
        && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(a),
            Encoding.ASCII.GetBytes(b)
        );
}
