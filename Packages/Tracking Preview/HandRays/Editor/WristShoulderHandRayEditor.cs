using UnityEditor;

namespace Leap.Unity.Preview.HandRays

{
    [CustomEditor(typeof(WristShoulderHandRay))]
    public class WristShoulderHandRayEditor : CustomEditorBase<WristShoulderHandRay>
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