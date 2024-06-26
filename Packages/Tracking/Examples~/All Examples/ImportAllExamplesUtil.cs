#if UNITY_EDITOR

using UnityEditor;

namespace Ultraleap.Examples
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
                    "com.ultraleap.tracking",
                    "All Examples",
                    new string[] { "Shared Example Assets REQUIRED", "XR Examples", "Tabletop Examples", "URP Examples" }
                );
            };
        }
    }
}

#endif