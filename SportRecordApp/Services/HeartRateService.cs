using SportRecordApp.Models;

namespace SportRecordApp.Services
{
    public class HeartRateEventArgs : EventArgs
    {
        public int HeartRate { get; }
        public DateTime Timestamp { get; }

        public HeartRateEventArgs(int heartRate)
        {
            HeartRate = heartRate;
            Timestamp = DateTime.Now;
        }
    }

    public static class HeartRateService
    {
        private static bool _isScanning = false;
        private static bool _isConnecting = false;
        private static int _currentHeartRate = 0;
        private static string _connectedDeviceName = string.Empty;
        private static string _connectedDeviceAddress = string.Empty;

        public static bool IsScanning => _isScanning;
        public static bool IsConnecting => _isConnecting;
        public static int CurrentHeartRate => _currentHeartRate;
        public static string ConnectedDeviceName => _connectedDeviceName;
        public static string ConnectedDeviceAddress => _connectedDeviceAddress;

        public static event EventHandler<HeartRateEventArgs>? HeartRateUpdated;
        public static event EventHandler? ConnectionStateChanged;
        public static event EventHandler<string>? ScanError;
        public static event EventHandler<BleDeviceInfo>? DeviceDiscovered;

        public static async Task<bool> StartScanForDevicesAsync()
        {
            if (_isScanning)
            {
                return true;
            }

#if ANDROID
            try
            {
                bool success = await Platforms.Android.HeartRateManager.StartScanForDevicesAsync();
                if (success)
                {
                    _isScanning = true;
                    ConnectionStateChanged?.Invoke(null, EventArgs.Empty);
                }
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动心率扫描失败: {ex.Message}");
                ScanError?.Invoke(null, ex.Message);
                return false;
            }
#else
            return false;
#endif
        }

        public static void StopScanForDevices()
        {
            if (!_isScanning)
            {
                return;
            }

#if ANDROID
            try
            {
                Platforms.Android.HeartRateManager.StopScanForDevices();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停止心率扫描失败: {ex.Message}");
            }
#endif

            _isScanning = false;
            ConnectionStateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static List<BleDeviceInfo> GetDiscoveredDevices()
        {
#if ANDROID
            return Platforms.Android.HeartRateManager.GetDiscoveredDevices();
#else
            return new List<BleDeviceInfo>();
#endif
        }

        public static bool ConnectToDevice(BleDeviceInfo device)
        {
#if ANDROID
            try
            {
                _isConnecting = true;
                ConnectionStateChanged?.Invoke(null, EventArgs.Empty);
                return Platforms.Android.HeartRateManager.ConnectToDevice(device);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接设备失败: {ex.Message}");
                _isConnecting = false;
                ConnectionStateChanged?.Invoke(null, EventArgs.Empty);
                return false;
            }
#else
            return false;
#endif
        }

        public static bool ConnectToDeviceByAddress(string address, string deviceName)
        {
#if ANDROID
            try
            {
                _isConnecting = true;
                ConnectionStateChanged?.Invoke(null, EventArgs.Empty);
                return Platforms.Android.HeartRateManager.ConnectToDeviceByAddress(address, deviceName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接设备失败: {ex.Message}");
                _isConnecting = false;
                ConnectionStateChanged?.Invoke(null, EventArgs.Empty);
                return false;
            }
#else
            return false;
#endif
        }

        public static bool TryAutoReconnect()
        {
            string lastAddress = SettingsService.GetLastConnectedDeviceAddress();
            string lastName = SettingsService.GetLastConnectedDeviceName();
            
            if (string.IsNullOrEmpty(lastAddress))
            {
                return false;
            }

            Console.WriteLine($"尝试自动连接上次设备: {lastName} ({lastAddress})");
            return ConnectToDeviceByAddress(lastAddress, lastName);
        }

        public static bool HasLastConnectedDevice()
        {
            return !string.IsNullOrEmpty(SettingsService.GetLastConnectedDeviceAddress());
        }

        public static string GetLastConnectedDeviceName()
        {
            return SettingsService.GetLastConnectedDeviceName();
        }

        public static void Disconnect()
        {
#if ANDROID
            try
            {
                Platforms.Android.HeartRateManager.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"断开连接失败: {ex.Message}");
            }
#endif

            _isScanning = false;
            _currentHeartRate = 0;
            _connectedDeviceName = string.Empty;
            ConnectionStateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void OnHeartRateReceived(int heartRate)
        {
            _currentHeartRate = heartRate;
            HeartRateUpdated?.Invoke(null, new HeartRateEventArgs(heartRate));
        }

        public static void OnDeviceConnected(string deviceName, string deviceAddress)
        {
            _connectedDeviceName = deviceName;
            _connectedDeviceAddress = deviceAddress;
            _isScanning = false;
            _isConnecting = false;
            
            SettingsService.SetLastConnectedDeviceName(deviceName);
            SettingsService.SetLastConnectedDeviceAddress(deviceAddress);
            SettingsService.AddDeviceToHistory(deviceName, deviceAddress);
            
            ConnectionStateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void OnDeviceDisconnected()
        {
            _connectedDeviceName = string.Empty;
            _connectedDeviceAddress = string.Empty;
            _currentHeartRate = 0;
            _isConnecting = false;
            ConnectionStateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void OnConnectionFailed(string error)
        {
            _isConnecting = false;
            _isScanning = false;
            ScanError?.Invoke(null, error);
            ConnectionStateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void OnDeviceDiscovered(BleDeviceInfo device)
        {
            DeviceDiscovered?.Invoke(null, device);
        }

        public static void OnScanFailed(string error)
        {
            _isScanning = false;
            ScanError?.Invoke(null, error);
            ConnectionStateChanged?.Invoke(null, EventArgs.Empty);
        }

        public static async Task<bool> RequestBluetoothPermissionAsync()
        {
#if ANDROID
            return await Platforms.Android.BluetoothPermissionHelper.RequestBluetoothPermissionAsync();
#else
            return false;
#endif
        }
    }
}
