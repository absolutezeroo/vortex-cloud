using System;
using System.Text;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using Vortex.Primitives.Crypto;

namespace Vortex.Crypto;

public sealed class DiffieService : IDiffieService
{
    // Fixed safe-prime Diffie-Hellman group (p = 2q + 1 with q prime, p ≡ 7 mod 8) so that
    // generator 2 generates the prime-order-q subgroup. This replaces the previous randomly
    // generated 128-bit probable primes, which offered no subgroup guarantees and were within
    // reach of discrete-log attacks.
    //
    // The size is protocol-constrained: the client encrypts its DH public key as a decimal
    // string inside a single PKCS#1 block of the 1024-bit RSA key (max 117 bytes), which caps
    // the prime at ~384 bits (116 decimal digits). This remains below the ≥2048-bit NIST
    // recommendation — an accepted residual risk imposed by the legacy client handshake.
    private const string DH_SAFE_PRIME_HEX =
        "BA772B7ED412CCEAFB345933614CEC83FD8D5F4DC5EC1DC08382BAD6B0CA97224EF920BD50CCFBE709993C09A766AE87";
    private const int DH_PRIVATE_BIT_SIZE = 380;
    private readonly BigInteger _dhGenerator;
    private readonly BigInteger _dhPrime;
    private readonly BigInteger _dhPrivate;
    private readonly BigInteger _dhPublic;
    private readonly IRsaService _rsaService;

    public DiffieService(IRsaService rsaService)
    {
        SecureRandom random = new SecureRandom();
        _rsaService = rsaService;

        _dhPrime = new BigInteger(DH_SAFE_PRIME_HEX, 16);
        _dhGenerator = BigInteger.Two;

        _dhPrivate = new BigInteger(DH_PRIVATE_BIT_SIZE, random).SetBit(DH_PRIVATE_BIT_SIZE - 1);
        _dhPublic = _dhGenerator.ModPow(_dhPrivate, _dhPrime);
    }

    public byte[] GenerateSharedKey(string publicKey)
    {
        BigInteger pubKey = ValidateClientPublicKey(DecryptBigInteger(publicKey));
        BigInteger? sharedKey = pubKey.ModPow(_dhPrivate, _dhPrime);

        return sharedKey.ToByteArrayUnsigned();
    }

    public BigInteger DecryptBigInteger(string str)
    {
        byte[]? bytes = Hex.Decode(str);
        byte[] decrypted = _rsaService.Decrypt(bytes);
        string intStr = Encoding.UTF8.GetString(decrypted);

        return new BigInteger(intStr);
    }

    public string GetSignedPrime()
    {
        return EncryptBigInteger(_dhPrime);
    }

    public string GetSignedGenerator()
    {
        return EncryptBigInteger(_dhGenerator);
    }

    public string EncryptBigInteger(BigInteger integer)
    {
        string? str = integer.ToString(10);
        byte[] bytes = Encoding.UTF8.GetBytes(str);

        // Use RSA to sign the byte array
        byte[] encrypted = _rsaService.Sign(bytes);

        return Hex.ToHexString(encrypted).ToLower();
    }

    public byte[] GetSharedKey(string publicKeyStr)
    {
        BigInteger publicKey = ValidateClientPublicKey(DecryptBigInteger(publicKeyStr));
        BigInteger? sharedKey = publicKey.ModPow(_dhPrivate, _dhPrime);

        return sharedKey.ToByteArrayUnsigned();
    }

    private BigInteger ValidateClientPublicKey(BigInteger publicKey)
    {
        // Reject trivial/out-of-range values (0, 1, p-1, ≥ p) that would force a predictable
        // shared secret or enable small-subgroup confinement.
        if (
            publicKey.CompareTo(BigInteger.Two) < 0
            || publicKey.CompareTo(_dhPrime.Subtract(BigInteger.One)) >= 0
        )
        {
            throw new ArgumentException("Client DH public key is out of range.");
        }

        return publicKey;
    }

    public string GetPublicKey()
    {
        return EncryptBigInteger(_dhPublic);
    }
}
