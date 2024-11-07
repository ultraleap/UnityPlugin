using UnityEditor;

namespace Leap.Preview.HandRays
{
    [CustomEditor(typeof(InferredBodyPositions))]
    public class InferredBodyPositionsEditor : CustomEditorBase<InferredBodyPositions>
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            specifyConditionalDrawing("drawDebugGizmos", "debugGizmoColor");
            specifyConditionalDrawing("drawDebugGizmos", "drawHeadPosition");
            specifyConditionalDrawing("drawDebugGizmos", "drawNeckPosition");
            specifyConditionalDrawing("drawDebugGizmos", "drawShoulderPositions");
        }
    }
}