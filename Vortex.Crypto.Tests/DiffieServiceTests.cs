using System;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using Vortex.Crypto.Configuration;
using Xunit;

namespace Vortex.Crypto.Tests;

/// <summary>
/// TEST-01 safety net for <see cref="DiffieService"/>. Per the audit, the DH private/public key
/// pair is generated once in the constructor (one per service instance), not per call, so:
/// <list type="bullet">
/// <item>the SAME instance must answer <see cref="DiffieService.GetPublicKey"/> identically every
/// time;</item>
/// <item>two DIFFERENT instances must land on different public values (fresh randomness happens at
/// construction, not at call time).</item>
/// </list>
/// The safe-prime hex constant below is copied verbatim from <c>DiffieService.DH_SAFE_PRIME_HEX</c>
/// (it is private, so it cannot be read off the class under test) purely so tests can independently
/// recompute the expected generator/prime and cross-check a real Diffie-Hellman exchange by manual
/// modpow.
/// </summary>
public sealed class DiffieServiceTests
{
    private const string DH_SAFE_PRIME_HEX =
        "BA772B7ED412CCEAFB345933614CEC83FD8D5F4DC5EC1DC08382BAD6B0CA97224EF920BD50CCFBE709993C09A766AE87";

    private static readonly BigInteger s_prime = new(DH_SAFE_PRIME_HEX, 16);
    private static readonly BigInteger s_generator = BigInteger.Two;

    private readonly struct Fixture(
        DiffieService diffie,
        RsaService rsa,
        RsaKeyParameters publicKey
    )
    {
        public DiffieService Diffie { get; } = diffie;
        public RsaService Rsa { get; } = rsa;
        public RsaKeyParameters PublicKey { get; } = publicKey;
    }

    private static Fixture NewService()
    {
        (RsaKeyParameters pub, RsaPrivateCrtKeyParameters priv) = TestRsaKeyFactory.Generate();
        IOptions<CryptoConfig> options = TestRsaKeyFactory.ToOptions(pub, priv);
        RsaService rsa = new(options);

        return new Fixture(new DiffieService(rsa), rsa, pub);
    }

    /// <summary>Mirrors <c>DiffieService.EncryptBigInteger</c>'s (private) decode side, using only
    /// the public key -- exactly what a real client does to read the server's signed values.</summary>
    private static BigInteger DecodeSigned(string signedHex, RsaKeyParameters publicKey)
    {
        byte[] signedBytes = Hex.Decode(signedHex);
        byte[] plain = TestRsaKeyFactory.VerifyWithPublicKey(publicKey, signedBytes);

        return new BigInteger(Encoding.UTF8.GetString(plain));
    }

    /// <summary>Mirrors what a real client does before calling <c>GenerateSharedKey</c>: RSA-encrypt
    /// (public key) the decimal string of its own DH public value.</summary>
    private static string EncryptClientPublicKey(BigInteger clientPublic, RsaService rsa)
    {
        byte[] payload = Encoding.UTF8.GetBytes(clientPublic.ToString(10));
        byte[] encrypted = rsa.Encrypt(payload);

        return Hex.ToHexString(encrypted);
    }

    [Fact]
    public void GetPublicKey_SameInstance_IsDeterministicAcrossCalls()
    {
        Fixture fixture = NewService();

        BigInteger first = DecodeSigned(fixture.Diffie.GetPublicKey(), fixture.PublicKey);
        BigInteger second = DecodeSigned(fixture.Diffie.GetPublicKey(), fixture.PublicKey);

        second.Should().Be(first, "the DH keypair is generated once per instance, not per call");
    }

    [Fact]
    public void GetPublicKey_TwoDifferentInstances_ProduceDifferentValues()
    {
        Fixture fixture = NewService();
        DiffieService second = new(fixture.Rsa);

        BigInteger firstValue = DecodeSigned(fixture.Diffie.GetPublicKey(), fixture.PublicKey);
        BigInteger secondValue = DecodeSigned(second.GetPublicKey(), fixture.PublicKey);

        secondValue
            .Should()
            .NotBe(firstValue, "each instance generates its own random DH private key");
    }

    [Fact]
    public void GetSignedPrime_MatchesTheConfiguredSafePrime()
    {
        Fixture fixture = NewService();

        BigInteger decoded = DecodeSigned(fixture.Diffie.GetSignedPrime(), fixture.PublicKey);

        decoded.Should().Be(s_prime);
    }

    [Fact]
    public void GetSignedGenerator_IsTwo()
    {
        Fixture fixture = NewService();

        BigInteger decoded = DecodeSigned(fixture.Diffie.GetSignedGenerator(), fixture.PublicKey);

        decoded.Should().Be(s_generator);
    }

    [Fact]
    public void GenerateSharedKey_MatchesManualModPowComputation()
    {
        Fixture fixture = NewService();

        BigInteger serverPublic = DecodeSigned(fixture.Diffie.GetPublicKey(), fixture.PublicKey);

        // Act as the "client": generate our own DH keypair against the same public group.
        SecureRandom random = new();
        BigInteger clientPrivate = new BigInteger(256, random).SetBit(255);
        BigInteger clientPublic = s_generator.ModPow(clientPrivate, s_prime);

        string encryptedClientPublic = EncryptClientPublicKey(clientPublic, fixture.Rsa);

        byte[] serverComputedShared = fixture.Diffie.GenerateSharedKey(encryptedClientPublic);

        // The client-side computation of the very same shared secret: serverPublic ^ clientPrivate.
        byte[] expectedShared = serverPublic.ModPow(clientPrivate, s_prime).ToByteArrayUnsigned();

        serverComputedShared.Should().Equal(expectedShared);
    }

    [Fact]
    public void GenerateSharedKey_RejectsZero()
    {
        Fixture fixture = NewService();
        string encrypted = EncryptClientPublicKey(BigInteger.Zero, fixture.Rsa);

        Action act = () => fixture.Diffie.GenerateSharedKey(encrypted);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateSharedKey_RejectsOne()
    {
        Fixture fixture = NewService();
        string encrypted = EncryptClientPublicKey(BigInteger.One, fixture.Rsa);

        Action act = () => fixture.Diffie.GenerateSharedKey(encrypted);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateSharedKey_RejectsPrimeMinusOne()
    {
        Fixture fixture = NewService();
        string encrypted = EncryptClientPublicKey(s_prime.Subtract(BigInteger.One), fixture.Rsa);

        Action act = () => fixture.Diffie.GenerateSharedKey(encrypted);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateSharedKey_RejectsThePrimeItself()
    {
        Fixture fixture = NewService();
        string encrypted = EncryptClientPublicKey(s_prime, fixture.Rsa);

        Action act = () => fixture.Diffie.GenerateSharedKey(encrypted);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateSharedKey_AcceptsValueJustInsideTheValidRange()
    {
        Fixture fixture = NewService();
        // Valid range is documented as [2, prime - 1) -- prime - 2 is the top boundary that must
        // still be accepted.
        string encrypted = EncryptClientPublicKey(s_prime.Subtract(BigInteger.Two), fixture.Rsa);

        Action act = () => fixture.Diffie.GenerateSharedKey(encrypted);

        act.Should().NotThrow();
    }
}
