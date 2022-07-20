using UnityEditor;

namespace Leap.Unity.Preview.HandRays

{
    [CustomEditor(typeof(WristShoulderFarFieldHandRay))]
    public class WristShoulderFarFieldHandRayEditor : CustomEditorBase<WristShoulderFarFieldHandRay>
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            specifyConditionalDrawing("drawDebugGizmos", "drawRay");
            specifyConditionalDrawing("drawDebugGizmos", "rayColor");
            specifyConditionalDrawing("drawDebugGizmos", "drawRayAimAndOrigin");
            specifyConditionalDrawing("drawDebugGizmos", "rayAimAndOriginColor");
            specifyConditionalDrawing("drawDebugGizmos", "drawWristShoulderBlend");
            specifyConditionalDrawing("drawDebugGizmos", "wristShoulderBlendColor");
        }
    }
}