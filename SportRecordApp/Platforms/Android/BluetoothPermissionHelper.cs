using Android.OS;
using Microsoft.Maui.ApplicationModel;

namespace SportRecordApp.Platforms.Android
{
    public static class BluetoothPermissionHelper
    {
        public static async Task<bool> RequestBluetoothPermissionAsync()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                var scanStatus = await Permissions.CheckStatusAsync<BluetoothScanPermission>();
                if (scanStatus != PermissionStatus.Granted)
                {
                    scanStatus = await Permissions.RequestAsync<BluetoothScanPermission>();
                }

                if (scanStatus != PermissionStatus.Granted)
                {
                    return false;
                }

                var connectStatus = await Permissions.CheckStatusAsync<BluetoothConnectPermission>();
                if (connectStatus != PermissionStatus.Granted)
                {
                    connectStatus = await Permissions.RequestAsync<BluetoothConnectPermission>();
                }

                return connectStatus == PermissionStatus.Granted;
            }
            else
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                return status == PermissionStatus.Granted;
            }
        }
    }
}
