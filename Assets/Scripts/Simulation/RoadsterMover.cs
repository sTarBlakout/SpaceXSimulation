using System;
using UnityEngine;
using RG.OrbitalElements;

/// <summary>
/// Moves the Roadster transform using preloaded orbital records.
/// Expects initialization via Initialize(...); does not load CSV itself.
/// </summary>
public class RoadsterMover : MonoBehaviour
{
    private const double TailSampleIntervalDays = 1d;
    private const int MaxTailSamplesPerAdvance = 256;

    [SerializeField] private LineRenderer tailRenderer;

    private bool _autoPlay = true;
    private float _daysPerSecond = 1f;
    private float _kmPerUnit = 10000f;
    private int _tailLength = 20;
    private bool _enableInterpolation;
    private bool _enableInterpolatedTail;
    private double _interpolationGapDays = 1d;
    private RoadsterPositionCalculationMode _positionCalculationMode = RoadsterPositionCalculationMode.OrbitalElementsDll;

    private RoadsterRecord[] _records;
    private Vector3[] _positions;

    private int _index;
    private int _nextIndex;
    private double _segmentDurationDays;
    private double _segmentElapsedDays;

    private bool _ready;
    private Vector3[] _tailBuffer;
    private int _tailCount;
    private double _nextTailSampleElapsedDays;

    private DateTime _simulatedUtc;

    private void Update()
    {
        if (!_ready || !_autoPlay)
            return;

        StepSimulation(Time.deltaTime);
    }

    public void Initialize(
        RoadsterRecord[] records,
        float kmPerUnit,
        float daysPerSecond,
        bool autoPlay,
        int tailLength,
        bool enableInterpolatedTail,
        bool enableInterpolation,
        float interpolationGapDays,
        RoadsterPositionCalculationMode positionCalculationMode)
    {
        if (records == null || records.Length == 0)
        {
            Debug.LogError("RoadsterMover.Initialize: records are null or empty.");
            _ready = false;
            return;
        }

        if (kmPerUnit <= 0f)
        {
            kmPerUnit = 10000f;
            Debug.LogWarning("RoadsterMover.Initialize: kmPerUnit was <= 0, reset to 10000.");
        }

        _records = records;
        _kmPerUnit = kmPerUnit;
        _daysPerSecond = daysPerSecond;
        _autoPlay = autoPlay;
        _tailLength = Mathf.Max(1, tailLength);
        _enableInterpolatedTail = enableInterpolatedTail;
        _enableInterpolation = enableInterpolation;
        _interpolationGapDays = Math.Max(0d, (double)interpolationGapDays);
        _positionCalculationMode = positionCalculationMode;

        _index = 0;
        _segmentElapsedDays = 0d;
        _ready = true;

        PrepareTailBuffer();

        PrecomputePositions();
        ResetToIndex(0, resetTail: true);
    }

    public void StepSimulation(float deltaTime)
    {
        if (!_ready)
            return;

        var simDays = (double)(deltaTime * _daysPerSecond);
        if (simDays <= 0d)
            return;

        AdvanceBySimulatedDays(simDays);
    }

    private void AdvanceBySimulatedDays(double days)
    {
        if (_records == null || _records.Length < 2)
            return;

        var remaining = days;
        var lastPos = transform.position;
        while (remaining > 0d)
        {
            // If we reached the end, loop back to start (do NOT interpolate last -> first).
            if (_index >= _records.Length - 1)
            {
                ResetToIndex(0, resetTail: true);
                // Continue consuming remaining days from the start of the dataset.
            }

            if (_segmentDurationDays <= 0d)
            {
                // Bad/duplicate timestamps; skip forward.
                JumpToNextRecord();
                continue;
            }

            var step = Math.Min(remaining, _segmentDurationDays - _segmentElapsedDays);
            _segmentElapsedDays += step;
            remaining -= step;

            // Keep simulated time continuous even if we "jump" position for <= 24h gaps.
            _simulatedUtc = _records[_index].UtcDate + TimeSpan.FromDays(_segmentElapsedDays);

            var shouldInterpolate = _enableInterpolation && _segmentDurationDays > _interpolationGapDays;
            if (shouldInterpolate)
            {
                var t = (float)(_segmentElapsedDays / _segmentDurationDays);
                var fromPos = _positions[_index];
                var toPos = _positions[_nextIndex];

                transform.position = CalculateInterpolatedOrbitalPosition(
                    _records[_index],
                    _records[_nextIndex],
                    fromPos,
                    toPos,
                    t);

                if (_enableInterpolatedTail)
                {
                    AppendInterpolatedTailSamples(
                        _records[_index],
                        _records[_nextIndex],
                        fromPos,
                        toPos);
                }
            }

            // Segment done -> advance to next record (with an optional jump).
            if (_segmentElapsedDays >= _segmentDurationDays - 1e-9)
            {
                JumpToNextRecord();
            }

            lastPos = transform.position;
        }

        // Make the tail "head" follow the current Roadster position smoothly even between tail point appends.
        UpdateTailHead(lastPos);
    }

