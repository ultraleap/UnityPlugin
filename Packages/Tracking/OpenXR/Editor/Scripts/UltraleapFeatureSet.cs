using Ultraleap.Tracking.OpenXR.ApiLayer;
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace Ultraleap.Tracking.OpenXR
{
    [OpenXRFeatureSet(
        FeatureIds = new string[]
        {
            HandTrackingFeature.FeatureId,
            HandTrackingApiLayerFeature.FeatureId
        },
        UiName = "Ultraleap Hand Tracking",
        Description = "Ultraleap hand-tracking support",
        FeatureSetId = FeatureSetId,
        SupportedBuildTargets = new BuildTargetGroup[]
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android
        }
    )]
    class UltraleapFeatureSet
    {
        public const string FeatureSetId = "com.ultraleap.tracking.openxr.featureset.handtracking";
    }
}
