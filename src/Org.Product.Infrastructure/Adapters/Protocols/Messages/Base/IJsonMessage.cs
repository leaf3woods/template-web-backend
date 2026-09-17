using System.Text.Json.Nodes;

namespace Org.Product.Infrastructure.Adapters.Protocols.Messages.Base;

public interface IJsonMessage : IMessage
{
    JsonNode? Node { get; set; }
}
