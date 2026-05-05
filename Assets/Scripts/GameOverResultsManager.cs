using System;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverResultsManager : MonoBehaviour
{
    const string NavigationPanelName = "Navigation";
    const string NavigationStatsObjectName = "NavigationRoundResults";

    [Header("UI Configuration")]
    [Tooltip("TextMesh component donde se mostrarán los resultados")]
    public TextMeshProUGUI resultText;

    [Header("NavigationScene")]
    [Tooltip("Optional TMP inside the Navigation Game Over panel. If empty, a suitable label under 'Navigation' is found or created at runtime so text is not hidden behind the blue overlay.")]
    public TextMeshProUGUI navigationResultText;
    
    void Start()
    {
        // Si no se asigna un TextMesh, intentar encontrarlo en el mismo GameObject
        if (resultText == null)
        {
            resultText = GetComponent<TextMeshProUGUI>();
        }
        
        if (resultText == null)
        {
            Debug.LogError("GameOverResultsManager: No se encontró un TextMeshProUGUI. Asigna uno en el inspector o coloca este script en un GameObject con TextMeshProUGUI.");
            return;
        }
        
        DisplayResults();
    }
    
    void DisplayResults()
    {
        if (SceneTracker.Instance == null)
        {
            resultText.text = "Error: No se pudo obtener información de la escena anterior";
            return;
        }
        
        string previousScene = SceneTracker.Instance.PreviousScene;
        string results = "";
        
        switch (previousScene)
        {
            case "ShooterScene":
                results = $"Score: {SceneTracker.Instance.shooterScore}";
                break;
                
            case "ObstacleCourseScene":
                int minutes = Mathf.FloorToInt(SceneTracker.Instance.obstacleCourseTime / 60F);
                int seconds = Mathf.FloorToInt(SceneTracker.Instance.obstacleCourseTime % 60F);
                results = $"Time: {minutes:00}:{seconds:00}      Success rate: {SceneTracker.Instance.obstacleCourseSuccessRate:F1}%";
                break;
                
            case "PuzzleScene":
                int puzzleMinutes = Mathf.FloorToInt(SceneTracker.Instance.puzzleTime / 60F);
                int puzzleSeconds = Mathf.FloorToInt(SceneTracker.Instance.puzzleTime % 60F);
                results = $"Time: {puzzleMinutes:00}:{puzzleSeconds:00}        Accuracy: {SceneTracker.Instance.puzzleAccuracy:F1}%";
                break;

            case "NavigationScene":
                float nav1 = SceneTracker.Instance.navigationRound1Seconds;
                float nav2 = SceneTracker.Instance.navigationRound2Seconds;
                // UI: only times (congratulations title stays on the localized TMP; mode/maze go to ParticipantSession log only).
                results =
                    $"Guided round: {FormatNavigationDuration(nav1)}\n" +
                    $"Unguided round: {FormatNavigationDuration(nav2)}";
                ApplyNavigationResults(results);
                Debug.Log($"GameOverResultsManager: Mostrando resultados para {previousScene} - {results}");
                return;

            default:
                results = $"Escena no reconocida: {previousScene}";
                break;
        }

        resultText.text = results;
        Debug.Log($"GameOverResultsManager: Mostrando resultados para {previousScene} - {results}");
    }

    /// <summary>
    /// Score_text lives on BaseCanva; the Navigation panel is drawn on top and hid the stats.
    /// Writes to a TMP under <see cref="NavigationPanelName"/> (above the background image).
    /// </summary>
    void ApplyNavigationResults(string results)
    {
        // Any TMP named Score_text (including duplicates / wrong inspector refs) — hide so nothing leaks over the Navigation panel.
        HideEveryLegacyScoreTextInScene();

        // Only write to the dedicated stats label — never the first random TMP under Navigation (old builds left "Mode:/Maze" there).
        TextMeshProUGUI target = navigationResultText;
        var navGo = GameObject.Find(NavigationPanelName);
        if (navGo != null)
        {
            ExpandNavigationPanelIfClipped(navGo);
            ClearStaleNavigationLabels(navGo.transform);
        }

        if (target == null && navGo != null)
        {
            var navRoot = navGo.transform;
            RemoveDuplicateNavigationStatsObjects(navRoot);
            var existing = navRoot.Find(NavigationStatsObjectName);
            if (existing != null)
                target = existing.GetComponent<TextMeshProUGUI>();
        }

        if (target == null && navGo != null)
            target = CreateRuntimeNavigationLabel(navGo.transform);

        if (target == null)
        {
            resultText.text = results;
            Debug.LogWarning(
                "GameOverResultsManager: No Navigation results label found. Assign 'navigationResultText' or add a child TextMeshPro under the GameObject named 'Navigation'.");
            return;
        }

        // Legacy Score_text (resultText) on BaseCanva — clear/hide if not the target we write to.
        if (resultText != null && !ReferenceEquals(resultText, target))
        {
            resultText.text = string.Empty;
            resultText.gameObject.SetActive(false);
        }

        target.text = results;
        ApplyNavigationStatsVisuals(target);
        target.gameObject.SetActive(true);
        target.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Single readable block: full white, consistent size, anchored from bottom so it is not clipped by a small Navigation rect.
    /// </summary>
    void ApplyNavigationStatsVisuals(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;

        if (resultText != null)
        {
            if (resultText.font != null)
                tmp.font = resultText.font;
            if (resultText.fontMaterial != null)
                tmp.fontMaterial = resultText.fontMaterial;
        }

        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableWordWrapping = true;
        tmp.lineSpacing = 2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontWeight = FontWeight.Medium;
        tmp.color = new Color(1f, 1f, 1f, 1f);
        tmp.faceColor = new Color32(255, 255, 255, 255);
        float fs = resultText != null ? Mathf.Clamp(resultText.fontSize * 2.25f, 6f, 8f) : 18f;
        tmp.fontSize = fs;

        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 640f), Mathf.Max(rt.sizeDelta.y, 92f));
        rt.anchoredPosition = new Vector2(0f, 78f);
    }

    static void ExpandNavigationPanelIfClipped(GameObject navigationPanel)
    {
        var rt = navigationPanel.GetComponent<RectTransform>();
        if (rt == null)
            return;
        if (rt.rect.height >= 180f)
            return;
        var sd = rt.sizeDelta;
        rt.sizeDelta = new Vector2(Mathf.Max(sd.x, 480f), Mathf.Max(sd.y, 260f));
    }

    static void RemoveDuplicateNavigationStatsObjects(Transform navigationRoot)
    {
        Transform first = null;
        for (int i = 0; i < navigationRoot.childCount; i++)
        {
            var c = navigationRoot.GetChild(i);
            if (c.name != NavigationStatsObjectName)
                continue;
            if (first == null)
            {
                first = c;
                continue;
            }

            UnityEngine.Object.Destroy(c.gameObject);
        }
    }

    /// <summary>Older builds wrote Mode/Maze or Score on arbitrary TMPs; disable those orphans.</summary>
    static void ClearStaleNavigationLabels(Transform navigationRoot)
    {
        foreach (var tmp in navigationRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.gameObject.name == NavigationStatsObjectName)
                continue;
            if (tmp.GetComponentInParent<Button>() != null)
                continue;
            if (tmp.GetComponent<LocalizeStringEvent>() != null
                || tmp.GetComponentInParent<LocalizeStringEvent>() != null)
                continue;

            string t = tmp.text ?? "";
            bool staleMode = t.IndexOf("Mode:", StringComparison.OrdinalIgnoreCase) >= 0;
            bool staleScore = LooksLikeLegacyScoreLabel(tmp.gameObject.name, t);
            if (!staleMode && !staleScore)
                continue;

            tmp.text = string.Empty;
            tmp.gameObject.SetActive(false);
        }
    }

    static bool LooksLikeLegacyScoreLabel(string objectName, string text)
    {
        if (!string.IsNullOrEmpty(objectName) && objectName.IndexOf("score", StringComparison.OrdinalIgnoreCase) >= 0
            && objectName.IndexOf(NavigationStatsObjectName, StringComparison.Ordinal) < 0)
            return true;

        string trimmed = text.Trim();
        if (trimmed.Length == 0)
            return false;
        if (trimmed.StartsWith("Score", StringComparison.OrdinalIgnoreCase) && trimmed.Length < 16)
            return true;
        return string.Equals(trimmed, "score:", StringComparison.OrdinalIgnoreCase)
               || string.Equals(trimmed, "score", StringComparison.OrdinalIgnoreCase);
    }

    static TextMeshProUGUI CreateRuntimeNavigationLabel(Transform navigationRoot)
    {
        var go = new GameObject(NavigationStatsObjectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(navigationRoot, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        go.transform.SetAsLastSibling();
        return tmp;
    }

    static void HideEveryLegacyScoreTextInScene()
    {
        TextMeshProUGUI[] texts;
#if UNITY_6000_0_OR_NEWER
        texts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        texts = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>(true);
#endif
        foreach (var tmp in texts)
        {
            if (tmp == null)
                continue;
            if (tmp.gameObject.scene != SceneManager.GetActiveScene())
                continue;

            if (tmp.gameObject.name == NavigationStatsObjectName)
                continue;

            string n = tmp.gameObject.name;
            if (n.IndexOf("score", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                tmp.text = string.Empty;
                tmp.gameObject.SetActive(false);
                continue;
            }

            string t = tmp.text;
            if (!string.IsNullOrEmpty(t) && LooksLikeLegacyScoreLabel(n, t))
            {
                tmp.text = string.Empty;
                tmp.gameObject.SetActive(false);
            }
        }

        var baseCanva = GameObject.Find("BaseCanva");
        if (baseCanva != null && baseCanva.scene == SceneManager.GetActiveScene())
            baseCanva.SetActive(false);
    }

    /// <summary>mm:ss.mmm (milliseconds always three digits).</summary>
    static string FormatNavigationDuration(float seconds)
    {
        if (seconds < 0f)
            seconds = 0f;

        int totalMs = Mathf.RoundToInt(seconds * 1000f);
        int ms = totalMs % 1000;
        int totalSec = totalMs / 1000;
        int s = totalSec % 60;
        int m = totalSec / 60;

        return $"{m:00}:{s:00}.{ms:000}";
    }
} 