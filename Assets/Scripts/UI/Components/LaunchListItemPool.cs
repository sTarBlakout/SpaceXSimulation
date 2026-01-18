using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple pool for <see cref="LaunchListItem"/> instances to avoid Instantiate/Destroy churn.
/// </summary>
public sealed class LaunchListItemPool
{
    private readonly LaunchListItem _prefab;
    private readonly Transform _poolRoot;
    private readonly Stack<LaunchListItem> _inactive = new Stack<LaunchListItem>(64);

    public LaunchListItemPool(LaunchListItem prefab, Transform poolRoot = null, int prewarmCount = 0)
    {
        _prefab = prefab != null ? prefab : throw new System.ArgumentNullException(nameof(prefab));
        _poolRoot = poolRoot;

        Prewarm(prewarmCount);
    }

    public LaunchListItem Get(Transform parent)
    {
        var item = _inactive.Count > 0 ? _inactive.Pop() : Object.Instantiate(_prefab);
        if (item == null)
            return null;

        var tr = item.transform;
        tr.SetParent(parent, false);

        item.gameObject.SetActive(true);
        return item;
    }

    public void Release(LaunchListItem item)
    {
        if (item == null)
            return;

        item.Unbind();
        item.gameObject.SetActive(false);

        if (_poolRoot != null)
            item.transform.SetParent(_poolRoot, false);

        _inactive.Push(item);
    }

    private void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var item = Object.Instantiate(_prefab, _poolRoot);
            item.gameObject.SetActive(false);
            _inactive.Push(item);
        }
    }
}


