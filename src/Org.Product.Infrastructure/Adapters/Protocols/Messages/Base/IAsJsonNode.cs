using System.Text.Json.Nodes;

namespace Org.Product.Infrastructure.Adapters.Protocols.Messages.Base;

public interface IAsJsonNode
{
    JsonNode? AsJson();

    JsonNode? Node { get; set; }
}
