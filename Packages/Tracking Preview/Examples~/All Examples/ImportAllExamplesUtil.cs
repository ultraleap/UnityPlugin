#if UNITY_EDITOR

using UnityEditor;

namespace Leap.Preview.Examples
{
    public static class AllSamplesDependencyImporter
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void CreateAssetWhenReady()
        {
            EditorApplication.delayCall += () =>
            {
                SampleDependencyImporter.FindAndImportSampleDependencies
                (
                    "com.ultraleap.tracking.preview",
                    "All Examples",
                    new string[] { "Main Examples", "Unity Input Manager (Old)" }
                );
            };
        }
    }
}

#endif