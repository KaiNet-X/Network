namespace Net.JsonResolvers;

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

internal class RSAContractResolver : JsonConverter<RSAParameters>
{
    public RSAContractResolver()
    {

    }

    public override RSAParameters Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var rsa = new RSAParameters();
        var keys = new List<Dictionary<string, byte[]>>();

        var currentProp = "";
        var currentNumber = 0;
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                case JsonTokenType.EndObject:
                case JsonTokenType.StartArray:
                case JsonTokenType.EndArray:
                case JsonTokenType.String:
                case JsonTokenType.Number:
                case JsonTokenType.PropertyName:
                    break;
            }
        }

        return rsa;
    }

    public override void Write(Utf8JsonWriter writer, RSAParameters value, JsonSerializerOptions options)
    {
        void Write(byte[] bytes, string name)
        {
            writer.WriteStartArray(name);
            foreach (byte b in bytes)
            {
                writer.WriteNumberValue(b);
            }
            writer.WriteEndArray();
        }

        writer.WriteStartObject();

        Write(value.D, "D");
        Write(value.DP, "DP");
        Write(value.DQ, "DQ");
        Write(value.Exponent, "E");
        Write(value.InverseQ, "IQ");
        Write(value.Modulus, "M");
        Write(value.Q, "Q");
        Write(value.P, "P");

        writer.WriteEndObject();
    }
}
