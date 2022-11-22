using JetBrains.Annotations;
using UnityEditor.Build.Reporting;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
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
        BuildTargetGroups = new[] {BuildTargetGroup.Android}
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
        }

        protected override void OnPostprocessBuildExt(BuildReport report)
        {
            
        }
        
    }
#endif
    
    
}