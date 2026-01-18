using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShipPopup : MonoBehaviour
{
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private ShipPopupItem itemPrefab;
    [SerializeField] private TMP_Text emptyText;

    private readonly List<ShipPopupItem> _spawned = new List<ShipPopupItem>();

    public void Show(List<ShipViewModel> ships)
    {
        Clear();

        if (ships == null || ships.Count == 0)
        {
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(true);
                emptyText.text = "No ships for this launch.";
            }
        }
        else
        {
            if (emptyText != null)
                emptyText.gameObject.SetActive(false);

            foreach (var ship in ships)
            {
                var item = Instantiate(itemPrefab, contentRoot);
                item.Bind(ship);
                _spawned.Add(item);
            }
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        Clear();
        gameObject.SetActive(false);
    }

    public void OnCloseButton()
    {
        Hide();
    }

    private void Clear()
    {
        foreach (var item in _spawned)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _spawned.Clear();

        if (emptyText != null)
            emptyText.gameObject.SetActive(false);
    }
}

