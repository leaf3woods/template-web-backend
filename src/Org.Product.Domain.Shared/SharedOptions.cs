using System.Text.Json;
using System.Text.Json.Serialization;

namespace Org.Product.Domain.Shared;

public static class SharedOptions
{
    public static JsonSerializerOptions CustomJsonSerializerOptions { get; set; } =
        new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
}
