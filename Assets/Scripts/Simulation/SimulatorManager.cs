using System;
using UnityEngine;

/// <summary>
/// Parent controller that loads configuration, parses CSV, and initializes the Roadster.
/// </summary>
public class SimulatorManager : MonoBehaviour
{
    [SerializeField] private SimulationConfig config;
    [SerializeField] private RoadsterMover roadsterMover;
    [SerializeField] private RoadsterHud hud;
    [SerializeField] private TouchOrbitCamera orbitCamera;

    private RoadsterRecord[] _records;
    private bool _started;

    public void ResetSimulation()
    {
        _started = false;
        if (hud != null)
            hud.gameObject.SetActive(false);

        if (orbitCamera != null)
            orbitCamera.ResetToInitial();

        if (roadsterMover != null)
            roadsterMover.gameObject.SetActive(false);

        if (roadsterMover != null && _records != null && _records.Length > 0)
        {
            roadsterMover.Initialize(
                _records,
                config.kmPerUnit,
                config.daysPerSecond,
                false,
                config.tailLength,
                config.enableInterpolatedTail,
                config.enableInterpolation,
                config.interpolationGapDays,
                config.positionCalculationMode);
        }
    }

    private void Awake()
    {
        if (hud != null)
            hud.gameObject.SetActive(false);

        if (roadsterMover != null)
            roadsterMover.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (config == null)
        {
            Debug.LogError("SimulatorManager: config not assigned.");
            enabled = false;
            return;
        }

        if (roadsterMover == null)
        {
            Debug.LogError("SimulatorManager: RoadsterMover not assigned.");
            enabled = false;
            return;
        }

        var start = ParseDate(config.startDateUtcIso);
        var end = ParseDate(config.endDateUtcIso);

        _records = RoadsterCsvLoader.LoadRecords(config.roadsterCsv, start, end);
        if (_records == null || _records.Length == 0)
        {
            Debug.LogError("SimulatorManager: no records loaded from CSV.");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (!_started)
            return;

        if (hud == null || roadsterMover == null || roadsterMover.RecordCount == 0)
            return;

        var hudUtc = (config != null && config.showInterpolatedHudTime)
            ? roadsterMover.CurrentSimulatedUtc
            : roadsterMover.CurrentRecord.UtcDate;

        hud.SetData(roadsterMover.CurrentRecord, hudUtc, roadsterMover.CurrentIndex + 1, roadsterMover.RecordCount);
    }

    public void StartSimulation()
    {
        if (_started)
            return;

        if (_records == null || _records.Length == 0)
        {
            Debug.LogError("SimulatorManager: cannot start simulation, records not loaded.");
            return;
        }

        if (roadsterMover != null)
            roadsterMover.gameObject.SetActive(true);

        roadsterMover.Initialize(
            _records,
            config.kmPerUnit,
            config.daysPerSecond,
            true,
            config.tailLength,
            config.enableInterpolatedTail,
            config.enableInterpolation,
            config.interpolationGapDays,
            config.positionCalculationMode);
        _started = true;

        if (hud != null)
            hud.gameObject.SetActive(true);
    }

    private static DateTime? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParse(value, null,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var dt))
        {
            return dt.ToUniversalTime();
        }

        Debug.LogWarning($"SimulatorManager: failed to parse date '{value}'. Expected ISO-like format, e.g., 2018-02-07T03:00:00Z");
        return null;
    }
}

