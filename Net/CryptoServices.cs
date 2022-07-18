namespace Net;

using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

internal static class CryptoServices
{
    private static Aes _aes = GetAes();

    public static byte[] CreateHash(byte[] input)
    {
        using (HashAlgorithm algorithm = SHA256.Create())
            return algorithm.ComputeHash(input);
    }

    public static byte[] CreateHash(string input) =>
        CreateHash(Encoding.UTF8.GetBytes(input));

    public static byte[] KeyFromHash(byte[] hash)
    {
        byte[] key = new byte[16];

        for (int i = 0; i < 16; i++)
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

    public static byte[] EncryptRSA(byte[] bytes, RSAParameters PublicKey)
    {
        using (var provider = new RSACryptoServiceProvider())
        {
            provider.ImportParameters(PublicKey);
            return provider.Encrypt(bytes, false);
        }
    }

    public static byte[] DecryptRSA(byte[] bytes, RSAParameters PrivateKey)
    {
        using (var provider = new RSACryptoServiceProvider())
        {
            try
            {
                provider.ImportParameters(PrivateKey);
                return provider.Decrypt(bytes, false);

            }
            catch (System.Exception ex)
            {
                throw;
            }
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

    private static Aes GetAes()
    {
        var a = Aes.Create();
        a.Padding = PaddingMode.PKCS7;
        a.Mode = CipherMode.CBC;
        a.KeySize = 128;
        a.BlockSize = 128;
        return a;
    }
}