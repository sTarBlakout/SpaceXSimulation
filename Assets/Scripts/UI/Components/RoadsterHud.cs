using System;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Displays current orbital data and simulation time (local).
/// </summary>
public class RoadsterHud : MonoBehaviour
{
    [SerializeField] private TMP_Text output;

    private readonly StringBuilder _sb = new StringBuilder(256);

    public void SetData(RoadsterRecord record, int currentIndex, int totalCount)
    {
        SetData(record, record.UtcDate, currentIndex, totalCount);
    }

    public void SetData(RoadsterRecord record, DateTime displayUtc, int currentIndex, int totalCount)
    {
        if (output == null || totalCount <= 0)
            return;

        var localTime = displayUtc.ToLocalTime();

        _sb.Clear();
        _sb.AppendLine($"Time (local): {localTime:yyyy-MM-dd HH:mm:ss}");
        _sb.AppendLine($"Semi-major axis (AU): {record.SemiMajorAxisAu:F6}");
        _sb.AppendLine($"Eccentricity: {record.Eccentricity:F6}");
        _sb.AppendLine($"Inclination (deg): {record.InclinationDeg:F4}");
        _sb.AppendLine($"Longitude of asc. node (deg): {record.LongitudeOfAscendingNodeDeg:F4}");
        _sb.AppendLine($"Argument of periapsis (deg): {record.ArgumentOfPeriapsisDeg:F4}");
        _sb.AppendLine($"Mean anomaly (deg): {record.MeanAnomalyDeg:F4}");
        _sb.AppendLine($"True anomaly (deg): {record.TrueAnomalyDeg:F4}");
        _sb.Append($"Record: {currentIndex}/{totalCount}");

        output.text = _sb.ToString();
    }
}

