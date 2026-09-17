using System.Text.Json;
using System.Text.Json.Serialization;

namespace Org.Product.Domain.Shared.Converts;

public class DatetimeStringJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => reader.GetDateTime();

    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options
    ) => writer.WriteStringValue($"{value:yyyy-MM-dd HH:mm:ss}");
}
