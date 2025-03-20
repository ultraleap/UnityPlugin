#if UNITY_EDITOR

using Leap.Examples;
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
                    new string[]
                    {
                        "Shared Example Assets REQUIRED",
                        "Main Examples",
                        "Unity Input Manager (Old)"
                    }
                );
            };
        }
    }
}

#endif