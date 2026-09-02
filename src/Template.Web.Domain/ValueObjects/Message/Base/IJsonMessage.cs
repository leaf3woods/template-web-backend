using System.Text.Json.Nodes;

namespace Template.Web.Domain.ValueObjects.Message.Base
{
    public interface IJsonMessage : IMessage
    {
        JsonNode? Node { get; set; }
    }
}
