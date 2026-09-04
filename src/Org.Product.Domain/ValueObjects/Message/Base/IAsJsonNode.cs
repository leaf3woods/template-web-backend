using System.Text.Json.Nodes;

namespace Org.Product.Domain.ValueObjects.Message.Base
{
    public interface IAsJsonNode
    {
        JsonNode? AsJson();

        JsonNode? Node { get; set; }
    }
}
