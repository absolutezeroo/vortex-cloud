using System.Text;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using Turbo.Primitives.Crypto;

namespace Turbo.Crypto;

public sealed class DiffieService : IDiffieService
{
    private const int DH_PRIMES_BIT_SIZE = 128;
    private readonly BigInteger _dhGenerator;
    private readonly BigInteger _dhPrime;
    private readonly BigInteger _dhPrivate;
    private readonly BigInteger _dhPublic;
    private readonly IRsaService _rsaService;

    public DiffieService(IRsaService rsaService)
    {
        SecureRandom random = new SecureRandom();
        _rsaService = rsaService;

        // Generate probable primes for DH parameters
        _dhPrime = BigInteger.ProbablePrime(DH_PRIMES_BIT_SIZE, random);
        _dhGenerator = BigInteger.ProbablePrime(DH_PRIMES_BIT_SIZE, random);

        while (_dhGenerator.CompareTo(_dhPrime) <= 0)
        {
            _dhPrime = BigInteger.ProbablePrime(DH_PRIMES_BIT_SIZE, random);
            _dhGenerator = BigInteger.ProbablePrime(DH_PRIMES_BIT_SIZE, random);
        }

        (_dhPrime, _dhGenerator) = (_dhGenerator, _dhPrime);

        _dhPrivate = BigInteger.ProbablePrime(DH_PRIMES_BIT_SIZE, random);
        _dhPublic = _dhGenerator.ModPow(_dhPrivate, _dhPrime);
    }

    public byte[] GenerateSharedKey(string publicKey)
    {
        BigInteger pubKey = DecryptBigInteger(publicKey);
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
        BigInteger publicKey = DecryptBigInteger(publicKeyStr);
        BigInteger? sharedKey = publicKey.ModPow(_dhPrivate, _dhPrime);

        return sharedKey.ToByteArrayUnsigned();
    }

    public string GetPublicKey()
    {
        return EncryptBigInteger(_dhPublic);
    }
}
