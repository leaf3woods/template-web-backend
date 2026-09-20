using System.Text.Json.Nodes;

namespace Org.Product.Infrastructure.Protocols.Messages.Base;

public interface IAsJsonNode
{
    JsonNode? AsJson();

    JsonNode? Node { get; set; }
}
