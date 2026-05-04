using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lobby UI for navigation: choose guide mode (Roadline / Agent / Verbal) and maze (1–3).
/// START stays disabled until both choices are made. Selection uses scale up + tint + shadow glow.
/// Uses <see cref="UnityEngine.UI.Shadow"/> (not Outline) to avoid clashing with QuickOutline's global Outline type.
/// </summary>
public class TeleportFSelectionController : MonoBehaviour
{
    [Header("References (optional — auto-found by child name if empty)")]
    public Button[] gameModeButtons = new Button[3];
    public Button[] mazeButtons = new Button[3];
    public Button startButton;
    public LoadSceneButton startLoadScene;

    [Header("Highlight")]
    [SerializeField] float selectedScale = 1.12f;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color selectedColor = new Color(0.45f, 0.85f, 1f, 1f);
    [SerializeField] Color glowShadowColor = new Color(0.25f, 0.7f, 1f, 0.95f);
    [SerializeField] Vector2 shadowDistance = new Vector2(3f, 3f);

    static readonly string[] ModeNames = { "Btn_Mode_Roadline", "Btn_Mode_Agent", "Btn_Mode_Verbal" };
    static readonly string[] MazeNames = { "Btn_Maze_1", "Btn_Maze_2", "Btn_Maze_3" };
    const string StartName = "Btn_Start";

    int _modeIndex = -1;
    int _mazeIndex = -1;

    Graphic[] _modeGraphics = new Graphic[3];
    Graphic[] _mazeGraphics = new Graphic[3];
    UnityEngine.UI.Shadow[] _modeShadows = new UnityEngine.UI.Shadow[3];
    UnityEngine.UI.Shadow[] _mazeShadows = new UnityEngine.UI.Shadow[3];
    Vector3[] _modeBaseScale = new Vector3[3];
    Vector3[] _mazeBaseScale = new Vector3[3];

    void Awake()
    {
        TryBindByChildName();
        CacheGraphicsAndShadows();

        for (int i = 0; i < 3; i++)
        {
            int mi = i;
            if (gameModeButtons[i] != null)
                gameModeButtons[i].onClick.AddListener(() => OnModeClicked(mi));
            if (mazeButtons[i] != null)
                mazeButtons[i].onClick.AddListener(() => OnMazeClicked(mi));
        }

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        RefreshVisuals();
        RefreshStartInteractable();
    }

    void TryBindByChildName()
    {
        if (gameModeButtons == null || gameModeButtons.Length != 3)
            gameModeButtons = new Button[3];
        if (mazeButtons == null || mazeButtons.Length != 3)
            mazeButtons = new Button[3];

        for (int i = 0; i < 3; i++)
        {
            if (gameModeButtons[i] == null)
            {
                var t = FindChildTransformByName(ModeNames[i]);
                if (t != null)
                    gameModeButtons[i] = t.GetComponent<Button>();
            }
            if (mazeButtons[i] == null)
            {
                var t = FindChildTransformByName(MazeNames[i]);
                if (t != null)
                    mazeButtons[i] = t.GetComponent<Button>();
            }
        }

        if (startButton == null)
        {
            var st = FindChildTransformByName(StartName);
            if (st != null)
                startButton = st.GetComponent<Button>();
        }

        if (startLoadScene == null && startButton != null)
            startLoadScene = startButton.GetComponent<LoadSceneButton>();
    }

    void CacheGraphicsAndShadows()
    {
        for (int i = 0; i < 3; i++)
        {
            CacheOne(gameModeButtons[i], i, true);
            CacheOne(mazeButtons[i], i, false);
        }
    }

    void CacheOne(Button btn, int index, bool modeRow)
    {
        if (btn == null)
            return;

        Graphic g = btn.targetGraphic != null ? btn.targetGraphic : btn.GetComponent<Graphic>();
        // Fully qualified: project has global QuickOutline.Outline which breaks unqualified Outline.
        UnityEngine.UI.Shadow sh = btn.GetComponent<UnityEngine.UI.Shadow>();
        if (sh == null)
        {
            sh = btn.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = glowShadowColor;
            sh.effectDistance = shadowDistance;
            sh.useGraphicAlpha = true;
            sh.enabled = false;
        }

        if (modeRow)
        {
            _modeGraphics[index] = g;
            _modeBaseScale[index] = btn.transform.localScale;
            _modeShadows[index] = sh;
        }
        else
        {
            _mazeGraphics[index] = g;
            _mazeBaseScale[index] = btn.transform.localScale;
            _mazeShadows[index] = sh;
        }
    }

    void OnModeClicked(int index)
    {
        _modeIndex = index;
        RefreshVisuals();
        RefreshStartInteractable();
    }

    void OnMazeClicked(int index)
    {
        _mazeIndex = index;
        RefreshVisuals();
        RefreshStartInteractable();
    }

    void RefreshVisuals()
    {
        for (int i = 0; i < 3; i++)
            ApplyHighlight(true, i, i == _modeIndex);
        for (int i = 0; i < 3; i++)
            ApplyHighlight(false, i, i == _mazeIndex);
    }

    void ApplyHighlight(bool modeRow, int index, bool selected)
    {
        Graphic g = modeRow ? _modeGraphics[index] : _mazeGraphics[index];
        Button b = modeRow ? gameModeButtons[index] : mazeButtons[index];
        UnityEngine.UI.Shadow sh = modeRow ? _modeShadows[index] : _mazeShadows[index];
        Vector3 baseScale = modeRow ? _modeBaseScale[index] : _mazeBaseScale[index];

        if (b != null)
            b.transform.localScale = selected ? baseScale * selectedScale : baseScale;

        if (g != null)
            g.color = selected ? selectedColor : normalColor;

        if (sh != null)
            sh.enabled = selected;
    }

    void RefreshStartInteractable()
    {
        bool ready = _modeIndex >= 0 && _mazeIndex >= 0;
        if (startButton != null)
            startButton.interactable = ready;
    }

    void OnStartClicked()
    {
        if (_modeIndex < 0 || _mazeIndex < 0)
            return;

        NavigationLobbyPrefs.Save(_modeIndex, _mazeIndex);

        if (startLoadScene != null)
            startLoadScene.LoadScene();
        else
            Debug.LogWarning("TeleportFSelectionController: Add LoadSceneButton to Btn_Start to load NavigationScene.");
    }

    Transform FindChildTransformByName(string name)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
                return t;
        }
        return null;
    }
}