    public RoadsterRecord CurrentRecord => (_records != null && _records.Length > 0) ? _records[_index] : default;
    public int CurrentIndex => _index;
    public int RecordCount => _records?.Length ?? 0;
    public DateTime CurrentSimulatedUtc => _simulatedUtc;

    private void PrepareTailBuffer()
    {
        if (_tailLength < 1)
            _tailLength = 1;

        if (tailRenderer != null && (tailRenderer.positionCount != _tailLength))
        {
            tailRenderer.positionCount = 0;
        }

        _tailBuffer = new Vector3[_tailLength];
        _tailCount = 0;
    }

    private void AppendTail(Vector3 pos)
    {
        if (tailRenderer == null)
            return;

        if (_tailBuffer == null || _tailBuffer.Length != _tailLength)
            PrepareTailBuffer();

        if (_tailCount < _tailLength)
        {
            _tailBuffer[_tailCount] = pos;
            _tailCount++;
        }
        else
        {
            // shift left, drop oldest
            Array.Copy(_tailBuffer, 1, _tailBuffer, 0, _tailLength - 1);
            _tailBuffer[_tailLength - 1] = pos;
        }

        tailRenderer.positionCount = _tailCount;
        for (int i = 0; i < _tailCount; i++)
        {
            tailRenderer.SetPosition(i, _tailBuffer[i]);
        }
    }

    private void UpdateTailHead(Vector3 pos)
    {
        if (tailRenderer == null || _tailCount <= 0)
            return;

        var lastIndex = _tailCount - 1;
        if (_tailBuffer != null && lastIndex < _tailBuffer.Length)
            _tailBuffer[lastIndex] = pos;

        if (tailRenderer.positionCount == _tailCount)
            tailRenderer.SetPosition(lastIndex, pos);
    }

    private void PrecomputePositions()
    {
        if (_records == null || _records.Length == 0)
        {
            _positions = null;
            return;
        }

        _positions = new Vector3[_records.Length];

        for (int i = 0; i < _records.Length; i++)
        {
            var record = _records[i];

            Vector3 posThousandKm;
            if (_positionCalculationMode == RoadsterPositionCalculationMode.CustomPhysical)
            {
                posThousandKm = OrbitalMath.CalculatePositionThousandKmPhysical(
                    record.SemiMajorAxisAu,
                    record.Eccentricity,
                    record.InclinationDeg,
                    record.LongitudeOfAscendingNodeDeg,
                    record.ArgumentOfPeriapsisDeg,
                    record.TrueAnomalyDeg);
            }
            else
            {
                var pos = Calculations.CalculateOrbitalPosition(
                    record.SemiMajorAxisAu,
                    record.Eccentricity,
                    record.InclinationDeg,
                    record.LongitudeOfAscendingNodeDeg,
                    record.ArgumentOfPeriapsisDeg,
                    record.TrueAnomalyDeg);

                // OrbitalElements.dll returns vector in 10^3 km units.
                posThousandKm = new Vector3((float)pos.x, (float)pos.y, (float)pos.z);
            }

            // Convert thousand-km -> km -> Unity units
            var kmToUnits = 1000f / _kmPerUnit;
            _positions[i] = posThousandKm * kmToUnits;
        }
    }

    private void ResetToIndex(int index, bool resetTail)
    {
        _index = Mathf.Clamp(index, 0, (_records?.Length ?? 1) - 1);
        _segmentElapsedDays = 0d;
        _simulatedUtc = _records[_index].UtcDate;

        if (resetTail)
        {
            PrepareTailBuffer();
            if (tailRenderer != null)
                tailRenderer.positionCount = 0;
        }

        transform.position = _positions != null && _positions.Length > 0 ? _positions[_index] : transform.position;
        AppendTail(transform.position);

        SetupNextSegment();
    }

    private void JumpToNextRecord()
    {
        // Advance index.
        _index++;
        if (_index >= _records.Length)
        {
            ResetToIndex(0, resetTail: true);
            return;
        }

        // If not interpolating this segment, we jump to the next record's position here.
        // If we were interpolating, we're already at (or very close to) the end position, but snap to exact anyway.
        if (_positions != null && _index < _positions.Length &&
            (!_enableInterpolation || _segmentDurationDays <= _interpolationGapDays))
            transform.position = _positions[_index];
        else if (_positions != null && _index < _positions.Length)
            transform.position = _positions[_index];

        AppendTail(transform.position);
        _segmentElapsedDays = 0d;
        _simulatedUtc = _records[_index].UtcDate;

        SetupNextSegment();
    }

    private void SetupNextSegment()
    {
        _nextIndex = _index + 1;
        if (_records == null || _nextIndex >= _records.Length)
        {
            _segmentDurationDays = 0d;
            return;
        }

        _segmentDurationDays = (_records[_nextIndex].UtcDate - _records[_index].UtcDate).TotalDays;
        if (_segmentDurationDays < 0d)
            _segmentDurationDays = 0d;

        _nextTailSampleElapsedDays = TailSampleIntervalDays;
    }

