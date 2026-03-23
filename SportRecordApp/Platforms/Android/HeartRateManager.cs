using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Java.Util;
using SportRecordApp.Models;

namespace SportRecordApp.Platforms.Android
{
    public static class HeartRateManager
    {
        private static BluetoothAdapter? _bluetoothAdapter;
        private static BluetoothLeScanner? _bluetoothLeScanner;
        private static DeviceScanCallback? _scanCallback;
        private static BluetoothGatt? _bluetoothGatt;
        private static HeartRateGattCallback? _gattCallback;
        private static bool _isScanning = false;
        private static Dictionary<string, BluetoothDevice> _discoveredDevices = new();
        private static HashSet<string> _heartRateDevices = new();
        private static System.Threading.Timer? _connectionTimeoutTimer;

        private static readonly UUID HeartRateServiceUuid = UUID.FromString("0000180d-0000-1000-8000-00805f9b34fb");
        private static readonly UUID HeartRateMeasurementUuid = UUID.FromString("00002a37-0000-1000-8000-00805f9b34fb");
        private static readonly UUID ClientCharacteristicConfigUuid = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");

        public static bool IsScanning => _isScanning;

        public static async Task<bool> StartScanForDevicesAsync()
        {
            _discoveredDevices.Clear();
            _heartRateDevices.Clear();

            var context = global::Android.App.Application.Context;
            if (context == null)
            {
                Services.HeartRateService.OnScanFailed("无法获取应用上下文");
                return false;
            }

            var bluetoothManager = context.GetSystemService(Context.BluetoothService) as BluetoothManager;
            if (bluetoothManager == null)
            {
                Services.HeartRateService.OnScanFailed("无法获取蓝牙管理器");
                return false;
            }

            _bluetoothAdapter = bluetoothManager.Adapter;
            if (_bluetoothAdapter == null || !_bluetoothAdapter.IsEnabled)
            {
                Services.HeartRateService.OnScanFailed("蓝牙未开启");
                return false;
            }

            if (!CheckBluetoothPermissions(context))
            {
                Services.HeartRateService.OnScanFailed("缺少蓝牙权限");
                return false;
            }

            _bluetoothLeScanner = _bluetoothAdapter.BluetoothLeScanner;
            if (_bluetoothLeScanner == null)
            {
                Services.HeartRateService.OnScanFailed("无法获取BLE扫描器");
                return false;
            }

            AddConnectedDevices(bluetoothManager);

            try
            {
                _scanCallback = new DeviceScanCallback();
                
                var settings = new ScanSettings.Builder()
                    .SetScanMode(global::Android.Bluetooth.LE.ScanMode.LowLatency)
                    .Build();

                _bluetoothLeScanner.StartScan(null, settings, _scanCallback);
                _isScanning = true;
                return true;
            }
            catch (Exception ex)
            {
                Services.HeartRateService.OnScanFailed($"启动扫描失败: {ex.Message}");
                return false;
            }
        }

