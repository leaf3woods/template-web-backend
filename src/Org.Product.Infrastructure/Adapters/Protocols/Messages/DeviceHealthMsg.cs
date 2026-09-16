using System.Text.Json.Nodes;
using Org.Product.Infrastructure.Adapters.Protocols.Messages.Base;

namespace Org.Product.Infrastructure.Adapters.Protocols.Messages
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