    private Vector3 CalculateInterpolatedOrbitalPosition(
        RoadsterRecord from,
        RoadsterRecord to,
        Vector3 fromPos,
        Vector3 toPos,
        float t01)
    {
        t01 = Mathf.Clamp01(t01);

        // Some early rows have discontinuous elements -> lerp positions there.
        if (!AreElementsStableForInterpolation(from, to))
            return Vector3.Lerp(fromPos, toPos, t01);

        // Interpolate elements (angles are wrap-aware).
        var semiMajorAxisAu = Mathf.Lerp((float)from.SemiMajorAxisAu, (float)to.SemiMajorAxisAu, t01);
        var eccentricity = Mathf.Lerp((float)from.Eccentricity, (float)to.Eccentricity, t01);

        var inclinationDeg = OrbitalMath.LerpAngleShortestDeg((float)from.InclinationDeg, (float)to.InclinationDeg, t01);
        var longitudeOfAscendingNodeDeg = OrbitalMath.LerpAngleShortestDeg((float)from.LongitudeOfAscendingNodeDeg, (float)to.LongitudeOfAscendingNodeDeg, t01);
        var argumentOfPeriapsisDeg = OrbitalMath.LerpAngleShortestDeg((float)from.ArgumentOfPeriapsisDeg, (float)to.ArgumentOfPeriapsisDeg, t01);

        // Interpolate mean anomaly forward, then convert M -> ν (Kepler).
        var meanAnomalyDeg = OrbitalMath.LerpAngleForwardDeg((float)from.MeanAnomalyDeg, (float)to.MeanAnomalyDeg, t01);
        var trueAnomalyDeg = (float)OrbitalMath.MeanToTrueAnomalyDeg(meanAnomalyDeg, eccentricity);

        Vector3 posThousandKm;
        if (_positionCalculationMode == RoadsterPositionCalculationMode.CustomPhysical)
        {
            posThousandKm = OrbitalMath.CalculatePositionThousandKmPhysical(
                semiMajorAxisAu,
                eccentricity,
                inclinationDeg,
                longitudeOfAscendingNodeDeg,
                argumentOfPeriapsisDeg,
                trueAnomalyDeg);
        }
        else
        {
            var pos = Calculations.CalculateOrbitalPosition(
                semiMajorAxisAu,
                eccentricity,
                inclinationDeg,
                longitudeOfAscendingNodeDeg,
                argumentOfPeriapsisDeg,
                trueAnomalyDeg);

            // OrbitalElements.dll returns vector in 10^3 km units.
            posThousandKm = new Vector3((float)pos.x, (float)pos.y, (float)pos.z);
        }

        // Convert thousand-km -> km -> Unity units
        var kmToUnits = 1000f / _kmPerUnit;
        return posThousandKm * kmToUnits;
    }

    private void AppendInterpolatedTailSamples(
        RoadsterRecord from,
        RoadsterRecord to,
        Vector3 fromPos,
        Vector3 toPos)
    {
        if (tailRenderer == null)
            return;

        // Add extra tail points inside long segments so the line doesn't stretch.
        var added = 0;
        while (_nextTailSampleElapsedDays < _segmentElapsedDays &&
               _nextTailSampleElapsedDays < _segmentDurationDays - 1e-9)
        {
            var t = (float)(_nextTailSampleElapsedDays / _segmentDurationDays);
            var p = CalculateInterpolatedOrbitalPosition(from, to, fromPos, toPos, t);
            AppendTail(p);

            _nextTailSampleElapsedDays += TailSampleIntervalDays;

            if (++added >= MaxTailSamplesPerAdvance)
                break;
        }
    }

    private static bool AreElementsStableForInterpolation(RoadsterRecord from, RoadsterRecord to)
    {
        // Conservative thresholds to avoid interpolating across discontinuities.
        var a0 = (float)from.SemiMajorAxisAu;
        var a1 = (float)to.SemiMajorAxisAu;
        var aRel = (Mathf.Abs(a1 - a0) / Mathf.Max(1e-6f, Mathf.Abs(a0)));
        if (aRel > 0.25f)
            return false;

        if (Mathf.Abs((float)to.Eccentricity - (float)from.Eccentricity) > 0.2f)
            return false;

        if (Mathf.Abs(Mathf.DeltaAngle((float)from.InclinationDeg, (float)to.InclinationDeg)) > 10f)
            return false;

        if (Mathf.Abs(Mathf.DeltaAngle((float)from.LongitudeOfAscendingNodeDeg, (float)to.LongitudeOfAscendingNodeDeg)) > 30f)
            return false;

        if (Mathf.Abs(Mathf.DeltaAngle((float)from.ArgumentOfPeriapsisDeg, (float)to.ArgumentOfPeriapsisDeg)) > 30f)
            return false;

        return true;
    }

}

