namespace FrostEvo.Core.Networking.Security.Cryptography;

public interface INetworkCipher
{
    void Encrypt(byte[] data, int length);
    void Encrypt(Span<byte> data, int length);

    void Decrypt(byte[] data, int length);
    void Decrypt(Span<byte> data, int length);
    void Decrypt(byte[] data, int index, int length, byte[] outBuffer);
}