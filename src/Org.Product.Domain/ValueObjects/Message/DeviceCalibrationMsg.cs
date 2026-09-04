using Org.Product.Domain.Utilities;
using Org.Product.Domain.ValueObjects.Message.Base;

namespace Org.Product.Domain.ValueObjects.Message
{
    public class DeviceCalibrationMsg : IBinaryMessage
    {
        public DeviceCalibrationMsg()
        {
            var timeOffset = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            FrameHeader = IBinaryMessage.DefaultFrameHeader;
            FrameType = 0x20;
            FrameData = BitConverter.GetBytes(timeOffset);
        }

        public byte[] FrameHeader { get; set; }
        public int FrameLength
        {
            get => 5 + FrameData.Length;
        }
        public byte FrameType { get; set; }
        public byte[] FrameData { get; set; }

        public byte[] AsFrame()
        {
            var payloadCount = 5 + FrameData.Length;
            var headers = new[] { (byte)payloadCount, (byte)(payloadCount >> 8), FrameType };
            var dataBytes = FrameHeader.Concat(headers).Concat(FrameData);
            return [.. dataBytes.AppendCrc32()];
        }

        public bool Verify(byte[] raw)
        {
            if (!raw.CheckCrc32())
            {
                throw new ArgumentException("crc check failed");
            }
            return true;
        }
    }
}
