using System.Text.Json.Nodes;
using Template.Web.Domain.ValueObjects.Message.Base;

namespace Template.Web.Domain.ValueObjects.Message
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
