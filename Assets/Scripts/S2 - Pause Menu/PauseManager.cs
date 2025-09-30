using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Assign in Inspector or via Resources")]
    [SerializeField] private GameObject pauseMenuPrefab; // PauseMenuCanvas prefab
    [SerializeField] private string lobbySceneName = "Lobby"; // change to your lobby scene name

    private GameObject menuInstance;
    private bool isPaused;

    // Input actions created at runtime (so you don't need an input asset setup)
    private InputAction pauseAction;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create a Pause action with multiple bindings (Desktop + Gamepad + VR)
        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");
        // Common XR menu bindings (will be ignored if not present)
        pauseAction.AddBinding("<XRController>{LeftHand}/menuButton");
        pauseAction.AddBinding("<XRController>{RightHand}/menuButton");
        pauseAction.performed += ctx => TogglePause();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnEnable() => pauseAction.Enable();
    void OnDisable() => pauseAction.Disable();

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ensure a menu exists in every scene
        EnsureMenuInstance();
        // Always start unpaused after a load
        if (isPaused) SetPaused(false);
    }

    private void EnsureMenuInstance()
    {
        if (menuInstance != null) return;

        if (pauseMenuPrefab == null)
        {
            // Try Resources if not assigned
            pauseMenuPrefab = Resources.Load<GameObject>("PauseMenuCanvas");
        }

        if (pauseMenuPrefab == null)
        {
            Debug.LogError("PauseManager: Assign pauseMenuPrefab or place one named 'PauseMenuCanvas' in a Resources/ folder.");
            return;
        }

        menuInstance = Instantiate(pauseMenuPrefab);
        menuInstance.SetActive(false);

        // Wire up the buttons
        var ui = menuInstance.GetComponentInChildren<PauseMenuUI>(true);
        if (ui == null) ui = menuInstance.AddComponent<PauseMenuUI>();
        ui.Setup(this);
    }

    public void TogglePause() => SetPaused(!isPaused);

    public void SetPaused(bool paused)
    {
        isPaused = paused;
        if (menuInstance == null) EnsureMenuInstance();

        if (menuInstance != null)
            menuInstance.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;
        // Optional: lock/unlock cursor in Desktop mode
        if (GameSettings.Instance != null && GameSettings.Instance.CurrentMode == GameMode.Desktop)
        {
            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isPaused;
        }
    }

    // Called by UI buttons
    public void Resume() => SetPaused(false);

    public void OpenSettings(bool open)
    {
        var ui = menuInstance.GetComponentInChildren<PauseMenuUI>(true);
        ui?.ShowSettings(open);
    }

    public void ReturnToLobby(bool resetMode = false)
    {
        // Clear run-specific data but keep the saved mode by default
        GameSession.ClearRunData(keepMode: !resetMode);
        SetPaused(false);
        SceneManager.LoadScene(lobbySceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        // Stop play mode in Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}