using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Lobby UI for navigation: choose guide mode (Roadline / Agent / Verbal) and maze (1–3).
/// START stays disabled until both choices are made. Selection uses scale up + tint + shadow glow.
/// Keyboard Tab focus tints the button image (like a filled highlight); optional outline; selection glow uses Shadow.
/// Fully qualify UI types: project may define a global Outline type (QuickOutline).
/// </summary>
[DefaultExecutionOrder(-200)]
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

    [Header("Keyboard focus (Tab)")]
    [Tooltip("Fills the button image while focused (same idea as a solid purple choice row on other menus).")]
    [SerializeField] Color keyboardFocusBackgroundColor = new Color(1f, 0.92f, 0.35f, 1f);
    [SerializeField] bool tintImageOnKeyboardFocus = true;
    [Tooltip("Optional extra border; can leave off when using a solid fill.")]
    [SerializeField] bool showKeyboardFocusOutline = false;
    [SerializeField] Color keyboardFocusOutlineColor = new Color(0.85f, 0.7f, 0f, 1f);
    [SerializeField] Vector2 keyboardFocusOutlineDistance = new Vector2(2f, -2f);

    static readonly string[] ModeNames = { "Btn_Mode_Roadline", "Btn_Mode_Agent", "Btn_Mode_Verbal" };
    static readonly string[] MazeNames = { "Btn_Maze_1", "Btn_Maze_2", "Btn_Maze_3" };
    const string StartName = "Btn_Start";

    int _modeIndex = -1;
    int _mazeIndex = -1;

    Graphic[] _modeGraphics = new Graphic[3];
    Graphic[] _mazeGraphics = new Graphic[3];
    UnityEngine.UI.Shadow[] _modeShadows = new UnityEngine.UI.Shadow[3];
    UnityEngine.UI.Shadow[] _mazeShadows = new UnityEngine.UI.Shadow[3];
    UnityEngine.UI.Shadow _startShadow;
    UnityEngine.UI.Outline[] _modeFocusOutlines = new UnityEngine.UI.Outline[3];
    UnityEngine.UI.Outline[] _mazeFocusOutlines = new UnityEngine.UI.Outline[3];
    UnityEngine.UI.Outline _startFocusOutline;
    Graphic _startGraphic;
    Color _startBaseImageColor = Color.white;
    Vector3[] _modeBaseScale = new Vector3[3];
    Vector3[] _mazeBaseScale = new Vector3[3];

    readonly List<Selectable> _tabChainScratch = new List<Selectable>(8);
    GameObject _selectionBeforeUiModule;

    void Awake()
    {
        TryBindByChildName();
        CacheGraphicsAndShadows();
        WireKeyboardFocusFeedback();

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

    void OnEnable()
    {
        // Panel can start inactive; rebuild when shown (Start interactable state may have changed).
        ConfigureTabNavigation();
        RefreshFocusHighlights();
    }

    void Update()
    {
        // Snapshot before InputSystemUIInputModule / StandaloneInputModule runs (default order 0),
        // so we know which button had focus before Tab navigation this frame.
        if (EventSystem.current != null)
            _selectionBeforeUiModule = EventSystem.current.currentSelectedGameObject;
    }

    void LateUpdate()
    {
        // Input System UI maps Tab to MoveDirection.Next and ignores explicit Navigation links,
        // so START never joins the cycle when it becomes interactable. Snapshots + applying our
        // chain here fixes Tab / Shift+Tab while leaving START non-focusable until ready.
        if (!WasTabPressedThisFrame())
            return;
        var es = EventSystem.current;
        if (es == null)
            return;

        FillTabChain(_tabChainScratch);
        int n = _tabChainScratch.Count;
        if (n == 0)
            return;

        GameObject fromGo = _selectionBeforeUiModule;
        if (fromGo == null)
            return;

        int idx = -1;
        for (int i = 0; i < n; i++)
        {
            if (_tabChainScratch[i] != null && _tabChainScratch[i].gameObject == fromGo)
            {
                idx = i;
                break;
            }
        }

        if (idx < 0)
            return;

        bool shift = ShiftHeldThisFrame();
        int nextIdx = shift ? (idx - 1 + n) % n : (idx + 1) % n;
        Selectable next = _tabChainScratch[nextIdx];
        if (next != null)
            es.SetSelectedGameObject(next.gameObject);
    }

    static bool WasTabPressedThisFrame()
    {
        Keyboard k = Keyboard.current;
        if (k != null && k.tabKey.wasPressedThisFrame)
            return true;
        return Input.GetKeyDown(KeyCode.Tab);
    }

    static bool ShiftHeldThisFrame()
    {
        Keyboard k = Keyboard.current;
        if (k != null)
            return k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
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

        CacheStartButtonEffects();
    }

    void CacheStartButtonEffects()
    {
        if (startButton == null)
            return;

        UnityEngine.UI.Shadow sh = startButton.GetComponent<UnityEngine.UI.Shadow>();
        if (sh == null)
        {
            sh = startButton.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = glowShadowColor;
            sh.effectDistance = shadowDistance;
            sh.useGraphicAlpha = true;
            sh.enabled = false;
        }

        _startShadow = sh;

        _startFocusOutline = EnsureFocusOutline(startButton.gameObject);

        _startGraphic = startButton.targetGraphic != null
            ? startButton.targetGraphic
            : startButton.GetComponent<Graphic>();
        if (_startGraphic != null)
            _startBaseImageColor = _startGraphic.color;
    }

    void WireKeyboardFocusFeedback()
    {
        for (int i = 0; i < 3; i++)
        {
            WireFocusRelay(this, gameModeButtons[i]);
            WireFocusRelay(this, mazeButtons[i]);
        }

        WireFocusRelay(this, startButton);
    }

    static void WireFocusRelay(TeleportFSelectionController owner, Button btn)
    {
        if (btn == null || owner == null)
            return;

        var relay = btn.GetComponent<TeleportFKeyboardFocusRelay>();
        if (relay == null)
            relay = btn.gameObject.AddComponent<TeleportFKeyboardFocusRelay>();

        relay.Initialize(owner);
    }

    void CacheOne(Button btn, int index, bool modeRow)
    {
        if (btn == null)
            return;

        Graphic g = btn.targetGraphic != null ? btn.targetGraphic : btn.GetComponent<Graphic>();
        UnityEngine.UI.Shadow sh = btn.GetComponent<UnityEngine.UI.Shadow>();
        if (sh == null)
        {
            sh = btn.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = glowShadowColor;
            sh.effectDistance = shadowDistance;
            sh.useGraphicAlpha = true;
            sh.enabled = false;
        }

        UnityEngine.UI.Outline focusOutline = EnsureFocusOutline(btn.gameObject);

        if (modeRow)
        {
            _modeGraphics[index] = g;
            _modeBaseScale[index] = btn.transform.localScale;
            _modeShadows[index] = sh;
            _modeFocusOutlines[index] = focusOutline;
        }
        else
        {
            _mazeGraphics[index] = g;
            _mazeBaseScale[index] = btn.transform.localScale;
            _mazeShadows[index] = sh;
            _mazeFocusOutlines[index] = focusOutline;
        }
    }

    UnityEngine.UI.Outline EnsureFocusOutline(GameObject buttonRoot)
    {
        UnityEngine.UI.Outline ol = buttonRoot.GetComponent<UnityEngine.UI.Outline>();
        if (ol == null)
        {
            ol = buttonRoot.AddComponent<UnityEngine.UI.Outline>();
            ol.effectColor = keyboardFocusOutlineColor;
            ol.effectDistance = keyboardFocusOutlineDistance;
            ol.useGraphicAlpha = true;
            ol.enabled = false;
        }

        return ol;
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

        RefreshFocusHighlights();
    }

    /// <summary>Called when EventSystem keyboard focus moves (Tab). Public for <see cref="TeleportFKeyboardFocusRelay"/>.</summary>
    public void RefreshFocusHighlights()
    {
        GameObject focused = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        for (int i = 0; i < 3; i++)
            ApplyFocusAndGlow(gameModeButtons[i], _modeGraphics[i], _modeShadows[i], _modeFocusOutlines[i], focused,
                i == _modeIndex, normalColor);
        for (int i = 0; i < 3; i++)
            ApplyFocusAndGlow(mazeButtons[i], _mazeGraphics[i], _mazeShadows[i], _mazeFocusOutlines[i], focused,
                i == _mazeIndex, normalColor);

        if (startButton != null)
            ApplyFocusAndGlow(startButton, _startGraphic, _startShadow, _startFocusOutline, focused, chosen: false,
                _startBaseImageColor);
    }

    void ApplyFocusAndGlow(Button btn, Graphic g, UnityEngine.UI.Shadow sh, UnityEngine.UI.Outline focusOutline,
        GameObject focused, bool chosen, Color baseImageColor)
    {
        if (btn == null)
            return;

        bool isFocused = focused == btn.gameObject;

        if (g != null)
        {
            if (tintImageOnKeyboardFocus && isFocused)
                g.color = keyboardFocusBackgroundColor;
            else if (chosen)
                g.color = selectedColor;
            else
                g.color = baseImageColor;
        }

        if (focusOutline != null)
        {
            if (showKeyboardFocusOutline && isFocused)
            {
                focusOutline.effectColor = keyboardFocusOutlineColor;
                focusOutline.effectDistance = keyboardFocusOutlineDistance;
                focusOutline.enabled = true;
            }
            else
                focusOutline.enabled = false;
        }

        if (sh == null)
            return;

        if (isFocused)
        {
            sh.enabled = false;
            return;
        }

        if (chosen)
        {
            sh.effectColor = glowShadowColor;
            sh.enabled = true;
        }
        else
            sh.enabled = false;
    }

    void ApplyHighlight(bool modeRow, int index, bool selected)
    {
        Button b = modeRow ? gameModeButtons[index] : mazeButtons[index];
        Vector3 baseScale = modeRow ? _modeBaseScale[index] : _mazeBaseScale[index];

        if (b != null)
            b.transform.localScale = selected ? baseScale * selectedScale : baseScale;
    }

    void RefreshStartInteractable()
    {
        bool ready = _modeIndex >= 0 && _mazeIndex >= 0;
        if (startButton != null)
            startButton.interactable = ready;

        // Disabled buttons are skipped by the EventSystem; when START becomes interactable we must
        // rebuild explicit navigation so Tab can reach it after Maze 3 (Automatic mode often never updates).
        ConfigureTabNavigation();
    }

    /// <summary>
    /// Tab / Shift+Tab order: game mode row → maze row → START (only when both picks are made).
    /// START is omitted from the cycle while disabled so it cannot be focused early.
    /// </summary>
    void ConfigureTabNavigation()
    {
        FillTabChain(_tabChainScratch);
        int n = _tabChainScratch.Count;
        if (n == 0)
            return;

        for (int i = 0; i < n; i++)
        {
            Selectable prev = _tabChainScratch[(i - 1 + n) % n];
            Selectable next = _tabChainScratch[(i + 1) % n];
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = prev;
            nav.selectOnDown = next;
            nav.selectOnLeft = prev;
            nav.selectOnRight = next;
            _tabChainScratch[i].navigation = nav;
        }
    }

    void FillTabChain(List<Selectable> chain)
    {
        chain.Clear();
        for (int i = 0; i < 3; i++)
        {
            if (gameModeButtons[i] != null)
                chain.Add(gameModeButtons[i]);
        }

        for (int i = 0; i < 3; i++)
        {
            if (mazeButtons[i] != null)
                chain.Add(mazeButtons[i]);
        }

        if (startButton != null && startButton.interactable)
            chain.Add(startButton);
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

/// <summary>Refreshes focus outline when uGUI selection changes (Tab / Shift+Tab).</summary>
sealed class TeleportFKeyboardFocusRelay : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    TeleportFSelectionController _owner;

    public void Initialize(TeleportFSelectionController owner)
    {
        _owner = owner;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (_owner != null)
            _owner.RefreshFocusHighlights();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (_owner != null)
            _owner.RefreshFocusHighlights();
    }
}
