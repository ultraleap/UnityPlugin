using UnityEditor;
using UnityEngine;

namespace Leap.Unity.HandsModule
{
    [CustomEditor(typeof(HandModelManager))]
    public class HandModelManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            HandModelManager manager = (HandModelManager)target;
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Register All Hand Models In Scene", GUILayout.Width(250), GUILayout.Height(20)))
            {
                manager.RegisterAllUnregisteredHandModels();
                EditorUtility.SetDirty(manager);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            DrawDefaultInspector();
        }
    }
}