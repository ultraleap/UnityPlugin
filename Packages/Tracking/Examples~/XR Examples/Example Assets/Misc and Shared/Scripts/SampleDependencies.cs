/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

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