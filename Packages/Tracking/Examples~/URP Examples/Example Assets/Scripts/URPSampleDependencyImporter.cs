#if UNITY_EDITOR

using UnityEditor;

namespace Leap.Examples
{
    public static class URPSampleDependencyImporter
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void CreateAssetWhenReady()
        {
            EditorApplication.delayCall += () =>
            {
                SampleDependencyImporter.FindAndImportSampleDependencies
                (
                    "com.ultraleap.tracking",
                    "URP Examples",
                    new string[] { "Shared Example Assets REQUIRED" }
                );
            };
        }
    }
}

#endif