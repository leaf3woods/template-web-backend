using System.Text.Json.Nodes;
using Org.Product.Domain.ValueObjects.Message.Base;

namespace Org.Product.Domain.ValueObjects.Message
{
    public class DeviceHealthMsg : IJsonMessage
    {
        public DeviceHealthMsg(string raw)
        {
            Node = JsonNode.Parse(raw) ?? throw new ArgumentException("can't parse json node");
        }

        public JsonNode? Node { get; set; }
    }
}
