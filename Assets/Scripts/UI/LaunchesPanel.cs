using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the launches list panel: fetches data, populates list items, shows loading/error.
/// </summary>
public class LaunchesPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private LaunchListItem itemPrefab;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private ShipPopup shipPopup;

    [Header("Dependencies")]
    [SerializeField] private SpaceXClient client;

    private readonly List<LaunchListItem> _spawned = new List<LaunchListItem>();
    private List<LaunchViewModel> _cache;
    private bool _isLoading;
    private LaunchListItemPool _pool;

    private void Awake()
    {
        if (client == null)
            client = new SpaceXClient();

        if (itemPrefab == null)
        {
            Debug.LogError("LaunchesPanel: itemPrefab not assigned.");
            enabled = false;
            return;
        }

        _pool = new LaunchListItemPool(itemPrefab);

        SetLoading(false, null);
        SetError(null);
    }

    public async void Open()
    {
        gameObject.SetActive(true);

        if (_cache != null)
        {
            Populate(_cache);
            return;
        }

        await LoadAndPopulate();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private async Task LoadAndPopulate()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        SetLoading(true, "Loading launches...");
        SetError(null);

        try
        {
            var launches = await client.FetchLaunchesAsync();
            if (launches == null || launches.Count == 0)
            {
            SetError("No launches found.");
                ClearList();
                return;
            }

            var vmList = new List<LaunchViewModel>(launches.Count);
            var rocketCache = new Dictionary<string, RocketDto>();
            var now = DateTime.UtcNow;

            foreach (var l in launches)
            {
                RocketDto rocket = null;
                if (!string.IsNullOrEmpty(l.rocket))
                {
                    if (!rocketCache.TryGetValue(l.rocket, out rocket))
                    {
                        rocket = await client.FetchRocketAsync(l.rocket);
                        if (rocket != null)
                            rocketCache[l.rocket] = rocket;
                    }
                }

                var launchDateUtc = ParseDateUtc(l.date_utc);
                vmList.Add(new LaunchViewModel
                {
                    LaunchId = l.id,
                    MissionName = l.name ?? "Unknown",
                    PayloadCount = l.payloads != null ? l.payloads.Count : 0,
                    RocketName = rocket?.name ?? "Unknown",
                    RocketCountry = rocket?.country ?? "Unknown",
                    IsPast = launchDateUtc <= now,
                    LaunchDateUtc = launchDateUtc,
                    ShipIds = l.ships
                });
            }

            _cache = vmList;
            Populate(vmList);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"LaunchesPanel: load failed: {ex.Message}");
            SetError("Failed to load launches.");
            ClearList();
        }
        finally
        {
            SetLoading(false, null);
            _isLoading = false;
        }
    }

    private void Populate(List<LaunchViewModel> list)
    {
        ClearList();
        foreach (var vm in list)
        {
            var item = _pool != null ? _pool.Get(contentRoot) : Instantiate(itemPrefab, contentRoot);
            _spawned.Add(item);
            item.Bind(vm, () => OnItemClicked(vm));
        }
    }

    private void ClearList()
    {
        // Important: Release in reverse to keep stable visual order when using a Stack-based pool (LIFO).
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            var item = _spawned[i];
            if (item == null)
                continue;

            if (_pool != null)
                _pool.Release(item);
            else
                Destroy(item.gameObject);
        }
        _spawned.Clear();
    }

    private void SetLoading(bool loading, string message)
    {
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(loading);
            loadingText.text = loading ? message ?? "Loading..." : string.Empty;
        }
    }

    private void SetError(string message)
    {
        if (errorText != null)
        {
            errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            errorText.text = message ?? string.Empty;
        }
    }

    private async void OnItemClicked(LaunchViewModel vm)
    {
        await ShowShipPopup(vm);
    }

    private async Task ShowShipPopup(LaunchViewModel vm)
    {
        if (shipPopup == null)
        {
            Debug.LogWarning("LaunchesPanel: ShipPopup not assigned.");
            return;
        }

        if (vm.ShipIds == null || vm.ShipIds.Count == 0)
        {
            shipPopup.Show(new List<ShipViewModel>());
            return;
        }

        var ships = new List<ShipViewModel>(vm.ShipIds.Count);
        foreach (var shipId in vm.ShipIds)
        {
            var dto = await client.FetchShipAsync(shipId);
            if (dto == null)
                continue;

            ships.Add(new ShipViewModel
            {
                Name = dto.name,
                Type = dto.type,
                HomePort = dto.home_port,
                MissionsCount = dto.launches != null ? dto.launches.Count : 0,
                ImageUrl = dto.image
            });
        }

        shipPopup.Show(ships);
    }

    private static DateTime ParseDateUtc(string dateUtc)
    {
        if (string.IsNullOrEmpty(dateUtc))
            return DateTime.MinValue;

        if (DateTime.TryParse(dateUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
            return dt.ToUniversalTime();

        return DateTime.MinValue;
    }
}

