#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MazeGen
{
    [CustomEditor(typeof(Maze))]
    public class MazeEditor : Editor
    {
    
        private Maze _maze;
        private Editor[] _editors;
        private bool[] _editorsFoldouts;
        private Vector2Int _search;
    
        private void OnEnable()
        {
            _maze = (Maze) target;
            _editors = new Editor[2];
            _editorsFoldouts = new bool[2];
        }
    
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            CustomEditorUtility.DrawCompleteScriptableObjectEditor("Maze generator", ref _maze.mazeGenerator, ref _editorsFoldouts[0], ref _editors[0]);
            CustomEditorUtility.DrawCompleteScriptableObjectEditor("Maze settings", ref _maze.mazeSettings, ref _editorsFoldouts[1], ref _editors[1]);
            
            if (_maze.GetMazeOrNull() != null)
            {
                if (_maze.mazeGenerator.Finish)
                {
                    if (GUILayout.Button("Recreate"))
                    {
                        _maze.Create();
                    }
                }
                else
                {
                    GUILayout.Button("Creating...");
                }
            }
            else
            {
                if(GUILayout.Button("Create"))
                {
                    _maze.Create();
                }
            }
    
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif