namespace Template.Web.Domain.ValueObjects.Message
{
    /// <summary>
    /// 设备修改的消息内容
    /// </summary>
    public class DeviceUpdateMsg
    {
        public string Version { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Url { get; set; } = null!;

        public IDictionary<string, DeviceModuleMsg> Mods { get; set; } =
            new Dictionary<string, DeviceModuleMsg>();
    }

    public class DeviceModuleMsg
    {
        public string Version { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string? Checksum { get; set; }
    }
}
