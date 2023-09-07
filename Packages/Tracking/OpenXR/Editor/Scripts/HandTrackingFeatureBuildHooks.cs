using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;

namespace Ultraleap.Tracking.OpenXR
{
    public partial class HandTrackingFeatureBuildHooks : OpenXRFeatureBuildHooks
    {
        private const string OpenXRPackageRuntimeService = "org.khronos.openxr.OpenXRRuntimeService";
        private const string OpenXRHandTrackingFeature = "org.khronos.openxr.feature.ext.HAND_TRACKING";
        private const string OpenXRHandTrackingPermission = "org.khronos.openxr.permission.ext.HAND_TRACKING";

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

            if (PlayerSettings.Android.minSdkVersion >= AndroidSdkVersions.AndroidApiLevel30)
            {
                // Intent query is required for OpenXR to work correctly.
                manifest.AddQueriesIntentAction(OpenXRPackageRuntimeService);
            }

            // Adds the feature and permission for headsets that support the unified hand-tracking permission.
            manifest.AddUsesFeature(OpenXRHandTrackingFeature, false);
            manifest.AddUsesPermission(OpenXRHandTrackingPermission);

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