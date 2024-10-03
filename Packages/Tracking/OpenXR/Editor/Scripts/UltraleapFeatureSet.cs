using JetBrains.Annotations;
using Leap.Tracking.OpenXR.ApiLayer;
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;

namespace Leap.Tracking.OpenXR
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
    public class UltraleapFeatureSet
    {
        [PublicAPI] public const string FeatureSetId = "com.ultraleap.tracking.openxr.featureset.handtracking";
    }
}
