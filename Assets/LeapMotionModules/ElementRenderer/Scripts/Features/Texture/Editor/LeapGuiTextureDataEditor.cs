using UnityEngine;
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapGuiTextureData))]
public class LeapGuiTextureDataEditor : CustomEditorBase<LeapGuiTextureData> {

  protected override void OnEnable() {
    base.OnEnable();
    dontShowScriptField();

    specifyCustomDrawer("texture", drawTexture);
  }

  private void drawTexture(SerializedProperty property) {
    var mainData = targets[0];
    var mainFeature = mainData.feature as LeapGuiTextureFeature;

    if (targets.Query().Any(t => t.feature != mainData.feature)) {
      mainFeature = null;
    }

    if (mainFeature != null) {
      EditorGUILayout.PropertyField(property, new GUIContent(mainFeature.propertyName, property.tooltip));
    } else {
      EditorGUILayout.PropertyField(property);
    }
  }
}
