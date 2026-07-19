namespace Vortex.Primitives.Crypto;

public interface IRsaService
{
    public byte[] Encrypt(byte[] data);
    public byte[] Decrypt(byte[] data);
    public byte[] Sign(byte[] data);
}
