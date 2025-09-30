using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Assign (or auto-find)")]
    [SerializeField] private GameObject window;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backFromSettingsButton;

    private PauseManager manager;

    public void Setup(PauseManager mgr)
    {
        manager = mgr;

        // Try to auto-find if not wired
        if (!window) window = transform.Find("Window")?.gameObject;
        if (!settingsPanel) settingsPanel = transform.Find("Window/SettingsPanel")?.gameObject;

        if (!resumeButton) resumeButton = transform.Find("Window/ResumeButton")?.GetComponent<Button>();
        if (!settingsButton) settingsButton = transform.Find("Window/SettingsButton")?.GetComponent<Button>();
        if (!returnToLobbyButton) returnToLobbyButton = transform.Find("Window/ReturnToLobbyButton")?.GetComponent<Button>();
        if (!quitButton) quitButton = transform.Find("Window/QuitButton")?.GetComponent<Button>();
        if (!backFromSettingsButton) backFromSettingsButton = transform.Find("Window/SettingsPanel/BackFromSettingsButton")?.GetComponent<Button>();

        resumeButton?.onClick.AddListener(() => manager.Resume());
        settingsButton?.onClick.AddListener(() => ShowSettings(true));
        returnToLobbyButton?.onClick.AddListener(() => manager.ReturnToLobby(false));
        quitButton?.onClick.AddListener(() => manager.QuitGame());
        backFromSettingsButton?.onClick.AddListener(() => ShowSettings(false));

        ShowSettings(false);
    }

    public void ShowSettings(bool show)
    {
        if (settingsPanel) settingsPanel.SetActive(show);
        if (window) window.SetActive(true); // keep base window visible
    }
}