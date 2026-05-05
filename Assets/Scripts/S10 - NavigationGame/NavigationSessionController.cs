using System.Collections;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;

/// <summary>
/// Runs after RoomScene lobby: loading UI, activates chosen maze + guide mode, enables path manager,
/// then starts round 1 (guided). After goal: wait, round 2 same maze without guide.
/// Add this to NavigationScene; assign maze roots, guide mode roots, UI, and path manager.
/// </summary>
[DefaultExecutionOrder(-500)]
public class NavigationSessionController : MonoBehaviour
{
    public static NavigationSessionController Instance { get; private set; }

    /// <summary>When true, <see cref="NavigationPathManager"/> skips auto goal lookup in Start (session drives setup).</summary>
    public static bool SessionOwnsPathSetup { get; private set; }

    [Header("Maze roots (e.g. Maze1, Maze2, Maze3 — deactivate all others)")]
    public GameObject[] mazeRoots = new GameObject[3];

    [Header("Guide modes matching lobby: 0 Roadline, 1 Agent, 2 Verbal")]
    public GameObject[] guideModeRoots = new GameObject[3];

    [Header("Core")]
    public NavigationPathManager pathManager;
    public NavigationGameManager gameManager;
    public Transform playerTeleportPoint;

    [Header("Loading / feedback UI")]
    public GameObject loadingCanvasRoot;
    public TMP_Text loadingMessageText;
    public GameObject feedbackCanvasRoot;
    public TMP_Text feedbackMessageText;

    [Header("Timing")]
    public float roundCompleteMessageSeconds = 5f;
    public float betweenRoundsDelaySeconds = 0.5f;

    [Header("Optional: block locomotion while loading (XR / desktop roots)")]
    public GameObject[] disableWhileLoading = new GameObject[0];

    [Header("Goal trigger (round 1–2 reset)")]
    [Tooltip("Optional. Leave empty to use the first GoalTrigger under the active maze (recommended when each maze has its own exit). If set, this instance is used for all mazes (e.g. one shared test volume).")]
    public GoalTrigger goalTrigger;

    static readonly string LoadingMessage = "Settings are loading, wait!";
    static readonly string Round1DoneMessage = "You reached the destination.\nNext round starts soon…";
    static readonly string BetweenRoundsLoading = "Loading next round…";
    static readonly string Round2DoneMessage = "You reached the destination.\nSession complete.";

    int _lobbyGuideMode;
    int _lobbyMazeIndex;
    int _currentRound = 1;
    bool _sessionComplete;
    bool _waitingForGoal;

    void Awake()
    {
        Instance = this;
        SessionOwnsPathSetup = true;

        if (pathManager != null)
            pathManager.enabled = false;

        if (gameManager != null)
            gameManager.enabled = false;

        if (loadingCanvasRoot != null)
            loadingCanvasRoot.SetActive(false);
        if (feedbackCanvasRoot != null)
            feedbackCanvasRoot.SetActive(false);
    }

    void Start()
    {
        StartCoroutine(RunSession());
    }

    IEnumerator RunSession()
    {
        if (!NavigationLobbyPrefs.TryGet(out _lobbyGuideMode, out _lobbyMazeIndex))
        {
            Debug.LogWarning("NavigationSessionController: No lobby prefs from RoomScene — using maze 0, mode 0.");
            _lobbyGuideMode = 0;
            _lobbyMazeIndex = 0;
        }

        _lobbyGuideMode = Mathf.Clamp(_lobbyGuideMode, 0, guideModeRoots.Length > 0 ? guideModeRoots.Length - 1 : 0);
        _lobbyMazeIndex = Mathf.Clamp(_lobbyMazeIndex, 0, mazeRoots.Length > 0 ? mazeRoots.Length - 1 : 0);

        NavigationParticipantLog.LogSessionStart(_lobbyGuideMode, _lobbyMazeIndex);

        yield return InitialLoadAndRound1();
    }

