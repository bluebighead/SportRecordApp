using Android.Media;
using Android.Content;
using Stream = Android.Media.Stream;

namespace SportRecordApp.Platforms.Android;

public static class SoundHelper
{
    private static MediaPlayer? _checkInPlayer;
    private static MediaPlayer? _successPlayer;
    private static MediaPlayer? _errorPlayer;
    private static readonly object _lock = new();

    public static void PlayCheckInSound()
    {
        try
        {
            lock (_lock)
            {
                var context = global::Android.App.Application.Context;
                if (context == null) return;

                if (_checkInPlayer == null)
                {
                    _checkInPlayer = new MediaPlayer();
                    var fd = context.Resources?.OpenRawResourceFd(Resource.Raw.checkin_sound);
                    if (fd != null)
                    {
                        _checkInPlayer.SetDataSource(fd.FileDescriptor, fd.StartOffset, fd.Length);
                        fd.Close();
                        _checkInPlayer.SetAudioAttributes(new AudioAttributes.Builder()
                            .SetUsage(AudioUsageKind.Media)
                            .SetContentType(AudioContentType.Sonification)
                            .Build());
                        _checkInPlayer.Prepare();
                    }
                }
                else
                {
                    _checkInPlayer.Stop();
                    _checkInPlayer.Prepare();
                }
                
                _checkInPlayer.Start();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"播放打卡音效失败: {ex.Message}");
        }
    }

    public static void PlaySuccessSound()
    {
        try
        {
            lock (_lock)
            {
                var context = global::Android.App.Application.Context;
                if (context == null) return;

                if (_successPlayer == null)
                {
                    _successPlayer = new MediaPlayer();
                    var fd = context.Resources?.OpenRawResourceFd(Resource.Raw.checkin_sound);
                    if (fd != null)
                    {
                        _successPlayer.SetDataSource(fd.FileDescriptor, fd.StartOffset, fd.Length);
                        fd.Close();
                        _successPlayer.SetAudioAttributes(new AudioAttributes.Builder()
                            .SetUsage(AudioUsageKind.Media)
                            .SetContentType(AudioContentType.Sonification)
                            .Build());
                        _successPlayer.Prepare();
                    }
                }
                else
                {
                    _successPlayer.Stop();
                    _successPlayer.Prepare();
                }
                
                _successPlayer.Start();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"播放成功音效失败: {ex.Message}");
        }
    }

    public static void PlayErrorSound()
    {
        try
        {
            lock (_lock)
            {
                var context = global::Android.App.Application.Context;
                if (context == null) return;

                if (_errorPlayer == null)
                {
                    _errorPlayer = new MediaPlayer();
                    var fd = context.Resources?.OpenRawResourceFd(Resource.Raw.error_sound);
                    if (fd != null)
                    {
                        _errorPlayer.SetDataSource(fd.FileDescriptor, fd.StartOffset, fd.Length);
                        fd.Close();
                        _errorPlayer.SetAudioAttributes(new AudioAttributes.Builder()
                            .SetUsage(AudioUsageKind.Media)
                            .SetContentType(AudioContentType.Sonification)
                            .Build());
                        _errorPlayer.Prepare();
                    }
                }
                else
                {
                    _errorPlayer.Stop();
                    _errorPlayer.Prepare();
                }
                
                _errorPlayer.Start();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"播放错误音效失败: {ex.Message}");
        }
    }

    public static void Release()
    {
        try
        {
            lock (_lock)
            {
                _checkInPlayer?.Release();
                _checkInPlayer = null;
                _successPlayer?.Release();
                _successPlayer = null;
                _errorPlayer?.Release();
                _errorPlayer = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"释放音效资源失败: {ex.Message}");
        }
    }
}
