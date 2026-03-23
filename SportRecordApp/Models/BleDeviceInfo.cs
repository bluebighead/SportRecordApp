namespace SportRecordApp.Models
{
    public class BleDeviceInfo
    {
        public string Name { get; set; } = "未知设备";
        public string Address { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public object? NativeDevice { get; set; }

        public BleDeviceInfo()
        {
        }

        public BleDeviceInfo(string name, string address, object? nativeDevice = null)
        {
            Name = string.IsNullOrEmpty(name) ? "未知设备" : name;
            Address = address;
            NativeDevice = nativeDevice;
        }
    }
}
