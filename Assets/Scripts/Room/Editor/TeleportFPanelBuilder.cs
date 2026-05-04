#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// One-time (or refresh) build of <c>UI_TeleportF</c> under <c>UI_Scenes</c> in the project prefab.
/// Run: <b>Tools / Room / Build UI_TeleportF Into UI_Scenes Prefab</b>
/// Then assign the new <c>UI_TeleportF</c> reference to <see cref="ShowTeleportUI.uiTeleportF"/> on teleport pads.
/// </summary>
public static class TeleportFPanelBuilder
{
    const string PrefabPath = "Assets/Prefabs/S1 - Rigs/Canvas/UI_Scenes.prefab";

    [MenuItem("Tools/Room/Build UI_TeleportF Into UI_Scenes Prefab")]
    static void BuildMenu() => ExecuteBuild();

    /// <summary>Optional batchmode: <c>-executeMethod TeleportFPanelBuilder.RunBuildFromBatch</c></summary>
    public static void RunBuildFromBatch() => ExecuteBuild();

    static void ExecuteBuild()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            // Prefab root IS UI_Scenes (Transform.Find only searches children, not self).
            Transform uiScenes = root.name == "UI_Scenes"
                ? root.transform
                : root.transform.Find("UI_Scenes");
            if (uiScenes == null)
            {
                Debug.LogError(
                    "TeleportFPanelBuilder: Could not find UI_Scenes. Prefab root must be named UI_Scenes or contain a child UI_Scenes.");
                return;
            }

            Transform existing = uiScenes.Find("UI_TeleportF");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject panel = BuildPanel(uiScenes);
            panel.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("TeleportFPanelBuilder: UI_TeleportF created under UI_Scenes. Pad F can use ShowTeleportUI auto-resolve or assign uiTeleportF.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.Refresh();
    }

    static GameObject BuildPanel(Transform parent)
    {
        TMP_FontAsset font = LoadDefaultTmpFont();

        GameObject canvasGo = new GameObject("UI_TeleportF");
        canvasGo.layer = 5;
        canvasGo.transform.SetParent(parent, false);

        RectTransform canvasRt = canvasGo.AddComponent<RectTransform>();
        canvasRt.localRotation = Quaternion.identity;
        canvasRt.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        canvasRt.anchorMin = canvasRt.anchorMax = new Vector2(0.5f, 0.5f);
        canvasRt.anchoredPosition = new Vector2(0f, 1.301f);
        canvasRt.sizeDelta = new Vector2(440f, 260f);
        canvasRt.localPosition = new Vector3(0f, 0f, 1.405f);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();
        canvasGo.AddComponent<CanvasAlwaysWorldSpace>();

        // Background
        {
            var bg = new GameObject("Background");
            bg.layer = 5;
            bg.transform.SetParent(canvasGo.transform, false);
            var rt = bg.AddComponent<RectTransform>();
            StretchFull(rt);
            var img = bg.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.15f, 0.92f);
            img.raycastTarget = true;
        }

        // Main column
        var main = new GameObject("Main");
        main.layer = 5;
        main.transform.SetParent(canvasGo.transform, false);
        var mainRt = main.AddComponent<RectTransform>();
        StretchFull(mainRt);
        var mainV = main.AddComponent<VerticalLayoutGroup>();
        mainV.padding = new RectOffset(10, 10, 8, 10);
        mainV.spacing = 6f;
        mainV.childAlignment = TextAnchor.UpperCenter;
        mainV.childControlHeight = true;
        mainV.childControlWidth = true;
        mainV.childForceExpandWidth = true;
        mainV.childForceExpandHeight = false;

        var title = CreateTmp("Title", "Navigation", main.transform, font, 20);
        var titleLe = title.gameObject.AddComponent<LayoutElement>();
        titleLe.preferredHeight = 28f;

        // Two columns
        var row = new GameObject("Row");
        row.layer = 5;
        row.transform.SetParent(main.transform, false);
        var rowRt = row.AddComponent<RectTransform>();
        var rowH = row.AddComponent<HorizontalLayoutGroup>();
        rowH.spacing = 10f;
        rowH.childAlignment = TextAnchor.UpperCenter;
        rowH.childControlWidth = true;
        rowH.childControlHeight = true;
        rowH.childForceExpandWidth = true;
        rowH.childForceExpandHeight = true;
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 150f;
        rowLe.flexibleWidth = 1f;

