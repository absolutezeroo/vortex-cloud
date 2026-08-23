using System;
using FluentAssertions;
using Vortex.Crypto;
using Xunit;

namespace Vortex.Crypto.Tests;

/// <summary>
/// A hand-written one-time-password implementation is only worth anything if a phone agrees with it,
/// and there is no phone in a test run. RFC 4226 publishes the answers instead: the same key, the
/// same counters, the codes an authenticator must produce. If these pass, an authenticator app will
/// agree; if they fail, every operator who enrolled is locked out.
/// </summary>
public sealed class TotpCodesTests
{
    /// <summary>The ASCII secret "12345678901234567890" from RFC 4226 Appendix D.</summary>
    private static byte[] RfcKey() => System.Text.Encoding.ASCII.GetBytes("12345678901234567890");

    [Theory]
    [InlineData(0, "755224")]
    [InlineData(1, "287082")]
    [InlineData(2, "359152")]
    [InlineData(3, "969429")]
    [InlineData(4, "338314")]
    [InlineData(5, "254676")]
    [InlineData(6, "287922")]
    [InlineData(7, "162583")]
    [InlineData(8, "399871")]
    [InlineData(9, "520489")]
    public void MatchesTheHotpVectorsFromRfc4226(long counter, string expected)
    {
        TotpCodes.Compute(RfcKey(), counter).Should().Be(expected);
    }

    /// <summary>
    /// Base32 is how the secret reaches the phone -- through a QR code or typed by hand -- so a
    /// round trip that loses a bit produces codes nothing else can reproduce. RFC 4648's own vectors.
    /// </summary>
    [Theory]
    [InlineData("", "")]
    [InlineData("f", "MY======")]
    [InlineData("fo", "MZXQ====")]
    [InlineData("foo", "MZXW6===")]
    [InlineData("foob", "MZXW6YQ=")]
    [InlineData("fooba", "MZXW6YTB")]
    [InlineData("foobar", "MZXW6YTBOI======")]
    public void EncodesBase32TheWayRfc4648Says(string plain, string padded)
    {
        string expected = padded.TrimEnd('=');

        TotpCodes.ToBase32(System.Text.Encoding.ASCII.GetBytes(plain)).Should().Be(expected);
        System.Text.Encoding.ASCII.GetString(TotpCodes.FromBase32(padded)).Should().Be(plain);
    }

    [Fact]
    public void AcceptsTheCodeForTheCurrentStep()
    {
        DateTime now = new(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        string secret = TotpCodes.ToBase32(RfcKey());
        string code = TotpCodes.Compute(RfcKey(), ToStep(now));

        TotpCodes.Verify(secret, code, now).Should().BeTrue();
    }

    /// <summary>
    /// One step either side is deliberate -- a phone whose clock is half a minute out still works.
    /// Two is not: a code stays usable for at most a minute and a half, and this is what says so.
    /// </summary>
    [Theory]
    [InlineData(-1, true)]
    [InlineData(1, true)]
    [InlineData(-2, false)]
    [InlineData(2, false)]
    public void AcceptsOneStepOfDriftAndNoMore(int offsetSteps, bool expected)
    {
        DateTime now = new(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        string secret = TotpCodes.ToBase32(RfcKey());
        string code = TotpCodes.Compute(RfcKey(), ToStep(now) + offsetSteps);

        TotpCodes.Verify(secret, code, now).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "755224")]
    [InlineData("MZXW6YTBOI", null)]
    [InlineData("MZXW6YTBOI", "")]
    [InlineData("MZXW6YTBOI", "12345")]
    [InlineData("MZXW6YTBOI", "1234567")]
    [InlineData("not base32 at all!", "755224")]
    public void RefusesAnythingMalformedInsteadOfThrowing(string? secret, string? code)
    {
        TotpCodes.Verify(secret, code, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void GeneratesADistinctUsableSecretEachTime()
    {
        string first = TotpCodes.GenerateSecret();
        string second = TotpCodes.GenerateSecret();

        first.Should().NotBe(second);
        TotpCodes.FromBase32(first).Should().HaveCount(20);
    }

    [Fact]
    public void BuildsAUriAnAuthenticatorCanRead()
    {
        string uri = TotpCodes.BuildUri("MZXW6YTBOI", "Vortex", "ops@example.com");

        uri.Should().StartWith("otpauth://totp/Vortex:ops%40example.com?");
        uri.Should().Contain("secret=MZXW6YTBOI");
        uri.Should().Contain("digits=6");
        uri.Should().Contain("period=30");
    }

    private static long ToStep(DateTime utc) => new DateTimeOffset(utc).ToUnixTimeSeconds() / 30;
}
