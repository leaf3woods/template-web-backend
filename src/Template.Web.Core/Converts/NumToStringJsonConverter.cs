using System.Text.Json;
using System.Text.Json.Serialization;

namespace Template.Web.Domain.Shared.Convert
{
    public class NumToStringJsonConverter : JsonConverter<string>
    {
        public override string? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        ) =>
            reader.TokenType == JsonTokenType.Number
                ? $"{reader.GetDecimal()}"
                : reader.GetString();

        public override void Write(
            Utf8JsonWriter writer,
            string? value,
            JsonSerializerOptions options
        ) => writer.WriteStringValue(value);
    }
}
