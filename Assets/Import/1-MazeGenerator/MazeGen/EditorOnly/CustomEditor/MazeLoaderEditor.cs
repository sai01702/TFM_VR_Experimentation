#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MazeGen
{
    [CustomEditor(typeof(MazeLoader))]
    public class MazeLoaderEditor : Editor
    {
        private MazeLoader _mazeLoader;
        private Editor[] _editors;
        private bool[] _editorsFoldouts;
        private void OnEnable()
        {
            _mazeLoader = (MazeLoader) target;
            _editors = new Editor[2];
            _editorsFoldouts = new bool[2];
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
    
            _mazeLoader.rootMazeObject =
                (GameObject) EditorGUILayout.ObjectField("Root maze object", _mazeLoader.rootMazeObject, typeof(GameObject), true);
    
            CustomEditorUtility.DrawCompleteScriptableObjectEditor("Maze Instantiator",ref _mazeLoader.mazeInstantiator,ref _editorsFoldouts[0], ref _editors[0]);
            
            CustomEditorUtility.DrawCompleteScriptableObjectEditor("Maze",ref _mazeLoader.maze,ref _editorsFoldouts[1], ref _editors[1]);
    
            if (GUILayout.Button("Create"))
            {
                _mazeLoader.Create();
            }
            if (GUILayout.Button("Load"))
            {
                _mazeLoader.Load(false,false);
            }
            if (GUILayout.Button("Bake"))
            {
                _mazeLoader.Bake();
            }
            
            if (GUILayout.Button("Create and Load"))
            {
                _mazeLoader.Load(false,true);
            }
            
            if (GUILayout.Button("Load and Bake"))
            {
                _mazeLoader.Load(true,false);
            }
    
            if (GUILayout.Button("Create, Load and Bake"))
            {
                _mazeLoader.Load(true,true);
            }
            
            if (GUILayout.Button("Destroy"))
            {
                _mazeLoader.Destroy();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif