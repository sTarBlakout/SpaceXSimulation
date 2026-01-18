using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Minimal SpaceX API client for launches and rockets (v4).
/// </summary>
public class SpaceXClient
{
    private const string BaseUrl = "https://api.spacexdata.com/v4";
    private readonly Dictionary<string, RocketDto> _rocketCache = new Dictionary<string, RocketDto>();
    private readonly Dictionary<string, ShipDto> _shipCache = new Dictionary<string, ShipDto>();

    /// <summary>
    /// Fetches all launches.
    /// </summary>
    public async Task<List<LaunchDto>> FetchLaunchesAsync()
    {
        var url = $"{BaseUrl}/launches";
        var json = await GetAsync(url);
        if (string.IsNullOrEmpty(json))
            return new List<LaunchDto>();

        return JsonUtilityWrapper.FromJsonArray<LaunchDto>(json) ?? new List<LaunchDto>();
    }

    /// <summary>
    /// Fetches rocket details with simple in-memory cache.
    /// </summary>
    public async Task<RocketDto> FetchRocketAsync(string rocketId)
    {
        if (string.IsNullOrEmpty(rocketId))
            return null;

        if (_rocketCache.TryGetValue(rocketId, out var cached))
            return cached;

        var url = $"{BaseUrl}/rockets/{rocketId}";
        var json = await GetAsync(url);
        if (string.IsNullOrEmpty(json))
            return null;

        var rocket = JsonUtility.FromJson<RocketDto>(json);
        if (rocket != null)
            _rocketCache[rocketId] = rocket;

        return rocket;
    }

    /// <summary>
    /// Fetches ship details with simple in-memory cache.
    /// </summary>
    public async Task<ShipDto> FetchShipAsync(string shipId)
    {
        if (string.IsNullOrEmpty(shipId))
            return null;

        if (_shipCache.TryGetValue(shipId, out var cached))
            return cached;

        var url = $"{BaseUrl}/ships/{shipId}";
        var json = await GetAsync(url);
        if (string.IsNullOrEmpty(json))
            return null;

        var ship = JsonUtility.FromJson<ShipDto>(json);
        if (ship != null)
            _shipCache[shipId] = ship;

        return ship;
    }

    private static async Task<string> GetAsync(string url)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            var op = req.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"SpaceXClient GET failed: {url} | {req.error}");
                return null;
            }

            return req.downloadHandler.text;
        }
    }
}
