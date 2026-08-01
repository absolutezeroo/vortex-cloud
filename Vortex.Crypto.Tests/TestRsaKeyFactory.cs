using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Vortex.Crypto.Configuration;

namespace Vortex.Crypto.Tests;

/// <summary>
/// Generates a fresh, real (never hard-coded/fabricated) RSA key pair for each test run and wraps
/// it as the <see cref="CryptoConfig"/> shape <see cref="RsaService"/> expects, plus a bare
/// "verify with the public key" helper -- <see cref="IRsaService"/> only exposes
/// Encrypt/Decrypt/Sign (both Decrypt and Sign always use the private key), so recovering the
/// plaintext behind a value <see cref="RsaService.Sign"/> produced requires driving BouncyCastle's
/// PKCS#1 engine directly with the public key, exactly like a real client would.
/// </summary>
internal static class TestRsaKeyFactory
{
    public static (RsaKeyParameters PublicKey, RsaPrivateCrtKeyParameters PrivateKey) Generate(
        int strengthBits = 1024
    )
    {
        RsaKeyPairGenerator generator = new();
        generator.Init(
            new RsaKeyGenerationParameters(
                BigInteger.ValueOf(0x10001),
                new SecureRandom(),
                strengthBits,
                80
            )
        );

        AsymmetricCipherKeyPair pair = generator.GenerateKeyPair();

        return ((RsaKeyParameters)pair.Public, (RsaPrivateCrtKeyParameters)pair.Private);
    }

    public static IOptions<CryptoConfig> ToOptions(
        RsaKeyParameters publicKey,
        RsaPrivateCrtKeyParameters privateKey
    )
    {
        CryptoConfig config = new()
        {
            KeySize = publicKey.Exponent.ToString(16),
            PublicKey = publicKey.Modulus.ToString(16),
            PrivateKey = privateKey.Exponent.ToString(16),
            EnableServerToClientEncryption = true,
        };

        return Options.Create(config);
    }

    /// <summary>
    /// The public-key-side inverse of <see cref="RsaService.Sign"/>: recovers the bytes that were
    /// signed, using only the public modulus/exponent (never the private key).
    /// </summary>
    public static byte[] VerifyWithPublicKey(RsaKeyParameters publicKey, byte[] signed)
    {
        Pkcs1Encoding cipher = new(new RsaEngine());
        cipher.Init(false, publicKey);

        return cipher.ProcessBlock(signed, 0, signed.Length);
    }
}
