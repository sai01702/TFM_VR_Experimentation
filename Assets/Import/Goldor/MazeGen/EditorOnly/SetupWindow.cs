using System;
using System.Collections.Generic;
using System.Linq;
using MazeGen;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;

public class SetupWindow : EditorWindow
{
    private enum Page
    {
        Start,
        BasicConfig,
        MazeGenerationConfig,
        MazeSettingsConfig,
        MazeInstantiatorConfig,
        End
    }
    
    private struct CreationConfig
    {
        public string PresetFolder;
        public string MazePresetName;
        public Maze Maze;
        public MazeInstantiator MazeInstantiator;
    }

    private Dictionary<Page, Action<VisualElement>> pageCreationReferences;

    private Page page;
    private List<Action> singleUnloadEvent = new();
    
    private CreationConfig creationConfig;
    
    private Button nextButton;
    private Button backButton;
    private Label currentPageIndicator;


    [MenuItem("Tools/MazeGen/Setup")]
    public static void Init()
    {
        SetupWindow win = GetWindow<SetupWindow>();
        win.Show();
    }

    public void InitPageReferences()
    {
        pageCreationReferences = new Dictionary<Page, Action<VisualElement>>()
        {
            { Page.Start, StartPage },
            { Page.BasicConfig, BasicMazeConfiguration },
            { Page.MazeGenerationConfig, MazeGenerationConfiguration },
            { Page.MazeSettingsConfig, MazeSettingsConfiguration },
            { Page.MazeInstantiatorConfig, MazeInstantiatorConfiguration},
            { Page.End, EndPage}
        };
            
        page = Page.Start;
    }
    
    private void CreateGUI()
    {
        InitPageReferences();
        var splitView = new TwoPaneSplitView(1, 60, TwoPaneSplitViewOrientation.Vertical);
        rootVisualElement.Add(splitView);
        var contentHolder = new VisualElement();
        splitView.Add(contentHolder);
        var bottomPage = new VisualElement();
        splitView.Add(bottomPage);

        bottomPage.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
        bottomPage.style.justifyContent = new StyleEnum<Justify>(Justify.SpaceBetween);
        
        backButton = new Button(() => PreviousPage(contentHolder))
        {
            text = "Back",
            style =
            {
                alignSelf = new StyleEnum<Align>(Align.Center),
                width = new StyleLength(Length.Percent(16)),
                height = new StyleLength(Length.Percent(60)),
                marginBottom = new StyleLength(10),
                marginTop = new StyleLength(10),
                marginLeft = new StyleLength(20)
            }
        };
        
        currentPageIndicator = new Label($"Page {page + 1} : {(int) page + 1}/{pageCreationReferences.Count}")
        {
            style =
            {
                alignSelf = new StyleEnum<Align>(Align.Center),
                unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter),
                height = new StyleLength(Length.Percent(60)),
                marginBottom = new StyleLength(10),
                marginTop = new StyleLength(10),
                flexGrow = new StyleFloat(2),
            }
        };
        
        nextButton = new Button(() => NextPage(contentHolder))
        {
            text = "Next",
            style =
            {
                alignSelf = new StyleEnum<Align>(Align.Center),
                width = new StyleLength(Length.Percent(16)),
                height = new StyleLength(Length.Percent(60)),
                marginBottom = new StyleLength(10),
                marginTop = new StyleLength(10),
                marginRight = new StyleLength(20)
            }
        };

