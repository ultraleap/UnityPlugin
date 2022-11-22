using JetBrains.Annotations;
using System.IO;
using System.Linq;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
using UnityEditor.Build.Reporting;
#endif

namespace Ultraleap.Tracking.OpenXR.ApiLayer
{
    /// <summary>
    /// Embeddable Ultraleap OpenXR API Layer.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(FeatureId = FeatureId,
        Version = "1.0.0",
        UiName = "Ultraleap Hand Tracking API Layer",
        Company = "Ultraleap",
        Desc = "Embeddable API layer for Ultraleap Hand Tracking",
        Category = FeatureCategory.Feature,
        Required = false,
        OpenxrExtensionStrings = "",
        BuildTargetGroups = new[] { BuildTargetGroup.Android }
    )]
#endif
    public class HandTrackingApiLayerFeature : OpenXRFeature
    {
        [PublicAPI] public const string FeatureId = "com.ultraleap.tracking.openxr.feature.handtracking.api_layer";
    }

#if UNITY_EDITOR
    public class HandTrackingApiLayerFeatureBuildHooks : OpenXRFeatureBuildHooks
    {
        public override int callbackOrder => 1;
        public override Type featureType => typeof(HandTrackingApiLayerFeature);

        protected override void OnPreprocessBuildExt(BuildReport report)
        {
        }

        protected override void OnPostGenerateGradleAndroidProjectExt(string path)
        {
            // Remove libLeapC.so and AndroidServiceBinder.aar before Gradle is run.
            // This is to work-around the fact that they are also included in the embedded API layer and will cause
            // conflicts.
            RemoveAAR(path, "UltraleapTrackingServiceBinder");
            RemoveLibrary(path, "arm64-v8a", "LeapC");
        }

        protected override void OnPostprocessBuildExt(BuildReport report)
        {
        }

        /// <summary>
        /// Removes an AAR from the project and the associated Gradle build script
        /// </summary>
        /// <param name="path">The project path</param>
        /// <param name="aarName">The name of the AAR library with out the `.aar` extension</param>
        private void RemoveAAR(string path, string aarName)
        {
            // Delete the AAR from the project before Gradle is run.
            var aarPath = Path.Combine(path, "libs", $"{aarName}.aar");
            if (File.Exists(aarPath))
            {
                File.Delete(aarPath);
            }

            // Remove the reference to the AAR from the Gradle build script.
            var buildScriptPath = Path.Combine(path, "build.gradle");
            var buildScriptContent = File.ReadAllLines(buildScriptPath);
            var modifiedBuildScriptContent = buildScriptContent.Where(
                line => !(line.Contains("implementation(") && line.Contains(aarName) && line.Contains("ext:'aar'"))
            );
            File.WriteAllLines(buildScriptPath, modifiedBuildScriptContent);
        }

        /// <summary>
        /// Removes a .so library from the build
        /// </summary>
        /// <param name="path">The project path</param>
        /// <param name="architecture">The CPU architecture of the library</param>
        /// <param name="libName">the library name without the .so or lib prefix</param>
        private void RemoveLibrary(string path, string architecture, string libName)
        {
            var libPath = Path.Combine(path, "src", "main", "jniLibs", architecture, $"lib{libName}.so");
            if (File.Exists(libPath))
            {
                File.Delete(libPath);
            }
        }
    }
#endif
}