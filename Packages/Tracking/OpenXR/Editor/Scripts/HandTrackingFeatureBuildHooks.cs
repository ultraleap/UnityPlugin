using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;

namespace Leap.Tracking.OpenXR
{
    public partial class HandTrackingFeatureBuildHooks : OpenXRFeatureBuildHooks
    {
        private const string OpenXRPackageRuntimeService = "org.khronos.openxr.OpenXRRuntimeService";
        private const string OpenXRPackageApiLayerService = "org.khronos.openxr.OpenXRApiLayerService";

        private const string MetaHandTrackingFeature = "oculus.software.handtracking";
        private const string MetaHandTrackingPermission = "com.oculus.permission.HAND_TRACKING";
        private const string MetaHandTrackingFrequency = "com.oculus.handtracking.frequency";

        public override int callbackOrder => 1;
        public override Type featureType => typeof(HandTrackingFeature);

        protected override void OnPreprocessBuildExt(BuildReport report)
        {
        }

        protected override void OnPostGenerateGradleAndroidProjectExt(string path)
        {
            var manifest = new AndroidManifest(GetAndroidManifestPath(path));
            var feature = OpenXRSettings.ActiveBuildTargetInstance.GetFeature<HandTrackingFeature>();

            if (PlayerSettings.Android.targetSdkVersion >= AndroidSdkVersions.AndroidApiLevel30)
            {
                // These intent queries are required for OpenXR runtimes and API layers to operate correctly, not all
                // loaders correctly include them so add them if they are missing to ensure applications work correctly.
                manifest.AddQueriesIntentAction(OpenXRPackageRuntimeService);
                manifest.AddQueriesIntentAction(OpenXRPackageApiLayerService);
            }

            if (feature.metaPermissions)
            {
                // Adds the feature and permission to also work on Meta headsets that support hand-tracking.
                manifest.AddUsesFeature(MetaHandTrackingFeature, false);
                manifest.AddUsesPermission(MetaHandTrackingPermission);
            }

            if (feature.metaHighFrequency)
            {
                // Enable Meta high-frequency hand-tracking if requested.
                manifest.AddMetadata(MetaHandTrackingFrequency, "HIGH");
            }

            manifest.Save();
        }

        protected override void OnPostprocessBuildExt(BuildReport report)
        {
        }

        private string GetAndroidManifestPath(string projectPath)
        {
            return Path.Combine(projectPath, "src", "main", "AndroidManifest.xml");
        }
    }
}