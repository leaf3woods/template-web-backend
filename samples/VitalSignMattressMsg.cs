using System.Text.Json.Nodes;
using Org.Product.Infrastructure.Adapters.Protocols.Messages.Base;

namespace Org.Product.Infrastructure.Adapters.Protocols.Messages;

/// <summary>
/// 二进制消息
/// </summary>
public class VitalSignMattressMsg : IBinaryMessage, IJsonMessage, IAsJsonNode
{
    public VitalSignMattressMsg(string raw)
    {
        Node = JsonNode.Parse(raw) ?? throw new ArgumentException("unexcepted frame header");
    }

    public VitalSignMattressMsg(byte[] raw)
    {
        Verify(raw);
        FrameHeader = IBinaryMessage.DefaultFrameHeader;
        FrameType = raw[4];
        FrameData = raw[5..(raw.Length - 4)];
        Node = AsJson();
    }

    public JsonNode? Node { get; set; }
    public byte[] FrameHeader { get; set; } = Array.Empty<byte>();
    public int FrameLength
    {
        get => 5 + FrameData.Length;
    }
    public byte FrameType { get; set; }
    public byte[] FrameData { get; set; } = Array.Empty<byte>();

    public JsonNode? AsJson()
    {
        var node = new JsonObject();
        switch (FrameType)
        {
            case 0x1:
                {
                    node.Add("time", BitConverter.ToInt64(FrameData.AsSpan()[..8]));
                    node.Add("heart", FrameData.ElementAtOrDefault(8));
                    node.Add("breath", FrameData.ElementAtOrDefault(9));
                    node.Add("move", FrameData.ElementAtOrDefault(10));
                    node.Add("state", FrameData.ElementAtOrDefault(11));

                    if (FrameData.Length <= 4)
                    {
                        return node;
                    }
                    // 开发阶段的光功率数据
                    var opticalPowerPayload = FrameData[4..];
                    var opticalPowerValues = Enumerable
                        .Range(0, opticalPowerPayload.Length / 4)
                        .Select(index =>
                            JsonValue.Create<float>(
                                BitConverter.ToSingle(
                                    opticalPowerPayload.AsSpan()[(index * 4)..(index * 4 + 4)]
                                )
                            )
                        )
                        .ToArray();
                    node.Add("opticalPowerValues", new JsonArray(opticalPowerValues));
                    node.Add("opticalPowerCount", opticalPowerValues.Length);
                    return node;
                }
            case 0x2:
                {
                    node.Add("score", FrameData.ElementAtOrDefault(0));
                    node.Add("efficiency", FrameData.ElementAtOrDefault(1));
                    node.Add("pkgCount", FrameData.ElementAtOrDefault(2));
                    node.Add("pkgIndex", FrameData.ElementAtOrDefault(3));
                    //Payload.Add("status", PayloadBuffer.ElementAtOrDefault(4));
                    if (FrameData.Length <= 4)
                    {
                        return node;
                    }

                    var statusValues = FrameData[4..]
                        .Select(x => JsonValue.Create<int>(x))
                        .ToArray();
                    node.Add("status", new JsonArray(statusValues));
                    return node;
                }
            default:
                return null;
        }
    }

    public byte[] AsFrame()
    {
        var payloadCount = 5 + FrameData.Length;
        var headers = new[] { (byte)payloadCount, (byte)(payloadCount >> 8), FrameType };
        var dataBytes = FrameHeader.Concat(headers).Concat(FrameData);
        return dataBytes.AppendCrc32().ToArray();
    }

    public bool Verify(byte[] raw)
    {
        //帧头比对
        if (!raw[..2].SequenceEqual(IBinaryMessage.DefaultFrameHeader))
        {
            return false;
        }
        //帧数组校验
        if (!raw.CheckCrc32())
        {
            throw new ArgumentException("crc check failed");
        }
        return true;
    }
}

public enum MattressState
{
    Leave = 3, //离床
    On = 4, //在床
    Error = 5, //故障
    Offline = 6, //离线
    Occupy = 9, //传感器负荷
    WeakSignal = 10, //信号弱
}
