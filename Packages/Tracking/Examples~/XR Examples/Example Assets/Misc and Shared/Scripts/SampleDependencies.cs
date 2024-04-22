#if UNITY_EDITOR

using UnityEditor;

namespace Leap.Examples
{
    public static class XRSampleDependencyImporter
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void CreateAssetWhenReady()
        {
            EditorApplication.delayCall += () =>
            {
                SampleDependencyImporter.FindAndImportSampleDependencies
                (
                    "com.ultraleap.tracking",
                    "XR Examples",
                    new string[] { "Shared Example Assets REQUIRED" }
                );
            };
        }
    }
}

#endif