    IEnumerator InitialLoadAndRound1()
    {
        SetLoadingVisible(true, LoadingMessage);
        SetBlocking(true);

        DeactivateAllMazes();
        DeactivateAllGuideModes();

        var maze = mazeRoots[_lobbyMazeIndex];
        if (maze != null)
            maze.SetActive(true);

        yield return null;
        yield return WaitForMazeGenerationIfPresent();

        if (pathManager != null)
        {
            pathManager.limitSearchToSubtree = maze != null ? maze.transform : null;
            pathManager.assignGoalFromMazeExitTag = true;
            pathManager.goalPoint = null;
            pathManager.TryAssignGoalFromMazeExit();
            pathManager.enabled = true;
            if (!pathManager.gameObject.activeSelf)
                pathManager.gameObject.SetActive(true);
        }

        if (GetGoalTriggerForActiveMaze() == null)
            Debug.LogWarning(
                "NavigationSessionController: No GoalTrigger for this maze. " +
                "Add a Box/Sphere trigger + GoalTrigger on the exit inside each maze (same object as MazeExit is fine), " +
                "or assign the optional 'goalTrigger' field for a shared volume.");

        ActivateGuideForLobbySelection();

        yield return null;

        SetLoadingVisible(false, null);
        SetBlocking(false);

        if (gameManager != null)
        {
            gameManager.enabled = true;
            gameManager.BeginRoundTimer();
        }

        _waitingForGoal = true;
    }