        bottomPage.Add(backButton);
        bottomPage.Add(currentPageIndicator);
        bottomPage.Add(nextButton);
        ReloadPage(contentHolder);
    }

    private void StartPage(VisualElement contentHolder)
    {
        var body = new VisualElement();
        body.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
        contentHolder.Add(body);

        var title = new Label("Welcome to MazeGen setup guide")
        {
            style =
            {
                alignSelf = new StyleEnum<Align>(Align.Center),
                marginTop = new StyleLength(35),
                marginBottom = new StyleLength(20),
                fontSize = new StyleLength(24)
            }
        };
        var stepOneText = StepText("Choose the Maze settings name");
        var settingsName = new TextField()
        {
            label = "Settings name",
            labelElement =
            {
                style =
                {
                    minWidth = new StyleLength(0f)
                }
            },
            value = "MazePreset",
            tooltip = "name of the settings that wille be created",
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };

        var stepTwoText = StepText("Choose a folder to save presets (relative to 'Assets' folder)");

        var browseFoldersButton = new Button()
        {
            text = "browse folders"
        };
        var settingsFolder = new TextField()
        {
            label = "Settings folder",
            labelElement =
            {
                style =
                {
                    minWidth = new StyleLength(0f)
                }
            },
            value = "/MazeGen/Presets",
            tooltip = "place to save preset you will create",
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };
        
        browseFoldersButton.clicked += () =>
        {
            string path = EditorUtility.OpenFolderPanel("Choose preset folder", "Assets", "");
            if (!path.StartsWith(Application.dataPath))
            {
                Debug.LogWarning("Please select folder inside project data folder");
                path = "";
            }
            else
            {
                path = path.Remove(0, Application.dataPath.Length);
            }
            
            settingsFolder.value = path;
        };
        settingsFolder.Add(browseFoldersButton);
        
        singleUnloadEvent.Add(() =>
        {
            if(settingsFolder.value == "")
                settingsFolder.value = "/MazeGen/Presets";
            creationConfig.PresetFolder = $"Assets{settingsFolder.value}".Replace("//", "/");
            creationConfig.MazePresetName = settingsName.value;
            if (creationConfig.MazePresetName == "")
            {
                creationConfig.MazePresetName = "MazePreset";
            }

            creationConfig.Maze = CreateInstance<Maze>();
            creationConfig.Maze.name = creationConfig.MazePresetName;
            GetOrCreateAsset(ref creationConfig.Maze, $"{creationConfig.PresetFolder}/{creationConfig.MazePresetName}.asset");
        });

        body.Add(title);
        body.Add(stepOneText);
        body.Add(settingsName);
        body.Add(stepTwoText);
        body.Add(settingsFolder);
    }

    private void BasicMazeConfiguration(VisualElement contentHolder)
    {
        var body = new VisualElement();
        body.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
        contentHolder.Add(body);

        var stepOne = StepText("Choose a maze generation configuration (or leave empty to create a new one)");
        var generationConfigurationField = new ObjectField("generation configuration")
        {
            objectType = typeof(AgentMazeGenerator),
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };

        var stepTwo = StepText("Choose a maze settings configuration (or leave empty to create a new one)");
        var settingsConfigurationField = new ObjectField("settings configuration")
        {
            objectType = typeof(MazeSettings),
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };
        
        var stepThree = StepText("Choose a maze instantiator configuration (or leave empty to create a new one)");
        var instantiatorConfigurationField = new ObjectField("instantiator configuration")
        {
            objectType = typeof(MazeInstantiator),
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };

        singleUnloadEvent.Add(() =>
        {
            if (generationConfigurationField.value == null)
            {
                creationConfig.Maze.mazeGenerator = CreateInstance<AgentMazeGenerator>();
                creationConfig.Maze.mazeGenerator.name = $"{creationConfig.MazePresetName}Generator";
                GetOrCreateAsset(ref creationConfig.Maze.mazeGenerator, $"{creationConfig.PresetFolder}/{creationConfig.Maze.mazeGenerator.name}.asset");
            }
            else
            {
                creationConfig.Maze.mazeGenerator = (MazeGenerator) generationConfigurationField.value;
            }
            
            if (settingsConfigurationField.value == null)
            {
                creationConfig.Maze.mazeSettings = CreateInstance<MazeSettings>();
                creationConfig.Maze.mazeSettings.name = $"{creationConfig.MazePresetName}Settings";
                GetOrCreateAsset(ref creationConfig.Maze.mazeSettings, $"{creationConfig.PresetFolder}/{creationConfig.Maze.mazeSettings.name}.asset");
            }
            else
            {
                creationConfig.Maze.mazeSettings = (MazeSettings) settingsConfigurationField.value;
            }

            if (instantiatorConfigurationField.value == null)
            {
                creationConfig.MazeInstantiator = CreateInstance<MazeInstantiator>();
                creationConfig.MazeInstantiator.name = $"{creationConfig.MazePresetName}Instantiator";
                GetOrCreateAsset(ref creationConfig.MazeInstantiator, $"{creationConfig.PresetFolder}/{creationConfig.MazeInstantiator.name}.asset");
            }
            else
            {
                creationConfig.MazeInstantiator = (MazeInstantiator) instantiatorConfigurationField.value;
            }
        });
        
        body.Add(stepOne);
        body.Add(generationConfigurationField);
        body.Add(stepTwo);
        body.Add(settingsConfigurationField);
    }

    private void MazeGenerationConfiguration(VisualElement contentHolder)
    {
        var body = GenerateBody(contentHolder);

        var stepOne = StepText("Config the maze generation");
        Editor subEditor = Editor.CreateEditor(creationConfig.Maze.mazeGenerator);
        var generatorConfig = subEditor.CreateInspectorGUI();
        if(generatorConfig == null) 
            generatorConfig = new IMGUIContainer(subEditor.OnInspectorGUI);
        generatorConfig.style.width = new StyleLength(Length.Percent(80));
        generatorConfig.style.alignSelf = new StyleEnum<Align>(Align.Center);
        
        body.Add(stepOne);
        body.Add(generatorConfig);
    }
    
    private void MazeSettingsConfiguration(VisualElement contentHolder)
    {
        var body = GenerateBody(contentHolder);

        var stepOne = StepText("Choose maze seed");
        var seed = new IntegerField("seed")
        {
            value = 0,
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };
        
        var stepTwo = StepText("Choose maze size");
        var sizeX = new IntegerField("sizeX")
        {
            value = 10,
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };
        var sizeY = new IntegerField("sizeY")
        {
            value = 10,
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };
        
        var stepThree = StepText("Choose split probability");
        
        var splitProbability = new FloatField("splitProbability")
        {
            value = 0.2f,
            tooltip = "probability for a path generator agent to split into two",
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };
        
        singleUnloadEvent.Add(() =>
        {
            creationConfig.Maze.mazeSettings.seed = seed.value;
            creationConfig.Maze.mazeSettings.sizeX = sizeX.value;
            creationConfig.Maze.mazeSettings.sizeY = sizeY.value;
            creationConfig.Maze.mazeSettings.splitProbability = splitProbability.value;
        });
        
        body.Add(stepOne);
        body.Add(seed);
        body.Add(stepTwo);
        body.Add(sizeX);
        body.Add(sizeY);
        body.Add(stepThree);
        body.Add(splitProbability);
    }

    private void MazeInstantiatorConfiguration(VisualElement contentHolder)
    {
        var body = GenerateBody(contentHolder);

        var stepOne = StepText("Choose the GameObject representing each path");
        var forwardPath = new ObjectField("Forward path")
        {
            objectType = typeof(GameObject),
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };

        //var stepTwo = StepText("Choose the GameObject representing turning right path");
        var turningRightPath = new ObjectField("Turning right path")
        {
            objectType = typeof(GameObject),
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };
        
        //var stepThree = StepText("Choose the GameObject representing turning left path");
        var turningLeftPath = new ObjectField("Turning left path")
        {
            objectType = typeof(GameObject),
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };

        //var stepFour = StepText("Choose the GameObject representing splitting path");
        var splittingPath = new ObjectField("Splitting path")
        {
            objectType = typeof(GameObject),
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };

        var crossPath = new ObjectField("Cross path")
        {
            objectType = typeof(GameObject),
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };
        
        var endPath = new ObjectField("End of path")
        {
            objectType = typeof(GameObject),
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };
        
        var endMazePath = new ObjectField("End of maze")
        {
            objectType = typeof(GameObject),
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };

        var stepTwo =
            StepText("Choose the scaling according to your path size (length of the border of your GameObjects)");
        var size = new FloatField("Size")
        {
            value = 1,
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };

        var stepThree = StepText("Choose the grouping size (used to bake multiple small mesh into a bigger one)");
        var groupResolution = new IntegerField("Group Resolution")
        {
            value = 10,
            style =
            {
                width = new StyleLength(Length.Percent(80)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };
        
        body.Add(stepOne);
        body.Add(forwardPath);
        body.Add(turningRightPath);
        body.Add(turningLeftPath);
        body.Add(splittingPath);
        body.Add(crossPath);
        body.Add(endPath);
        body.Add(endMazePath);
        body.Add(stepTwo);
        body.Add(size);
        body.Add(stepThree);
        body.Add(groupResolution);

        singleUnloadEvent.Add(() =>
        {
            creationConfig.MazeInstantiator.forwardPath = (GameObject) forwardPath.value;
            creationConfig.MazeInstantiator.cornerRightPath = (GameObject) turningRightPath.value;
            creationConfig.MazeInstantiator.cornerLeftPath = (GameObject) turningLeftPath.value;
            creationConfig.MazeInstantiator.splitPath = (GameObject) splittingPath.value;
            creationConfig.MazeInstantiator.crossPath = (GameObject) crossPath.value;
            creationConfig.MazeInstantiator.endPath = (GameObject) endPath.value;
            creationConfig.MazeInstantiator.mazeEnd = (GameObject) endMazePath.value;
            creationConfig.MazeInstantiator.size = size.value;
            creationConfig.MazeInstantiator.groupResolution = groupResolution.value;
        });
    }

    private void EndPage(VisualElement contentHolder)
    {
        var body = GenerateBody(contentHolder);
        var info = new Label(
            "You completed the configuration process, to customise even more the generation process look at the documentation online. " +
            "To generate your maze go to the Maze GameObject and click on the 'Generate and Load' button")
        {
            style =
            {
                fontSize = new StyleLength(14),
                width = new StyleLength(Length.Percent(90)),
                alignSelf = new StyleEnum<Align>(Align.Center),
                whiteSpace = WhiteSpace.Normal
            }
        };

        var button = new Button(() =>
        {
            var mazeHolder = new GameObject("Maze");
            var mazeLoader = mazeHolder.AddComponent<MazeLoader>();
            mazeLoader.maze = creationConfig.Maze;
            mazeLoader.mazeInstantiator = creationConfig.MazeInstantiator;
            Close();
        })
        {
            text = "Finish",
            style =
            {
                marginTop = new StyleLength(40),
                width = new StyleLength(Length.Percent(16)),
                height = new StyleLength(Length.Percent(40)),
                alignSelf = new StyleEnum<Align>(Align.Center),
            }
        };

        body.Add(info);
        body.Add(button);
    }

    private static VisualElement GenerateBody(VisualElement contentHolder)
    {
        var body = new VisualElement();
        body.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
        contentHolder.Add(body);
        return body;
    }

    private static Label StepText(string text)
    {
        var stepOneText = new Label(text)
        {
            style =
            {
                fontSize = new StyleLength(16),
                marginBottom = new StyleLength(8),
                marginTop = new StyleLength(8),
                marginLeft = new StyleLength(8)
            }
        };
        return stepOneText;
    }

    public void NextPage(VisualElement contentHolder)
    {
        page += 1;
        ReloadPage(contentHolder);
    }

    public void PreviousPage(VisualElement contentHolder)
    {
        page -= 1;
        ReloadPage(contentHolder);
    }

    public void ReloadPage(VisualElement contentHolder)
    {
        singleUnloadEvent?.ForEach(action => action.Invoke());
        singleUnloadEvent?.Clear();
        contentHolder.Clear();
        currentPageIndicator.text = $"Page {page} : {(int) page + 1}/{pageCreationReferences.Count}";
        backButton.SetEnabled(page > 0);
        nextButton.SetEnabled((int) page < pageCreationReferences.Count - 1);
        
        pageCreationReferences[page](contentHolder);
    }

    public void GetOrCreateAsset<T>(ref T asset, string path) where T : Object
    {
        var presentAsset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (presentAsset== null)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                String[] folders = path.Split('/');
                String currentPath = "Assets";
                for (int i = 1; i < folders.Length - 1; i++)
                {
                    if(AssetDatabase.IsValidFolder(currentPath + $"/{folders[i]}"))
                    {
                        currentPath += $"/{folders[i]}";
                        continue;
                    }

                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                    currentPath += $"/{folders[i]}";
                }
            }
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            AssetDatabase.Refresh();

            return;
        }

        asset = presentAsset;
    }
}