        Button[] modeButtons = new Button[3];
        Button[] mazeButtons = new Button[3];

        BuildColumn(row.transform, "Panel_GameMode", "Game Mode", new[]
        {
            ("Btn_Mode_Roadline", "○ Roadline"),
            ("Btn_Mode_Agent", "○ Agent"),
            ("Btn_Mode_Verbal", "○ Verbal")
        }, font, modeButtons, 0);

        BuildColumn(row.transform, "Panel_Maze", "Maze Select", new[]
        {
            ("Btn_Maze_1", "□ Maze 1"),
            ("Btn_Maze_2", "□ Maze 2"),
            ("Btn_Maze_3", "□ Maze 3")
        }, font, mazeButtons, 0);

        // Start
        var startGo = CreateButton("Btn_Start", "START", main.transform, font, new Color(0.2f, 0.55f, 0.9f, 1f));
        var startLe = startGo.AddComponent<LayoutElement>();
        startLe.preferredHeight = 36f;
        var startBtn = startGo.GetComponent<Button>();
        startBtn.interactable = false;
        var load = startGo.AddComponent<LoadSceneButton>();
        load.sceneName = "NavigationScene";
        load.objectToActivateInScene = "";
        load.controllerToActivateInScene = "";

        var controller = canvasGo.AddComponent<TeleportFSelectionController>();
        var so = new SerializedObject(controller);
        for (int i = 0; i < 3; i++)
        {
            so.FindProperty("gameModeButtons").GetArrayElementAtIndex(i).objectReferenceValue = modeButtons[i];
            so.FindProperty("mazeButtons").GetArrayElementAtIndex(i).objectReferenceValue = mazeButtons[i];
        }
        so.FindProperty("startButton").objectReferenceValue = startBtn;
        so.FindProperty("startLoadScene").objectReferenceValue = load;
        so.ApplyModifiedProperties();

        return canvasGo;
    }

    static void BuildColumn(Transform parent, string panelName, string header, (string goName, string label)[] items,
        TMP_FontAsset font, Button[] outButtons, int outStart)
    {
        var panel = new GameObject(panelName);
        panel.layer = 5;
        panel.transform.SetParent(parent, false);
        var pRt = panel.AddComponent<RectTransform>();
        var pV = panel.AddComponent<VerticalLayoutGroup>();
        pV.spacing = 4f;
        pV.childAlignment = TextAnchor.UpperCenter;
        pV.childControlHeight = true;
        pV.childControlWidth = true;
        pV.childForceExpandWidth = true;
        pV.padding = new RectOffset(4, 4, 4, 4);
        var pLe = panel.AddComponent<LayoutElement>();
        pLe.flexibleWidth = 1f;

        var border = panel.AddComponent<Image>();
        border.color = new Color(0.25f, 0.25f, 0.3f, 0.5f);
        border.raycastTarget = false;

        var head = CreateTmp("Header", header, panel.transform, font, 15);
        var hLe = head.gameObject.AddComponent<LayoutElement>();
        hLe.preferredHeight = 22f;

        for (int i = 0; i < items.Length; i++)
        {
            var b = CreateButton(items[i].goName, items[i].label, panel.transform, font, new Color(0.95f, 0.95f, 0.95f, 1f));
            outButtons[outStart + i] = b.GetComponent<Button>();
            var bLe = b.AddComponent<LayoutElement>();
            bLe.preferredHeight = 28f;
        }
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    static TextMeshProUGUI CreateTmp(string name, string text, Transform parent, TMP_FontAsset font, float size)
    {
        var go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 24f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        tmp.raycastTarget = false;
        return tmp;
    }

    static GameObject CreateButton(string name, string label, Transform parent, TMP_FontAsset font, Color imageColor)
    {
        var go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 28f);

        var img = go.AddComponent<Image>();
        img.color = imageColor;
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.layer = 5;
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        StretchFull(trt);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 14f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        tmp.raycastTarget = false;

        return go;
    }

    static TMP_FontAsset LoadDefaultTmpFont()
    {
        string[] guids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }
}
#endif
