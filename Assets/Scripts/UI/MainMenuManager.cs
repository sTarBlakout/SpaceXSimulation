using UnityEngine;

/// <summary>
/// Handles main menu buttons
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private SimulatorManager simulatorManager;
    [SerializeField] private LaunchesPanel launchesPanel;
    [SerializeField] private GameObject simulatorUI;

    public void StartSimulation()
    {
        if (simulatorManager != null)
            simulatorManager.StartSimulation();

        if (simulatorUI != null)
            simulatorUI.SetActive(true);

        gameObject.SetActive(false);
    }

    public void OpenLaunches()
    {
        if (launchesPanel != null)
            launchesPanel.Open();

        gameObject.SetActive(false);
    }

    public void BackFromSimulator()
    {
        if (simulatorManager != null)
            simulatorManager.ResetSimulation();

        if (simulatorUI != null)
            simulatorUI.SetActive(false);

        gameObject.SetActive(true);
    }

    public void BackFromLaunches()
    {
        if (launchesPanel != null)
            launchesPanel.Close();

        gameObject.SetActive(true);
    }
}

