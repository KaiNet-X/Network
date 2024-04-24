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
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, _aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
            {
                cryptoStream.Write(input, 0, input.Length);
                cryptoStream.FlushFinalBlock();

                return memoryStream.ToArray();
            }
        }
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

    public static async Task EncryptFileAsync(string path, byte[] key, byte[] iv)
    {
        await using FileStream encFs = File.Create($"{path}.aes");
        await using CryptoStream cryptoStream = new CryptoStream(encFs, _aes.CreateEncryptor(key, iv), CryptoStreamMode.Write);
        await using (FileStream fs = File.OpenRead(path))
        {
            await fs.CopyToAsync(cryptoStream);
        }

        File.Delete(path);
    }

    public static async Task CreateEncryptedFileAsync(string path, byte[] source, byte[] key, byte[] iv)
    {
        await using FileStream encFs = File.Create($"{path}.aes");
        await using CryptoStream cs = new CryptoStream(encFs, _aes.CreateEncryptor(key, iv), CryptoStreamMode.Write);
        await cs.WriteAsync(source);
    }

    public static async Task DecryptFileAsync(string path, byte[] key, byte[] iv)
    {
        await using FileStream encFs = File.OpenRead($"{path}.aes");
        await using CryptoStream cryptoStream = new CryptoStream(encFs, _aes.CreateDecryptor(key, iv), CryptoStreamMode.Read);
        await using FileStream fs = File.Create(path.Replace(".aes", ""));
        
        await cryptoStream.CopyToAsync(fs);
    }

    public static async Task<Stream> DecryptedFileStreamAsync(string path, byte[] key, byte[] iv)
    {
        await using FileStream encFs = File.OpenRead($"{path}.aes");
        await using CryptoStream cryptoStream = new CryptoStream(encFs, _aes.CreateDecryptor(key, iv), CryptoStreamMode.Read);
        FileStream fs = File.Create(path.Replace(".aes", ""));

        await cryptoStream.CopyToAsync(fs);
        fs.Seek(0, SeekOrigin.Begin);
        return fs;
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