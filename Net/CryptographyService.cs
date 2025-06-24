namespace Net;

using System;
using System.Security.Cryptography;

public class CryptographyService
{
    private Aes _aes = GetAes();

    private const ushort KeySize = 256;
    private const ushort BlockSize = 128;

    public byte[] AesKey 
    {
        get => _aes.Key;
        set
        {
            _aes.Key = value;
        }
    }

    public byte[] AesIv
    {
        get => _aes.IV;
        set
        {
            _aes.IV = value;
        }
    }

    public RSAParameters? PublicKey { get; set; }
    public RSAParameters? PrivateKey { get; set; }

    public static byte[] CreateRandomKey(ushort length) => 
        RandomNumberGenerator.GetBytes(length);

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
        using var prov = new RSACryptoServiceProvider(2048);
        PrivateKey = prov.ExportParameters(true);
        PublicKey = prov.ExportParameters(false);
    }

    public byte[] EncryptRSA(ReadOnlySpan<byte> bytes)
    {
        using var provider = new RSACryptoServiceProvider();
        provider.ImportParameters(PublicKey.Value);
        return provider.Encrypt(bytes, RSAEncryptionPadding.Pkcs1);
    }
    
    public byte[] DecryptRSA(ReadOnlySpan<byte> bytes)
    {
        using var provider = new RSACryptoServiceProvider();
        provider.ImportParameters(PrivateKey.Value);
        return provider.Decrypt(bytes, RSAEncryptionPadding.Pkcs1);
    }
    
    public ReadOnlySpan<byte> EncryptAES(ReadOnlySpan<byte> input) =>
        _aes.EncryptCbc(input, AesIv);

    public ReadOnlyMemory<byte> EncryptAES(ReadOnlyMemory<byte> input) =>
        _aes.EncryptCbc(input.Span, AesIv);

    public byte[] DecryptAES(byte[] input) =>
        _aes.DecryptCbc(input.AsSpan(), AesIv);

    public ReadOnlyMemory<byte> DecryptAES(ReadOnlyMemory<byte> input) =>
        _aes.DecryptCbc(input.Span, AesIv);

    public ReadOnlySpan<byte> DecryptAES(ReadOnlySpan<byte> input) =>
        _aes.DecryptCbc(input, AesIv);

    private static Aes GetAes()
    {
        var a = Aes.Create();
        a.Padding = PaddingMode.PKCS7;
        a.Mode = CipherMode.CBC;
        a.KeySize = KeySize;
        a.BlockSize = BlockSize;
        return a;
    }
}