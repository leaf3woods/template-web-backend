using System.Text.Json.Nodes;

namespace Org.Product.Domain.ValueObjects.Message.Base
{
    public interface IJsonMessage : IMessage
    {
        JsonNode? Node { get; set; }
    }
}
