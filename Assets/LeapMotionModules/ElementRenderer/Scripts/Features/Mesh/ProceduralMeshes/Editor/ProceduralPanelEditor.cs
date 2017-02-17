using UnityEditor;
using Leap.Unity;

[CanEditMultipleObjects]
[CustomEditor(typeof(ProceduralPanel))]
public class ProceduralPanelEditor : CustomEditorBase<ProceduralPanel> {

  protected override void OnEnable() {
    base.OnEnable();

    specifyCustomDrawer("_resolutionType", resolutionTypeDecorator);
  }

  private void resolutionTypeDecorator(SerializedProperty prop) {
    var newValue = EditorGUILayout.EnumPopup((ProceduralPanel.ResolutionType)prop.intValue);
  }
}
