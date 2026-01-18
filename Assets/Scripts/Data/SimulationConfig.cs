using UnityEngine;

[System.Serializable]
public enum RoadsterPositionCalculationMode
{
    OrbitalElementsDll = 0,
    CustomPhysical = 1
}

[CreateAssetMenu(menuName = "Space/Simulation Config", fileName = "SimulationConfig")]
public class SimulationConfig : ScriptableObject
{
    [Header("Source Data")]
    [Tooltip("Roadster orbital elements CSV as TextAsset.")]
    public TextAsset roadsterCsv;

    [Header("Simulation")]
    [Tooltip("Sim speed: days advanced per real-time second. 1 = 24h/sec.")]
    public float daysPerSecond = 1f;

    [Tooltip("Unity units per kilometer scaling. 10,000 km -> 1 unit by default.")]
    public float kmPerUnit = 10000f;

    [Header("Roadster Tail")]
    [Tooltip("Number of recent positions to render (e.g., 20).")]
    public int tailLength = 20;

    [Tooltip("If enabled, adds extra tail samples while interpolating long gaps.")]
    public bool enableInterpolatedTail = true;

    [Header("Roadster Interpolation")]
    [Tooltip("If enabled, Roadster position is interpolated when gap between consecutive CSV records exceeds the threshold below.")]
    public bool enableInterpolation = true;

    [Tooltip("Interpolate when gap between consecutive records exceeds this many days. Set to 0 to interpolate always.")]
    public float interpolationGapDays = 1f;

    [Header("Roadster Position Calculation")]
    [Tooltip(
        "How Roadster position is calculated:\n" +
        "- OrbitalElementsDll: uses provided OrbitalElements.dll\n" +
        "- CustomPhysical: uses standard orbital mechanics radius r = a(1-e^2)/(1+e cos(nu))")]
    public RoadsterPositionCalculationMode positionCalculationMode = RoadsterPositionCalculationMode.OrbitalElementsDll;

    [Header("HUD")]
    [Tooltip("If enabled, HUD time follows the interpolated simulation time (smooth). If disabled, HUD shows current record time (steps).")]
    public bool showInterpolatedHudTime = true;

    [Header("Date window (UTC)")]
    [Tooltip("e.g. 2018-02-07 03:00:00")]
    public string startDateUtcIso;

    [Tooltip("e.g. 2019-10-08 23:59:59")]
    public string endDateUtcIso;
}

