using System;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Parameters;
using Vortex.Crypto.Configuration;
using Xunit;

namespace Vortex.Crypto.Tests;

/// <summary>
/// TEST-01 safety net for <see cref="RsaService"/>: encrypt/decrypt round-trips at the configured
/// key size, sign/verify round-trips, and that corrupted ciphertext throws instead of silently
/// returning wrong bytes (PKCS#1 padding failure).
/// </summary>
public sealed class RsaServiceTests
{
    private static (RsaService Service, RsaKeyParameters PublicKey) NewService()
    {
        (RsaKeyParameters pub, RsaPrivateCrtKeyParameters priv) = TestRsaKeyFactory.Generate();
        IOptions<CryptoConfig> options = TestRsaKeyFactory.ToOptions(pub, priv);

        return (new RsaService(options), pub);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("a somewhat longer payload that still fits in a single 1024-bit RSA/PKCS#1 block")]
    public void EncryptThenDecrypt_RoundTrips_ToOriginalBytes(string plaintext)
    {
        (RsaService service, _) = NewService();
        byte[] original = Encoding.UTF8.GetBytes(plaintext);

        byte[] encrypted = service.Encrypt(original);
        byte[] decrypted = service.Decrypt(encrypted);

        decrypted.Should().Equal(original);
    }

    [Fact]
    public void Encrypt_ProducesCiphertextDifferentFromPlaintext()
    {
        (RsaService service, _) = NewService();
        byte[] original = Encoding.UTF8.GetBytes("do not leak me in the clear");

        byte[] encrypted = service.Encrypt(original);

        encrypted.Should().NotEqual(original);
    }

    [Fact]
    public void Encrypt_IsNonDeterministic_AcrossCalls()
    {
        // PKCS#1 v1.5 encryption pads with random bytes, so the same plaintext must not encrypt to
        // the same ciphertext twice -- a regression here would mean padding got dropped.
        (RsaService service, _) = NewService();
        byte[] original = Encoding.UTF8.GetBytes("same plaintext, twice");

        byte[] first = service.Encrypt(original);
        byte[] second = service.Encrypt(original);

        first.Should().NotEqual(second);
        service.Decrypt(first).Should().Equal(original);
        service.Decrypt(second).Should().Equal(original);
    }

    [Fact]
    public void SignThenVerifyWithPublicKey_RecoversOriginalBytes()
    {
        (RsaService service, RsaKeyParameters publicKey) = NewService();
        byte[] original = Encoding.UTF8.GetBytes("398471209384712093847120938471209");

        byte[] signed = service.Sign(original);
        byte[] recovered = TestRsaKeyFactory.VerifyWithPublicKey(publicKey, signed);

        recovered.Should().Equal(original);
    }

    [Fact]
    public void Decrypt_CorruptedCiphertext_ThrowsInsteadOfReturningWrongData()
    {
        (RsaService service, _) = NewService();
        byte[] encrypted = service.Encrypt(Encoding.UTF8.GetBytes("legit payload"));

        // Flip a byte in the middle of the ciphertext block -- PKCS#1 padding must reject this.
        encrypted[encrypted.Length / 2] ^= 0xFF;

        Action act = () => service.Decrypt(encrypted);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Decrypt_RandomGarbageOfCorrectBlockSize_Throws()
    {
        (RsaService service, RsaKeyParameters publicKey) = NewService();
        byte[] garbage = new byte[(publicKey.Modulus.BitLength + 7) / 8];
        new Random(42).NextBytes(garbage);
        // PKCS#1 v1.5 requires the first byte to be 0x00; force it invalid so this is not a flaky,
        // astronomically-unlikely-to-pass-by-chance assertion.
        garbage[0] = 0xFF;

        Action act = () => service.Decrypt(garbage);

        act.Should().Throw<Exception>();
    }
}
