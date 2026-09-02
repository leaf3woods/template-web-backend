using System.Text.Json.Nodes;

namespace Template.Web.Domain.ValueObjects.Message.Base
{
    public interface IAsJsonNode
    {
        JsonNode? AsJson();

        JsonNode? Node { get; set; }
    }
}
