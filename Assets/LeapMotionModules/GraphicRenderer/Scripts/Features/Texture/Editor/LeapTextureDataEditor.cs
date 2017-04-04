using UnityEngine;
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(LeapTextureData))]
  public class LeapTextureDataEditor : CustomEditorBase<LeapTextureData> {

    protected override void OnEnable() {
      base.OnEnable();
      dontShowScriptField();

      specifyCustomDrawer("texture", drawTexture);
    }

    private void drawTexture(SerializedProperty property) {
      var mainData = targets[0];
      var mainFeature = mainData.feature as LeapTextureFeature;

      if (targets.Query().Any(t => t.feature != mainData.feature)) {
        mainFeature = null;
      }

      if (mainFeature != null) {
        EditorGUILayout.PropertyField(property, new GUIContent(mainFeature.propertyName, property.tooltip));
      } else {
        int mainIndex = mainData.graphic.featureData.IndexOf(mainData);
        if (targets.Query().Any(t => t.graphic.featureData.IndexOf(t) != mainIndex)) {
          EditorGUILayout.PropertyField(property);
        } else {
          EditorGUILayout.PropertyField(property, new GUIContent("Channel " + mainIndex, property.tooltip));
        }
      }
    }
  }
}
