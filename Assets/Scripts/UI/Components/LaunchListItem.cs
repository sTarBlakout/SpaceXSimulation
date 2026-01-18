using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LaunchListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text missionNameText;
    [SerializeField] private TMP_Text payloadCountText;
    [SerializeField] private TMP_Text rocketNameText;
    [SerializeField] private TMP_Text rocketCountryText;
    [SerializeField] private Toggle statusToggle;
    [SerializeField] private Button button;

    private Action _onClick;

    public void Bind(LaunchViewModel vm, Action onClick)
    {
        missionNameText.text = vm.MissionName;
        payloadCountText.text = vm.PayloadCount.ToString();
        rocketNameText.text = vm.RocketName;
        rocketCountryText.text = vm.RocketCountry;

        if (statusToggle != null)
        {
            statusToggle.isOn = vm.IsPast;
            statusToggle.interactable = false; // display-only
        }

        _onClick = onClick;
    }

    public void Unbind()
    {
        _onClick = null;
    }

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        _onClick?.Invoke();
    }
}

