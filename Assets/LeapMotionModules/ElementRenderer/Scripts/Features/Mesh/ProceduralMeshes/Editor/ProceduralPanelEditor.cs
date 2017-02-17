using UnityEditor;
using Leap.Unity;

[CanEditMultipleObjects]
[CustomEditor(typeof(ProceduralPanel))]
public class ProceduralPanelEditor : CustomEditorBase<ProceduralPanel> {

  protected override void OnEnable() {
    base.OnEnable();

    specifyConditionalDrawing("_resolutionMode", 
                              (int)ProceduralPanel.ResolutionMode.Explicit, 
                              "_resolutionX", 
                              "_resolutionY");
    specifyConditionalDrawing("_resolutionMode",
                              (int)ProceduralPanel.ResolutionMode.Implicit,
                              "_vertexPerMeterX",
                              "_vertexPerMeterY");
  }
}
