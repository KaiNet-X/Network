namespace Net;

using System;
using System.Security.Cryptography;
using System.Text;

public class CryptographyService
{
    private Aes _aes = GetAes();
    private byte[] _aesKey;

    public byte[] AesKey 
    {
        get => _aesKey;
        set
        {
            _aesKey = value;
            _aes.Key = _aesKey;
        }
    }

    public RSAParameters? PublicKey { get; set; }
    public RSAParameters? PrivateKey { get; set; }

    public static byte[] CreateHash(byte[] input)
    {
        using (HashAlgorithm algorithm = SHA256.Create())
            return algorithm.ComputeHash(input);
    }


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

    public ReadOnlySpan<byte> EncryptRSA(ReadOnlySpan<byte> bytes)
    {
        using (var provider = new RSACryptoServiceProvider())
        {
            provider.ImportParameters(PublicKey.Value);
            return provider.Encrypt(bytes, RSAEncryptionPadding.OaepSHA256);
        }
    }

    public ReadOnlyMemory<byte> EncryptRSA(ReadOnlyMemory<byte> bytes)
    {
        using (var provider = new RSACryptoServiceProvider())
        {
            provider.ImportParameters(PublicKey.Value);
            return provider.Encrypt(bytes.Span, RSAEncryptionPadding.Pkcs1);
        }
    }

    public ReadOnlySpan<byte> DecryptRSA(ReadOnlySpan<byte> bytes)
    {
        using (var provider = new RSACryptoServiceProvider())
        {
            provider.ImportParameters(PrivateKey.Value);
            return provider.Decrypt(bytes, RSAEncryptionPadding.Pkcs1);
        }
    }

    public ReadOnlyMemory<byte> DecryptRSA(ReadOnlyMemory<byte> bytes)
    {
        using (var provider = new RSACryptoServiceProvider())
        {
            provider.ImportParameters(PrivateKey.Value);
            return provider.Decrypt(bytes.Span, RSAEncryptionPadding.OaepSHA256);
        }
    }

    public ReadOnlySpan<byte> EncryptAES(ReadOnlySpan<byte> input, ReadOnlySpan<byte> iv) =>
        _aes.EncryptCbc(input, iv);

    public ReadOnlyMemory<byte> EncryptAES(ReadOnlyMemory<byte> input, ReadOnlyMemory<byte> iv) =>
        _aes.EncryptCbc(input.Span, iv.Span);

    public byte[] DecryptAES(byte[] input, byte[] iv) =>
        _aes.DecryptCbc(input, iv);

    public ReadOnlyMemory<byte> DecryptAES(ReadOnlyMemory<byte> input, ReadOnlyMemory<byte> iv) =>
        _aes.DecryptCbc(input.Span, iv.Span);

    public ReadOnlySpan<byte> DecryptAES(ReadOnlySpan<byte> input, ReadOnlySpan<byte> iv) =>
        _aes.DecryptCbc(input, iv);

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