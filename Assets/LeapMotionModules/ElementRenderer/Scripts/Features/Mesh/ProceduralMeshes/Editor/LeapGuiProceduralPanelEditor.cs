using System;
using UnityEngine;
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapGuiProceduralPanel))]
public class LeapGuiProceduralPanelEditor : CustomEditorBase<LeapGuiProceduralPanel> {

  protected override void OnEnable() {
    base.OnEnable();

    specifyCustomDrawer("_resolution_verts_per_meter", drawResolution);

    Func<bool> shouldDrawSize = () => {
      return targets.Query().All(p => p.GetComponent<RectTransform>() == null);
    };
    specifyConditionalDrawing(shouldDrawSize, "_size");

    specifyCustomDrawer("_nineSliced", drawSize);
  }

  private void drawResolution(SerializedProperty property) {
    LeapGuiProceduralPanel.ResolutionType mainType = targets[0].resolutionType;
    bool allSameType = targets.Query().All(p => p.resolutionType == mainType);

    if (!allSameType) {
      return;
    }

    Rect rect = EditorGUILayout.GetControlRect();

    EditorGUI.LabelField(rect, "Resolution");

    rect.x += EditorGUIUtility.labelWidth - 2;
    rect.width -= EditorGUIUtility.labelWidth;
    rect.width *= 2.0f / 3.0f;

    float originalWidth = EditorGUIUtility.labelWidth;
    EditorGUIUtility.labelWidth = 14;

    Rect left = rect;
    left.width /= 2;
    Rect right = left;
    right.x += right.width + 1;

    if (mainType == LeapGuiProceduralPanel.ResolutionType.Vertices) {
      SerializedProperty x = serializedObject.FindProperty("_resolution_vert_x");
      SerializedProperty y = serializedObject.FindProperty("_resolution_vert_y");

      x.intValue = EditorGUI.IntField(left, "X", x.intValue);
      y.intValue = EditorGUI.IntField(right, "Y", y.intValue);
    } else {
      Vector2 value = property.vector2Value;

      value.x = EditorGUI.FloatField(left, "X", value.x);
      value.y = EditorGUI.FloatField(right, "Y", value.y);

      property.vector2Value = value;
    }

    EditorGUIUtility.labelWidth = originalWidth;
  }

  private void drawSize(SerializedProperty property) {
    using (new GUILayout.HorizontalScope()) {
      var canAllNineSlice = targets.Query().All(p => p.canNineSlice);
      using (new EditorGUI.DisabledGroupScope(!canAllNineSlice)) {
        EditorGUILayout.PropertyField(property);
      }

      if (targets.Length == 1) {
        var rectTransform = target.GetComponent<RectTransform>();
        if (rectTransform == null) {
          if (GUILayout.Button("Add Rect Transform", GUILayout.MaxWidth(150))) {
            Vector2 initialSize = target.rect.size;

            rectTransform = target.gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = initialSize;
          }
        }
      }
    }
  }
}
