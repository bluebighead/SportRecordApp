namespace SportRecordApp.Services;

public static class AnimationService
{
    public static async Task PlayCheckInAnimationAsync(VisualElement element)
    {
        if (element == null) return;

        try
        {
            await Task.WhenAll(
                element.ScaleToAsync(0.8, 100, Easing.CubicOut),
                element.FadeToAsync(0.5, 100)
            );

            await Task.WhenAll(
                element.ScaleToAsync(1.2, 150, Easing.CubicIn),
                element.FadeToAsync(1, 150)
            );

            await element.ScaleToAsync(1.0, 100, Easing.SpringOut);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"动画播放失败: {ex.Message}");
        }
    }

    public static async Task PlaySuccessAnimationAsync(VisualElement element)
    {
        if (element == null) return;

        try
        {
            await element.ScaleToAsync(1.3, 200, Easing.SpringOut);
            await element.ScaleToAsync(1.0, 200, Easing.SpringOut);
            
            await element.RotateToAsync(360, 500, Easing.CubicOut);
            element.Rotation = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"成功动画播放失败: {ex.Message}");
        }
    }

    public static async Task PlayPulseAnimationAsync(VisualElement element)
    {
        if (element == null) return;

        try
        {
            for (int i = 0; i < 3; i++)
            {
                await element.ScaleToAsync(1.1, 100, Easing.CubicOut);
                await element.ScaleToAsync(1.0, 100, Easing.CubicIn);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"脉冲动画播放失败: {ex.Message}");
        }
    }

    public static async Task PlayBounceAnimationAsync(VisualElement element)
    {
        if (element == null) return;

        try
        {
            await element.TranslateToAsync(0, -20, 100, Easing.CubicOut);
            await element.TranslateToAsync(0, 0, 200, Easing.BounceOut);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"弹跳动画播放失败: {ex.Message}");
        }
    }

    public static async Task PlayShakeAnimationAsync(VisualElement element)
    {
        if (element == null) return;

        try
        {
            await element.TranslateToAsync(-10, 0, 50, Easing.Linear);
            await element.TranslateToAsync(10, 0, 50, Easing.Linear);
            await element.TranslateToAsync(-10, 0, 50, Easing.Linear);
            await element.TranslateToAsync(10, 0, 50, Easing.Linear);
            await element.TranslateToAsync(0, 0, 50, Easing.Linear);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"抖动动画播放失败: {ex.Message}");
        }
    }
}
