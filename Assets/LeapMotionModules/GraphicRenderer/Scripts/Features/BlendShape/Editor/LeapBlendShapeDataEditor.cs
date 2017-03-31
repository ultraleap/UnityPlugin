using UnityEditor;
using Leap.Unity;

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapBlendShapeData))]
public class LeapBlendShapeDataEditor : CustomEditorBase {
  protected override void OnEnable() {
    base.OnEnable();

    if (target == null) return;

    dontShowScriptField();

    specifyConditionalDrawing("_type", (int)LeapBlendShapeData.BlendShapeType.Translation, "_translation");
    specifyConditionalDrawing("_type", (int)LeapBlendShapeData.BlendShapeType.Rotation, "_rotation");
    specifyConditionalDrawing("_type", (int)LeapBlendShapeData.BlendShapeType.Scale, "_scale");
    specifyConditionalDrawing("_type", (int)LeapBlendShapeData.BlendShapeType.Mesh, "_mesh");
  }
}
