using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity's JsonUtility cannot parse top-level arrays directly; this helper wraps them.
/// </summary>
public static class JsonUtilityWrapper
{
    [Serializable]
    private class Wrapper<T>
    {
        public List<T> items;
    }

    public static List<T> FromJsonArray<T>(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        var wrapped = "{\"items\":" + json + "}";
        try
        {
            var w = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return w?.items;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"JsonUtilityWrapper parse failed: {ex.Message}");
            return null;
        }
    }
}