    IEnumerator WaitForMazeGenerationIfPresent()
    {
        if (MazeManager.Instance == null)
            yield break;

        var loader = MazeManager.Instance.loader;
        if (loader?.maze?.mazeGenerator == null)
            yield break;

        float t = 0f;
        while (loader.maze.mazeGenerator != null && !loader.maze.mazeGenerator.Finish && t < 60f)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    void DeactivateAllMazes()
    {
        foreach (var m in mazeRoots)
            if (m != null)
                m.SetActive(false);
    }

    void DeactivateAllGuideModes()
    {
        foreach (var g in guideModeRoots)
            if (g != null)
                g.SetActive(false);
    }

    void ActivateGuideForLobbySelection()
    {
        if (_lobbyGuideMode < 0 || _lobbyGuideMode >= guideModeRoots.Length)
            return;
        var go = guideModeRoots[_lobbyGuideMode];
        if (go != null)
            go.SetActive(true);
    }

    /// <summary>Called from <see cref="GoalTrigger"/> instead of going straight to <see cref="NavigationGameManager"/>.</summary>
    public void NotifyGoalReached()
    {
        if (!_waitingForGoal || _sessionComplete)
            return;

        _waitingForGoal = false;
        StartCoroutine(OnGoalHandled());
    }

    IEnumerator OnGoalHandled()
    {
        if (_currentRound == 1)
        {
            if (gameManager != null)
                gameManager.NotifyRoundGoalReached(1);

            SetFeedbackVisible(true, Round1DoneMessage);
            yield return new WaitForSecondsRealtime(roundCompleteMessageSeconds);
            SetFeedbackVisible(false, null);

            yield return StartRound2Sequence();
        }
        else if (_currentRound == 2)
        {
            if (gameManager != null)
                gameManager.NotifyRoundGoalReached(2);

            SetFeedbackVisible(true, Round2DoneMessage);
            _sessionComplete = true;
            yield return new WaitForSecondsRealtime(roundCompleteMessageSeconds);
            SetFeedbackVisible(false, null);

            if (gameManager != null)
                gameManager.EndNavigationSession();
        }
    }

    IEnumerator StartRound2Sequence()
    {
        SetLoadingVisible(true, BetweenRoundsLoading);
        SetBlocking(true);

        DeactivateAllGuideModes();

        // Move player out of the goal before toggling the trigger; avoids instant overlap bugs
        // and ensures round 2 starts from the maze entry.
        TeleportPlayerTo(playerTeleportPoint);

        yield return null;

        var gt = GetGoalTriggerForActiveMaze();
        if (gt != null)
            gt.ResetTriggerForAnotherPass();
        else
            Debug.LogWarning("NavigationSessionController: No GoalTrigger for round 2 — add one under the active maze (or assign 'goalTrigger').");

        yield return new WaitForSecondsRealtime(betweenRoundsDelaySeconds);

        SetLoadingVisible(false, null);
        SetBlocking(false);

        _currentRound = 2;
        if (gameManager != null)
            gameManager.BeginRoundTimer();

        _waitingForGoal = true;

        if (gt != null)
            gt.RefreshOverlapWithPlayer();
    }

    const string MazeExitTag = "MazeExit";

    /// <summary>
    /// Manual <see cref="goalTrigger"/> overrides. Otherwise finds the <see cref="GoalTrigger"/> under
    /// whichever <see cref="mazeRoots"/> entry is <b>actually active</b> (not only the lobby index).
    /// Prefers the component on the object tagged <see cref="MazeExitTag"/> when you have several triggers.
    /// </summary>
    GoalTrigger GetGoalTriggerForActiveMaze()
    {
        if (goalTrigger != null)
            return goalTrigger;

        var maze = FindActiveMazeRoot();
        if (maze == null)
            return null;

        return FindGoalTriggerUnderMaze(maze);
    }

    /// <summary>Uses hierarchy truth: the enabled maze among <see cref="mazeRoots"/>.</summary>
    GameObject FindActiveMazeRoot()
    {
        GameObject found = null;
        foreach (var m in mazeRoots)
        {
            if (m == null || !m.activeInHierarchy)
                continue;
            if (found != null)
                Debug.LogWarning(
                    "NavigationSessionController: Multiple maze roots are active; using '" + found.name + "' for GoalTrigger. Deactivate other mazes.");
            else
                found = m;
        }

        if (found != null)
            return found;

        if (_lobbyMazeIndex >= 0 && _lobbyMazeIndex < mazeRoots.Length && mazeRoots[_lobbyMazeIndex] != null)
            return mazeRoots[_lobbyMazeIndex];

        return null;
    }

    static GoalTrigger FindGoalTriggerUnderMaze(GameObject mazeRoot)
    {
        var all = mazeRoot.GetComponentsInChildren<GoalTrigger>(true);
        if (all == null || all.Length == 0)
            return null;

        foreach (var gt in all)
        {
            if (gt != null && gt.CompareTag(MazeExitTag))
                return gt;
        }

        return all[0];
    }

    /// <summary>Moves the tagged Player rig; supports XROrigin and CharacterController teleport quirks.</summary>
    void TeleportPlayerTo(Transform destination)
    {
        if (destination == null)
        {
            Debug.LogWarning("NavigationSessionController: Assign playerTeleportPoint (e.g. StartPoint) so round 2 resets outside the goal.");
            return;
        }

        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged == null)
        {
            Debug.LogWarning("NavigationSessionController: No GameObject with tag 'Player' — cannot teleport for round 2.");
            return;
        }

        var origin = tagged.GetComponentInParent<XROrigin>();
        if (origin != null)
        {
            // MoveCameraToWorldLocation expects the desired *camera* world position. Using the floor
            // spawn point alone drives the HMD to ground level (round 2 VR bug).
            var rigTransform = origin.transform;
            var xrCc = rigTransform.GetComponent<CharacterController>()
                       ?? rigTransform.GetComponentInChildren<CharacterController>();

            bool hadCc = xrCc != null && xrCc.enabled;
            if (hadCc)
                xrCc.enabled = false;

            try
            {
                var cam = origin.Camera;
                if (cam != null)
                {
                    Vector3 cameraWorldOffset = cam.transform.position - rigTransform.position;
                    Vector3 targetCameraWorldPos = destination.position + cameraWorldOffset;
                    origin.MoveCameraToWorldLocation(targetCameraWorldPos);

                    // Face maze entry (yaw only); rotate around camera so eye height is unchanged.
                    Vector3 fromFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
                    Vector3 destFwd = Vector3.ProjectOnPlane(destination.forward, Vector3.up);
                    if (fromFwd.sqrMagnitude > 1e-6f && destFwd.sqrMagnitude > 1e-6f)
                    {
                        fromFwd.Normalize();
                        destFwd.Normalize();
                        float yawAngle = Vector3.SignedAngle(fromFwd, destFwd, Vector3.up);
                        if (Mathf.Abs(yawAngle) > 0.01f)
                            rigTransform.RotateAround(cam.transform.position, Vector3.up, yawAngle);
                    }
                }
                else
                {
                    rigTransform.SetPositionAndRotation(destination.position, destination.rotation);
                }
            }
            finally
            {
                if (hadCc && xrCc != null)
                    xrCc.enabled = true;
            }

            return;
        }

        var playerTransform = tagged.transform.root;
        var cc = playerTransform.GetComponent<CharacterController>()
                 ?? playerTransform.GetComponentInChildren<CharacterController>();
        var targetPos = destination.position;
        if (cc != null)
        {
            cc.enabled = false;
            playerTransform.position = targetPos;
            cc.enabled = true;
        }
        else
            playerTransform.position = targetPos;
    }

    void SetLoadingVisible(bool vis, string msg)
    {
        if (loadingCanvasRoot != null)
            loadingCanvasRoot.SetActive(vis);
        if (loadingMessageText != null && msg != null)
            loadingMessageText.text = msg;
    }

    void SetFeedbackVisible(bool vis, string msg)
    {
        if (feedbackCanvasRoot != null)
            feedbackCanvasRoot.SetActive(vis);
        if (feedbackMessageText != null && msg != null)
            feedbackMessageText.text = msg;
    }

    void SetBlocking(bool block)
    {
        foreach (var go in disableWhileLoading)
            if (go != null)
                go.SetActive(!block);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            SessionOwnsPathSetup = false;
        }
    }
}
