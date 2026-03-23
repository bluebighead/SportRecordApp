namespace SportRecordApp.Models
{
    public class HistoryDeviceDisplayItem
    {
        public string DeviceName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int ConnectCount { get; set; }
        public bool IsSelected { get; set; }

        public string DisplayName => ConnectCount > 0 
            ? $"{DeviceName}（连接{ConnectCount}次）" 
            : DeviceName;

        public HistoryDeviceDisplayItem()
        {
        }

        public HistoryDeviceDisplayItem(string deviceName, string address, int connectCount)
        {
            DeviceName = deviceName;
            Address = address;
            ConnectCount = connectCount;
        }
    }
}
