using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShipPopupItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text homePortText;
    [SerializeField] private TMP_Text missionsText;
    [SerializeField] private Button photoButton;

    private string _imageUrl;

    private void Awake()
    {
        if (photoButton == null)
            photoButton = GetComponent<Button>();

        // If the prefab has no button, add one to the root so the feature works without manual setup.
        if (photoButton == null)
        {
            photoButton = gameObject.AddComponent<Button>();
            photoButton.transition = Selectable.Transition.ColorTint;
            photoButton.targetGraphic = GetComponent<Image>();
        }
    }

    public void Bind(ShipViewModel vm)
    {
        nameText.text = vm.Name ?? "Unknown";
        typeText.text = vm.Type ?? "Unknown";
        homePortText.text = vm.HomePort ?? "Unknown";
        missionsText.text = vm.MissionsCount.ToString();

        _imageUrl = vm.ImageUrl;
        if (photoButton != null)
        {
            photoButton.onClick.RemoveListener(OnPhotoClicked);
            var hasUrl = !string.IsNullOrWhiteSpace(_imageUrl);
            photoButton.interactable = hasUrl;
            if (hasUrl)
                photoButton.onClick.AddListener(OnPhotoClicked);
        }
    }

    private void OnDestroy()
    {
        if (photoButton != null)
            photoButton.onClick.RemoveListener(OnPhotoClicked);
    }

    private void OnPhotoClicked()
    {
        if (string.IsNullOrWhiteSpace(_imageUrl))
            return;

        Application.OpenURL(_imageUrl);
    }
}

