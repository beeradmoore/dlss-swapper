using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DLSS_Swapper.JsonConverters;

internal class HexStringToUintConverter : JsonConverter<uint>
{
    public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            Logger.Error($"Expected string token type, but got {reader.TokenType}.");
            return 0;
        }

        var hexString = reader.GetString();
        if (string.IsNullOrWhiteSpace(hexString))
        {
            return 0;
        }

        var value = Convert.ToUInt32(hexString, 16);


        return value;
    }
                

    public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
