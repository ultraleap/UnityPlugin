using UnityEditor;

namespace Leap.Unity.Readme
{
    [CustomEditor(typeof(SceneReadmePing))]
    public class SceneReadmePingEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (!SceneReadmeEditor.SelectSceneReadme(true))
            {
                EditorGUILayout.HelpBox("No Readme currently exists for this scene.", MessageType.Warning);
            }
        }
    }
}