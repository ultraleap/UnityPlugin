/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Leap.Unity.InputModule {

  [CustomEditor(typeof(CompressibleUI))]
  public class CompressibleUIEditor : CustomEditorBase<CompressibleUI> {
    private ReorderableList list;
    protected override void OnEnable() {
      base.OnEnable();
      list = new ReorderableList(serializedObject,
                                 serializedObject.FindProperty("Layers"),
                                 true, true, true, true);
      list.drawHeaderCallback = drawHeader;
      list.drawElementCallback = drawElement;
      list.elementHeight = EditorGUIUtility.singleLineHeight * 7;
      specifyCustomDrawer("Layers", doLayoutList);

      specifyConditionalDrawing(() => showEventTrigger(),
                            "LayerCollapse",
                            "LayerExpand",
                            "LayerDepress");
    }

    private bool showEventTrigger() {
      bool showEventTrigger = false;
      if (target.Layers != null) {
        for (int i = 0; i < target.Layers.Length; i++) {
          if (target.Layers[i].TriggerLayerEvent) {
            showEventTrigger = true;
          }
        }
      }
      return showEventTrigger;
    }

    private void doLayoutList(SerializedProperty p) {
      list.DoLayoutList();
    }

    private void drawHeader(Rect rect) {
      EditorGUI.LabelField(rect, "Floating UI Layers");
    }

    private void drawElement(Rect rect, int index, bool isActive, bool isFocused) {
      var element = list.serializedProperty.GetArrayElementAtIndex(index);
      Rect r = rect;
      r.height /= 7;
       
      EditorGUI.PropertyField(r, element.FindPropertyRelative("LayerTransform"));
      r.y += EditorGUIUtility.singleLineHeight;
      EditorGUI.PropertyField(r, element.FindPropertyRelative("MaxFloatDistance"));
      r.y += EditorGUIUtility.singleLineHeight;
      EditorGUI.PropertyField(r, element.FindPropertyRelative("MinFloatDistance"));
      r.y += EditorGUIUtility.singleLineHeight;
      EditorGUI.PropertyField(r, element.FindPropertyRelative("Shadow"));
      r.y += EditorGUIUtility.singleLineHeight;
      EditorGUI.PropertyField(r, element.FindPropertyRelative("ShadowOnAboveLayer"));
      r.y += EditorGUIUtility.singleLineHeight;
      EditorGUI.PropertyField(r, element.FindPropertyRelative("TriggerLayerEvent"));
      //r.y += EditorGUIUtility.singleLineHeight*1.5f;
      //r.height /= 10;
      //EditorGUI.DrawRect(r,new Color(0.25f, 0.25f, 0.25f));
      //r.height *= 10;
    }
  }
}
