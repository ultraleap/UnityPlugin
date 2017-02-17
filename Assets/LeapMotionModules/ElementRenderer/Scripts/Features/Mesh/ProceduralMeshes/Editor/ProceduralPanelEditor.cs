using System;
using UnityEngine;
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;

[CanEditMultipleObjects]
[CustomEditor(typeof(ProceduralPanel))]
public class ProceduralPanelEditor : CustomEditorBase<ProceduralPanel> {

  protected override void OnEnable() {
    base.OnEnable();

    renameProperty("_resolution_verts", "Resolution");
    renameProperty("_resolution_verts_per_meter", "Resolution");
    specifyConditionalDrawing("_resolutionType", (int)ProceduralPanel.ResolutionType.Vertices, "_resolution_verts");
    specifyConditionalDrawing("_resolutionType", (int)ProceduralPanel.ResolutionType.VerticesPerMeter, "_resolution_verts_per_meter");

    Func<bool> shouldDrawSize = () => {
      return targets.Query().All(p => p.GetComponent<RectTransform>() == null);
    };
    specifyConditionalDrawing(shouldDrawSize, "_size");

    specifyCustomDrawer("_nineSliced", drawSize);
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
