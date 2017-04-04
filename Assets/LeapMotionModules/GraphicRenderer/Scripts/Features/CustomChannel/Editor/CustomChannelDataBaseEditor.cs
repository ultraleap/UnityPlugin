using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(CustomChannelDataBase), editorForChildClasses: true)]
  public class CustomChannelDataBaseEditor : CustomEditorBase<CustomChannelDataBase> {

    protected override void OnEnable() {
      base.OnEnable();

      dontShowScriptField();
      specifyCustomDrawer("_value", drawValue);
    }

    private void drawValue(SerializedProperty property) {
      var mainData = targets[0];
      var mainFeature = mainData.feature as ICustomChannelFeature;

      if (targets.Query().Any(t => t.feature != mainData.feature)) {
        mainFeature = null;
      }

      if (mainFeature != null) {
        EditorGUILayout.PropertyField(property, new GUIContent(mainFeature.channelName, property.tooltip), true);
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
