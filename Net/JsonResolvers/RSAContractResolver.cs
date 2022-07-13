namespace Net.JsonResolvers;

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

public class RSAContractResolver : JsonConverter<RSAParameters>
{
    public RSAContractResolver()
    {

    }

    public override RSAParameters Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var rsa = new RSAParameters();
        var keys = new Dictionary<string, byte[]>();

        var currentProp = "";
        List<byte> bytes = new List<byte>();
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    currentProp = reader.GetString();
                    break;
                case JsonTokenType.EndArray:
                    keys[currentProp] = bytes.ToArray();
                    bytes.Clear();
                    break;
                case JsonTokenType.Number:
                    bytes.Add(reader.GetByte());
                    break;
                case JsonTokenType.EndObject:
                    goto exit;
            }
        }
        exit:
        if (keys.ContainsKey("M"))
            rsa.Modulus = keys["M"];
        if (keys.ContainsKey("M"))
            rsa.Exponent = keys["E"];
        //rsa.DP = keys["DP"];
        //rsa.D = keys["D"];
        //rsa.DQ = keys["DQ"];
        //rsa.P = keys["P"];
        //rsa.InverseQ = keys["InverseQ"];

        return rsa;
    }

    public override void Write(Utf8JsonWriter writer, RSAParameters value, JsonSerializerOptions options)
    {
        void Write(byte[] bytes, string name)
        {
            if (bytes == null)
            {
                writer.WriteNull(name);
                return;
            }

            writer.WriteStartArray(name);
            foreach (byte b in bytes)
                writer.WriteNumberValue(b);
            writer.WriteEndArray();
        }

        writer.WriteStartObject();

        Write(value.Exponent, "E");
        Write(value.Modulus, "M");
        //Write(value.D, "D");
        //Write(value.DP, "DP");
        //Write(value.DQ, "DQ");
        //Write(value.InverseQ, "IQ");
        //Write(value.Q, "Q");
        //Write(value.P, "P");

        writer.WriteEndObject();
    }
}
