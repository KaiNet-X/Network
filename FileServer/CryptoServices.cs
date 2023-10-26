namespace FileServer;

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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

    public static void GenerateKeyPair(out RSAParameters PublicKey, out RSAParameters PrivateKey)
    {
        using (var prov = new RSACryptoServiceProvider(2048))
        {
            PrivateKey = prov.ExportParameters(true);
            PublicKey = prov.ExportParameters(false);
        }
    }

    public static byte[] EncryptAES(byte[] input, byte[] key, byte[] iv)
    {
        byte[] result = null;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, _aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
            {
                cryptoStream.Write(input, 0, input.Length);
                cryptoStream.FlushFinalBlock();

                result = memoryStream.ToArray();
            }
        }

        return result;
    }

    public static byte[] DecryptAES(byte[] input, byte[] key, byte[] iv)
    {
        using (MemoryStream memoryStream = new MemoryStream(input))
        {
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, _aes.CreateDecryptor(key, iv), CryptoStreamMode.Read))
            {
                using (var outputStream = new MemoryStream())
                {
                    cryptoStream.CopyTo(outputStream);
                    return outputStream.ToArray();
                }
            }
        }
    }

    public static async Task<byte[]> EncryptAESAsync(byte[] input, byte[] key, byte[] iv)
    {
        byte[] result = null;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, _aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
            {
                await cryptoStream.WriteAsync(input, 0, input.Length);
                await cryptoStream.FlushFinalBlockAsync();

                result = memoryStream.ToArray();
            }
        }

        return result;
    }

    public static async Task<byte[]> DecryptAESAsync(byte[] input, byte[] key, byte[] iv)
    {
        using (MemoryStream memoryStream = new MemoryStream(input))
        {
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, _aes.CreateDecryptor(key, iv), CryptoStreamMode.Read))
            {
                using (var outputStream = new MemoryStream())
                {
                    await cryptoStream.CopyToAsync(outputStream);
                    return outputStream.ToArray();
                }
            }
        }
    }

    public static async Task EncryptFileAsync(string path, byte[] key, byte[] iv)
    {
        await using FileStream encFs = File.Create($"{path}.aes");
        await using CryptoStream cryptoStream = new CryptoStream(encFs, _aes.CreateEncryptor(key, iv), CryptoStreamMode.Write);
        await using (FileStream fs = File.OpenRead(path))
        {
            byte[] buffer = new byte[1048576];
            int readBytes;

            while ((readBytes = await fs.ReadAsync(buffer.AsMemory())) > 0)
            {
                await cryptoStream.WriteAsync(buffer.AsMemory().Slice(0, readBytes));
            }
        }

        File.Delete(path);
    }

    public static async Task DecryptFileAsync(string path, byte[] key, byte[] iv)
    {
        using FileStream encFs = File.OpenRead($"{path}.aes");
        using CryptoStream cryptoStream = new CryptoStream(encFs, _aes.CreateDecryptor(key, iv), CryptoStreamMode.Read);
        using FileStream fs = File.Create(path.Replace(".aes", ""));

        byte[] buffer = new byte[1048576];
        int readBytes;

        while ((readBytes = await cryptoStream.ReadAsync(buffer.AsMemory())) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory().Slice(0, readBytes));
        }
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