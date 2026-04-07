using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarketPulseX.Converters
{
    public sealed class IntArrayOrSingleConverter : JsonConverter<int[]?>
    {
        public override int[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.Number)
                return new[] { reader.GetInt32() };

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var values = new List<int>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return values.ToArray();

                    if (reader.TokenType != JsonTokenType.Number)
                        throw new JsonException($"Expected number inside array, got {reader.TokenType}.");

                    values.Add(reader.GetInt32());
                }
            }

            throw new JsonException($"Unexpected token {reader.TokenType} for condition codes.");
        }

        public override void Write(Utf8JsonWriter writer, int[]? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value.Length == 1)
            {
                writer.WriteNumberValue(value[0]);
                return;
            }

            writer.WriteStartArray();

            foreach (var item in value)
                writer.WriteNumberValue(item);

            writer.WriteEndArray();
        }
    }
}