        private static void AddConnectedDevices(BluetoothManager bluetoothManager)
        {
            try
            {
                var connectedDevices = bluetoothManager.GetConnectedDevices(ProfileType.Gatt);
                foreach (var device in connectedDevices)
                {
                    if (device != null && !_discoveredDevices.ContainsKey(device.Address))
                    {
                        _discoveredDevices[device.Address] = device;
                        string deviceName = device.Name ?? "未知设备";
                        Console.WriteLine($"发现已连接设备: {deviceName} ({device.Address})");
                        Services.HeartRateService.OnDeviceDiscovered(new BleDeviceInfo(deviceName, device.Address, device));
                    }
                }

                if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr2)
                {
                    var bondedDevices = _bluetoothAdapter?.BondedDevices;
                    if (bondedDevices != null)
                    {
                        foreach (var device in bondedDevices)
                        {
                            if (device != null && !_discoveredDevices.ContainsKey(device.Address))
                            {
                                _discoveredDevices[device.Address] = device;
                                string deviceName = device.Name ?? "未知设备";
                                Console.WriteLine($"发现已配对设备: {deviceName} ({device.Address})");
                                Services.HeartRateService.OnDeviceDiscovered(new BleDeviceInfo(deviceName, device.Address, device));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取已连接设备失败: {ex.Message}");
            }
        }

        public static void StopScanForDevices()
        {
            if (_bluetoothLeScanner != null && _scanCallback != null && _isScanning)
            {
                try
                {
                    _bluetoothLeScanner.StopScan(_scanCallback);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"停止扫描失败: {ex.Message}");
                }
            }

            _isScanning = false;
            _scanCallback = null;
        }

        public static List<BleDeviceInfo> GetDiscoveredDevices()
        {
            var devices = new List<BleDeviceInfo>();
            foreach (var device in _discoveredDevices)
            {
                string deviceName = device.Value.Name ?? "未知设备";
                if (_heartRateDevices.Contains(device.Key))
                {
                    deviceName += " (心率)";
                }
                devices.Add(new BleDeviceInfo(
                    deviceName,
                    device.Value.Address,
                    device.Value
                ));
            }
            return devices;
        }

        public static bool ConnectToDevice(BleDeviceInfo deviceInfo)
        {
            if (deviceInfo.NativeDevice is not BluetoothDevice device)
            {
                Services.HeartRateService.OnConnectionFailed("无效的设备信息");
                return false;
            }

            return ConnectToBluetoothDevice(device);
        }

        public static bool ConnectToDeviceByAddress(string address, string deviceName)
        {
            if (string.IsNullOrEmpty(address))
            {
                Services.HeartRateService.OnConnectionFailed("无效的设备地址");
                return false;
            }

            var context = global::Android.App.Application.Context;
            if (context == null)
            {
                Services.HeartRateService.OnConnectionFailed("无法获取应用上下文");
                return false;
            }

            var bluetoothManager = context.GetSystemService(Context.BluetoothService) as BluetoothManager;
            if (bluetoothManager == null || bluetoothManager.Adapter == null)
            {
                Services.HeartRateService.OnConnectionFailed("无法获取蓝牙适配器");
                return false;
            }

            BluetoothDevice? device = null;
            try
            {
                device = bluetoothManager.Adapter.GetRemoteDevice(address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取远程设备失败: {ex.Message}");
                Services.HeartRateService.OnConnectionFailed($"无法找到设备: {deviceName}");
                return false;
            }

            if (device == null)
            {
                Services.HeartRateService.OnConnectionFailed($"无法找到设备: {deviceName}");
                return false;
            }

            return ConnectToBluetoothDevice(device);
        }

        private static bool ConnectToBluetoothDevice(BluetoothDevice device)
        {
            var context = global::Android.App.Application.Context;
            if (context == null)
            {
                Services.HeartRateService.OnConnectionFailed("无法获取应用上下文");
                return false;
            }

            StopScanForDevices();

            CancelConnectionTimeout();
            _connectionTimeoutTimer = new System.Threading.Timer(OnConnectionTimeout, null, TimeSpan.FromSeconds(15), Timeout.InfiniteTimeSpan);

            _gattCallback = new HeartRateGattCallback();
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                _bluetoothGatt = device.ConnectGatt(context, false, _gattCallback, BluetoothTransports.Le);
            }
            else
            {
                _bluetoothGatt = device.ConnectGatt(context, false, _gattCallback);
            }

            return _bluetoothGatt != null;
        }

        private static void OnConnectionTimeout(object? state)
        {
            Console.WriteLine("连接超时");
            Services.HeartRateService.OnConnectionFailed("连接超时，请重试");
            Disconnect();
        }

        private static void CancelConnectionTimeout()
        {
            _connectionTimeoutTimer?.Dispose();
            _connectionTimeoutTimer = null;
        }

        public static void Disconnect()
        {
            CancelConnectionTimeout();
            
            if (_bluetoothGatt != null)
            {
                try
                {
                    _bluetoothGatt.Close();
                    _bluetoothGatt = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"断开连接失败: {ex.Message}");
                }
            }
            _gattCallback = null;
        }

        private static bool CheckBluetoothPermissions(Context context)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                if (context.CheckSelfPermission(global::Android.Manifest.Permission.BluetoothScan) != Permission.Granted ||
                    context.CheckSelfPermission(global::Android.Manifest.Permission.BluetoothConnect) != Permission.Granted)
                {
                    return false;
                }
            }
            else
            {
                if (context.CheckSelfPermission(global::Android.Manifest.Permission.AccessFineLocation) != Permission.Granted)
                {
                    return false;
                }
            }
            return true;
        }

