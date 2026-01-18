using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Loads Roadster orbital element records from the provided CSV TextAsset.
/// Expected columns:
/// Epoch JD,Date UTC,Semi-major axis au,Eccentricity,Inclination degrees,
/// Longitude of asc. node degrees,Argument of periapsis degrees,
/// Mean Anomaly degrees,True Anomaly degrees
/// </summary>
public static class RoadsterCsvLoader
{
    public static readonly DateTime DefaultStartUtc = new DateTime(2018, 2, 7, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DefaultEndUtc = new DateTime(2019, 10, 8, 23, 59, 59, DateTimeKind.Utc);

    public static RoadsterRecord[] LoadRecords(TextAsset csv, DateTime? startUtc = null, DateTime? endUtc = null)
    {
        if (csv == null)
        {
            throw new ArgumentNullException(nameof(csv), "CSV TextAsset is required.");
        }

        var minDate = startUtc ?? DefaultStartUtc;
        var maxDate = endUtc ?? DefaultEndUtc;

        var lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var records = new List<RoadsterRecord>(lines.Length);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Skip header if present.
            if (i == 0 && line.StartsWith("Epoch JD", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!TryParseLine(line, out var record))
                continue;

            if (record.UtcDate < minDate || record.UtcDate > maxDate)
                continue;

            records.Add(record);
        }

        return records.ToArray();
    }

    private static bool TryParseLine(string line, out RoadsterRecord record)
    {
        record = default;
        var parts = line.Split(',');
        if (parts.Length < 9)
        {
            Debug.LogWarning($"Roadster CSV: not enough columns in line: {line}");
            return false;
        }

        try
        {
            // parts[0] epoch JD is currently unused.
            var utc = DateTime.Parse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            record = new RoadsterRecord
            {
                UtcDate = utc,
                SemiMajorAxisAu = double.Parse(parts[2], CultureInfo.InvariantCulture),
                Eccentricity = double.Parse(parts[3], CultureInfo.InvariantCulture),
                InclinationDeg = double.Parse(parts[4], CultureInfo.InvariantCulture),
                LongitudeOfAscendingNodeDeg = double.Parse(parts[5], CultureInfo.InvariantCulture),
                ArgumentOfPeriapsisDeg = double.Parse(parts[6], CultureInfo.InvariantCulture),
                MeanAnomalyDeg = double.Parse(parts[7], CultureInfo.InvariantCulture),
                TrueAnomalyDeg = double.Parse(parts[8], CultureInfo.InvariantCulture)
            };

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Roadster CSV: failed to parse line: {line}. Error: {ex.Message}");
            return false;
        }
    }
}

[Serializable]
public struct RoadsterRecord
{
    public DateTime UtcDate;
    public double SemiMajorAxisAu;
    public double Eccentricity;
    public double InclinationDeg;
    public double LongitudeOfAscendingNodeDeg;
    public double ArgumentOfPeriapsisDeg;
    public double MeanAnomalyDeg;
    public double TrueAnomalyDeg;
}

