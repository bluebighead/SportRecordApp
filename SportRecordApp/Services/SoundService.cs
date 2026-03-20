namespace SportRecordApp.Services;

public static class SoundService
{
    public static void PlayCheckInSound()
    {
#if ANDROID
        Platforms.Android.SoundHelper.PlayCheckInSound();
#endif
    }

    public static void PlaySuccessSound()
    {
#if ANDROID
        Platforms.Android.SoundHelper.PlaySuccessSound();
#endif
    }

    public static void PlayErrorSound()
    {
#if ANDROID
        Platforms.Android.SoundHelper.PlayErrorSound();
#endif
    }
}
