namespace FileServer;

using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

internal static class CryptoServices
{
    private static Aes _aes = GetAes();

    public const ushort KeyLength = 256;
    public const ushort IvLength = 128;

    public static byte[] GenerateRandomKey(ushort keyLength) =>
        RandomNumberGenerator.GetBytes(keyLength);

    public static byte[] CreateHash(byte[] input)
    {
        using (HashAlgorithm algorithm = SHA256.Create())
            return algorithm.ComputeHash(input);
    }

    public static byte[] CreateHash(string input) =>
        CreateHash(Encoding.UTF8.GetBytes(input));

    public static byte[] KeyFromHash(byte[] hash, int length = 16)
    {
        byte[] key = new byte[length];

        for (int i = 0; i < length; i++)
            key[i] = hash[i % hash.Length];

        return key;
    }

    public static async Task<byte[]> EncryptAESAsync(byte[] input, byte[] key, byte[] iv)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, _aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
            {
                await cryptoStream.WriteAsync(input, 0, input.Length);
                await cryptoStream.FlushFinalBlockAsync();

                return memoryStream.ToArray();
            }
        }
    }

    public static async Task<byte[]> DecryptAESAsync(byte[] input, byte[] key, byte[] iv)
    {
        await using (MemoryStream memoryStream = new MemoryStream(input))
        {
            await using (CryptoStream cryptoStream = new CryptoStream(memoryStream, _aes.CreateDecryptor(key, iv), CryptoStreamMode.Read))
            {
                await using (MemoryStream outputStream = new MemoryStream())
                {
                    await cryptoStream.CopyToAsync(outputStream);
                    return outputStream.ToArray();
                }
            }
        }
    }

    public static async Task EncryptStreamAsync(Stream source, Stream destination, byte[] key, byte[] iv)
    {
        await using CryptoStream cryptoStream = new CryptoStream(destination, _aes.CreateEncryptor(key, iv), CryptoStreamMode.Write);
        await source.CopyToAsync(cryptoStream);
    }

    public static async Task DecryptStreamAsync(Stream source, Stream destination, byte[] key, byte[] iv)
    {
        await using CryptoStream cryptoStream = new CryptoStream(source, _aes.CreateDecryptor(key, iv), CryptoStreamMode.Read);
        await cryptoStream.CopyToAsync(destination);
    }

    private static Aes GetAes()
    {
        var a = Aes.Create();
        a.Padding = PaddingMode.PKCS7;
        a.Mode = CipherMode.CBC;
        a.KeySize = KeyLength;
        a.BlockSize = IvLength;
        return a;
    }
}