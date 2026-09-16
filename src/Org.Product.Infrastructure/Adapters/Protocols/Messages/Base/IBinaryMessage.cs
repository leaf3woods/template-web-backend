namespace Org.Product.Infrastructure.Adapters.Protocols.Messages.Base
{
    public interface IBinaryMessage : IMessage
    {
        static byte[] DefaultFrameHeader
        {
            get => [0xA5, 0x5A];
        }
        byte[] FrameHeader { get; set; }
        int FrameLength
        {
            get => 5 + FrameData.Length;
        }
        byte FrameType { get; set; }
        byte[] FrameData { get; set; }

        byte[] AsFrame();

        bool Verify(byte[] raw);
    }
}
