namespace FileServer;

using System.Collections.Generic;
using System.Text.Json;

public class AuthService
{
    private readonly EncryptionPair encryption;

    private readonly string PasswordPath;
    private readonly string EncryptionPath;
    private Dictionary<string, byte[]> users;

    public AuthService()
    {
        PasswordPath = $@"{Directory.GetCurrentDirectory()}/Users.json";
        EncryptionPath = $@"{Directory.GetCurrentDirectory()}/Enc.json";
        encryption = InitializeEncryption();
    }

    private EncryptionPair InitializeEncryption()
    {
        if (File.Exists(EncryptionPath))
        {
            using var file = File.OpenRead(EncryptionPath);

            return JsonSerializer.Deserialize<EncryptionPair>(file);
        }
        else
        {
            var enc = new EncryptionPair(CryptoServices.GenerateRandomKey(CryptoServices.KeyLength / 8), CryptoServices.GenerateRandomKey(CryptoServices.IvLength / 8));

            using var file = File.OpenWrite(EncryptionPath);

            JsonSerializer.Serialize(file, enc);

            return enc;
        }
    }

    public async Task LoadUsersAsync()
    {
        FileStream file = null;
        if (!File.Exists(PasswordPath))
        {
            file = File.Create(PasswordPath);
            file.WriteByte((byte)'{');
            file.WriteByte((byte)'}');
            await file.FlushAsync();
            file.Seek(0, SeekOrigin.Begin);
        }

        file ??= File.OpenRead(PasswordPath);

        users = await JsonSerializer.DeserializeAsync<Dictionary<string, byte[]>>(file);

        await file.DisposeAsync();
    }

    public async Task AddUser(string username, string password)
    {
        if (users.ContainsKey(username)) return;

        var pHash = CryptoServices.CreateHash(password);
        var encHash = await CryptoServices.EncryptAESAsync(pHash, encryption.Key, encryption.Iv);

        users.Add(username, encHash);
    }

    public async Task<bool> CheckUserAsync(string username, string password)
    {
        if (!users.ContainsKey(username)) return false;

        var pHash = CryptoServices.CreateHash(password);
        var encHash = await CryptoServices.EncryptAESAsync(pHash, encryption.Key, encryption.Iv);

        return encHash.SequenceEqual(users[username]);
    }

    public async Task<byte[]> GetUserKeyAsync(string username, string password)
    {
        return await CheckUserAsync(username, password) ?
            CryptoServices.KeyFromHash(CryptoServices.CreateHash(password)) : Array.Empty<byte>();
    }

    private record struct EncryptionPair(byte[] Key, byte[] Iv);
}
