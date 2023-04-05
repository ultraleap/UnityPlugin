using UnityEditor;

namespace Leap.Unity.Readme
{
    [CustomEditor(typeof(SceneReadmePing))]
    public class SceneReadmePingEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SceneReadmeEditor.SelectSceneReadme();
        }
    }
}