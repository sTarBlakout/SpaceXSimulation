using System;
using UnityEngine;

/// <summary>
/// Custom orbital math for converting orbital elements to a Cartesian position vector.
/// Returns position in 10^3 km ("thousand km") units, aligned to Unity axes (Y-up).
/// </summary>
public static class OrbitalMath
{
    // IAU 2012 definition (km)
    private const double AstronomicalUnitKm = 149_597_870.7;
    private const double TwoPi = Math.PI * 2.0;

    /// <summary>
    /// Computes position in 10^3 km units from classical orbital elements using true anomaly (PHYSICALLY CORRECT).
    /// Radius convention:
    /// r = a(1-e^2)/(1+e cos(nu))
    /// Inputs:
    /// - semiMajorAxisAu: AU
    /// - eccentricity: unitless
    /// - angles: degrees
    /// </summary>
    public static Vector3 CalculatePositionThousandKmPhysical(
        double semiMajorAxisAu,
        double eccentricity,
        double inclinationDeg,
        double longitudeOfAscendingNodeDeg,
        double argumentOfPeriapsisDeg,
        double trueAnomalyDeg)
    {
        // Radius in km: r = a(1-e^2)/(1+e cos nu)
        var aKm = semiMajorAxisAu * AstronomicalUnitKm;
        var e = eccentricity;

        var nu = Deg2Rad(trueAnomalyDeg);
        var i = Deg2Rad(inclinationDeg);
        var omega = Deg2Rad(argumentOfPeriapsisDeg);
        var bigOmega = Deg2Rad(longitudeOfAscendingNodeDeg);

        var denom = 1.0 + e * Math.Cos(nu);
        if (Math.Abs(denom) < 1e-12)
            denom = 1e-12;

        var rKm = (aKm * (1.0 - e * e)) / denom;

        // Argument of latitude: u = ω + ν
        var u = omega + nu;

        // Precompute sines/cosines
        var cosO = Math.Cos(bigOmega);
        var sinO = Math.Sin(bigOmega);
        var cosI = Math.Cos(i);
        var sinI = Math.Sin(i);
        var cosU = Math.Cos(u);
        var sinU = Math.Sin(u);

        // Direct PQW->IJK position formula (km)
        var xKm = rKm * (cosO * cosU - sinO * sinU * cosI);
        var yKm = rKm * (sinO * cosU + cosO * sinU * cosI);
        var zKm = rKm * (sinU * sinI);

        // Convert to 10^3 km units (thousand km) to match OrbitalElements.dll convention.
        var inertialThousandKm = new Vector3((float)(xKm / 1000.0), (float)(yKm / 1000.0), (float)(zKm / 1000.0));

        // Inertial (right-handed) XYZ -> Unity (X, Z, -Y)
        return new Vector3(inertialThousandKm.x, inertialThousandKm.z, -inertialThousandKm.y);
    }

    private static double Deg2Rad(double deg) => deg * (Math.PI / 180.0);
    private static double Rad2Deg(double rad) => rad * (180.0 / Math.PI);

    /// <summary>
    /// Mean anomaly (deg) -> true anomaly (deg), elliptical orbits (0 <= e < 1).
    /// </summary>
    public static double MeanToTrueAnomalyDeg(double meanAnomalyDeg, double eccentricity)
    {
        var e = Math.Clamp(eccentricity, 0.0, 0.999999999);
        var m = Deg2Rad(meanAnomalyDeg);
        m = WrapRadians0To2Pi(m);

        // Kepler solve (Newton-Raphson).
        var E = (e < 0.8) ? m : Math.PI;
        for (int iter = 0; iter < 12; iter++)
        {
            var f = E - e * Math.Sin(E) - m;
            var fp = 1.0 - e * Math.Cos(E);
            if (Math.Abs(fp) < 1e-12)
                break;

            var dE = f / fp;
            E -= dE;

            if (Math.Abs(dE) < 1e-12)
                break;
        }

        // True anomaly from eccentric anomaly.
        var sinHalfE = Math.Sin(E * 0.5);
        var cosHalfE = Math.Cos(E * 0.5);
        var sqrt1pe = Math.Sqrt(1.0 + e);
        var sqrt1me = Math.Sqrt(1.0 - e);

        var nu = 2.0 * Math.Atan2(sqrt1pe * sinHalfE, sqrt1me * cosHalfE);
        nu = WrapRadians0To2Pi(nu);

        return Rad2Deg(nu);
    }

    private static double WrapRadians0To2Pi(double rad)
    {
        rad %= TwoPi;
        if (rad < 0.0)
            rad += TwoPi;
        return rad;
    }

    /// <summary>Shortest-arc angle lerp (deg).</summary>
    public static float LerpAngleShortestDeg(float fromDeg, float toDeg, float t01)
    {
        return fromDeg + Mathf.DeltaAngle(fromDeg, toDeg) * Mathf.Clamp01(t01);
    }

    /// <summary>Forward angle lerp (deg).</summary>
    public static float LerpAngleForwardDeg(float fromDeg, float toDeg, float t01)
    {
        t01 = Mathf.Clamp01(t01);
        var delta = RepeatDeg(toDeg - fromDeg);
        return fromDeg + delta * t01;
    }

    /// <summary>Wrap degrees to [0, 360).</summary>
    public static float RepeatDeg(float deg)
    {
        return deg - 360f * Mathf.Floor(deg / 360f);
    }
}
