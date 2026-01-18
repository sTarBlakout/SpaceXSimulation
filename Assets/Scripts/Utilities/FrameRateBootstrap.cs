using UnityEngine;

/// <summary>
/// Sets a consistent target frame rate for the app.
/// </summary>
public static class FrameRateBootstrap
{
    private const int TargetFps = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFps;
    }
}


