using System;
using System.IO;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Turbo.Crypto.Configuration;
using Turbo.Primitives.Crypto;

namespace Turbo.Crypto;

public sealed class RsaService : IRsaService
{
    private readonly int _blockSize;
    private readonly BigInteger _exponent;
    private readonly BigInteger _modulus;
    private readonly BigInteger _privateExponent;
    private readonly RsaKeyParameters _privateKey;
    private readonly RsaKeyParameters _publicKey;

    public RsaService(IOptions<CryptoConfig> config)
    {
        _exponent = new BigInteger(config.Value.KeySize, 16);
        _modulus = new BigInteger(config.Value.PublicKey, 16);
        _privateExponent = new BigInteger(config.Value.PrivateKey, 16);
        _publicKey = new RsaKeyParameters(false, _modulus, _exponent);
        _privateKey = new RsaKeyParameters(true, _modulus, _privateExponent);
        _blockSize = (_modulus.BitLength + 7) / 8;
    }

    public byte[] Encrypt(byte[] data)
    {
        Pkcs1Encoding cipher = new Pkcs1Encoding(new RsaEngine());

        cipher.Init(true, _publicKey);

        return cipher.ProcessBlock(data, 0, data.Length);
    }

    public byte[] Decrypt(byte[] data)
    {
        Pkcs1Encoding cipher = new Pkcs1Encoding(new RsaEngine());

        cipher.Init(false, _privateKey);

        return cipher.ProcessBlock(data, 0, data.Length);
    }

    public byte[] Sign(byte[] data)
    {
        Pkcs1Encoding cipher = new Pkcs1Encoding(new RsaEngine());

        cipher.Init(true, _privateKey);

        return ProcessData(cipher, data);
    }

    private static byte[] ProcessData(IAsymmetricBlockCipher cipher, byte[] data)
    {
        MemoryStream outputStream = new MemoryStream();
        int chunkSize = cipher.GetInputBlockSize();

        for (int chunkPosition = 0; chunkPosition < data.Length; chunkPosition += chunkSize)
        {
            int chunkLength = Math.Min(chunkSize, data.Length - chunkPosition);
            byte[]? chunkResult = cipher.ProcessBlock(data, chunkPosition, chunkLength);

            outputStream.Write(chunkResult, 0, chunkResult.Length);
        }

        return outputStream.ToArray();
    }
}