        private class DeviceScanCallback : ScanCallback
        {
            public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
            {
                base.OnScanResult(callbackType, result);
                
                if (result?.Device != null)
                {
                    var device = result.Device;
                    string address = device.Address;
                    
                    if (!_discoveredDevices.ContainsKey(address))
                    {
                        _discoveredDevices[address] = device;
                        string deviceName = device.Name ?? "未知设备";
                        
                        var scanRecord = result.ScanRecord;
                        if (scanRecord != null)
                        {
                            var serviceUuids = scanRecord.ServiceUuids;
                            if (serviceUuids != null)
                            {
                                foreach (var uuid in serviceUuids)
                                {
                                    if (uuid.Uuid.Equals(HeartRateServiceUuid))
                                    {
                                        _heartRateDevices.Add(address);
                                        deviceName += " (心率)";
                                        break;
                                    }
                                }
                            }
                        }
                        
                        Console.WriteLine($"发现设备: {deviceName} ({address})");
                        Services.HeartRateService.OnDeviceDiscovered(new BleDeviceInfo(deviceName, address, device));
                    }
                }
            }

            public override void OnScanFailed(ScanFailure errorCode)
            {
                base.OnScanFailed(errorCode);
                Services.HeartRateService.OnScanFailed($"扫描失败，错误码: {errorCode}");
            }
        }

        private class HeartRateGattCallback : BluetoothGattCallback
        {
            public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
            {
                base.OnConnectionStateChange(gatt, status, newState);

                if (newState == ProfileState.Connected)
                {
                    CancelConnectionTimeout();
                    Console.WriteLine("已连接到心率设备，正在发现服务...");
                    gatt?.DiscoverServices();
                }
                else if (newState == ProfileState.Disconnected)
                {
                    CancelConnectionTimeout();
                    Console.WriteLine("已断开心率设备");
                    Services.HeartRateService.OnDeviceDisconnected();
                }
                else if (status != GattStatus.Success)
                {
                    CancelConnectionTimeout();
                    Console.WriteLine($"连接状态改变失败: {status}");
                    Services.HeartRateService.OnConnectionFailed($"连接失败: {status}");
                }
            }

            public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
            {
                base.OnServicesDiscovered(gatt, status);

                if (status == GattStatus.Success && gatt != null)
                {
                    var heartRateService = gatt.GetService(HeartRateServiceUuid);
                    if (heartRateService != null)
                    {
                        var heartRateCharacteristic = heartRateService.GetCharacteristic(HeartRateMeasurementUuid);
                        if (heartRateCharacteristic != null)
                        {
                            gatt.SetCharacteristicNotification(heartRateCharacteristic, true);
                            
                            var descriptor = heartRateCharacteristic.GetDescriptor(ClientCharacteristicConfigUuid);
                            if (descriptor != null)
                            {
                                descriptor.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                                gatt.WriteDescriptor(descriptor);
                            }

                            string deviceName = gatt.Device?.Name ?? "未知设备";
                            string deviceAddress = gatt.Device?.Address ?? "";
                            Console.WriteLine($"心率服务已发现，设备: {deviceName}");
                            Services.HeartRateService.OnDeviceConnected(deviceName, deviceAddress);
                        }
                        else
                        {
                            Console.WriteLine("未找到心率测量特征");
                            Services.HeartRateService.OnConnectionFailed("设备不支持心率测量");
                            gatt.Disconnect();
                        }
                    }
                    else
                    {
                        Console.WriteLine("设备不支持心率服务");
                        Services.HeartRateService.OnConnectionFailed("该设备不支持心率服务");
                        gatt.Disconnect();
                    }
                }
                else
                {
                    Console.WriteLine($"服务发现失败: {status}");
                    Services.HeartRateService.OnConnectionFailed($"服务发现失败: {status}");
                    gatt?.Disconnect();
                }
            }

            public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
            {
                base.OnCharacteristicChanged(gatt, characteristic);

                if (characteristic != null && characteristic.Uuid.Equals(HeartRateMeasurementUuid))
                {
                    int heartRate = ParseHeartRate(characteristic);
                    Services.HeartRateService.OnHeartRateReceived(heartRate);
                }
            }

            private int ParseHeartRate(BluetoothGattCharacteristic characteristic)
            {
                var flagObj = characteristic.GetIntValue(GattFormat.Uint8, 0);
                int flag = flagObj != null ? (int)flagObj : 0;
                int heartRate;

                if ((flag & 0x01) != 0)
                {
                    var heartRateObj = characteristic.GetIntValue(GattFormat.Uint16, 1);
                    heartRate = heartRateObj != null ? (int)heartRateObj : 0;
                }
                else
                {
                    var heartRateObj = characteristic.GetIntValue(GattFormat.Uint8, 1);
                    heartRate = heartRateObj != null ? (int)heartRateObj : 0;
                }

                return heartRate;
            }
        }
    }
}
