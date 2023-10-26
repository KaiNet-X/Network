﻿namespace Net.Messages;

using System.Security.Cryptography;
using Attributes;

[RegisterMessage]
public sealed class EncryptionMessage : MessageBase
{
    public EncryptionStage Stage { get; set; }
    public RSAParameters RsaPair { get; set; }
    public byte[] AesKey { get; set; }
    public byte[] AesIv { get; set; }

    public EncryptionMessage(RSAParameters param)
    {
        Stage = EncryptionStage.SYN;
        RsaPair = param;
    }

    public EncryptionMessage(byte[] aesKey, byte[] aesIv)
    {
        Stage = EncryptionStage.ACK;
        AesKey = aesKey;
        AesIv = aesIv;
    }

    public EncryptionMessage(EncryptionStage stage)
    {
        this.Stage = stage;
    }

    public EncryptionMessage() { }
}

public enum EncryptionStage
{
    NONE,
    SYN,
    ACK,
    SYNACK
}