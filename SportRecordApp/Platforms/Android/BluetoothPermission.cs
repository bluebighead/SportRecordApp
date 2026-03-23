using Android;
using Android.OS;
using Microsoft.Maui.ApplicationModel;

namespace SportRecordApp.Platforms.Android
{
    public class BluetoothScanPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            Build.VERSION.SdkInt >= BuildVersionCodes.S
                ? new[] { (Manifest.Permission.BluetoothScan, true) }
                : new[] { (Manifest.Permission.AccessFineLocation, true) };
    }

    public class BluetoothConnectPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            Build.VERSION.SdkInt >= BuildVersionCodes.S
                ? new[] { (Manifest.Permission.BluetoothConnect, true) }
                : Array.Empty<(string, bool)>();
    }
}
