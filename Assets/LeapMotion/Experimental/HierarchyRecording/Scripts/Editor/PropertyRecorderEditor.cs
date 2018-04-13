/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;

namespace Leap.Unity.Recording {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(PropertyRecorder))]
  public class PropertyRecorderEditor : CustomEditorBase<PropertyRecorder> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDrawer("_bindings", drawProperties);
      hideField("_expandedTypes");
    }

    private void drawProperties(SerializedProperty list) {
      List<EditorCurveBinding> bindings = null;

      foreach (var target in targets) {
        var parent = target.transform.root.gameObject;
        var singleBinding = new List<EditorCurveBinding>(AnimationUtility.GetAnimatableBindings(target.gameObject, parent));
        if (bindings == null) {
          bindings = new List<EditorCurveBinding>(singleBinding);
        } else {
          for (int i = bindings.Count; i-- != 0;) {
            if (!singleBinding.Query().Any(t => t.propertyName == bindings[i].propertyName && t.type == bindings[i].type)) {
              bindings.RemoveAt(i);
            }
          }
        }
      }

      bindings = bindings.Query().
                          Where(t => t.type != typeof(PropertyRecorder) &&
                                     t.type != typeof(GameObject) &&
                                     t.type != typeof(Transform) &&
                                     t.propertyName != "m_Enabled").
                          ToList();

      bool shouldOverride = false;
      bool overrideValue = false;
      Type currType = null;
      EditorGUI.indentLevel++;

      foreach (var binding in bindings) {
        bool isTypeExpanded = targets.All(t => t.IsBindingExpanded(binding));

        if (binding.type != currType) {
          currType = binding.type;
          shouldOverride = false;

          EditorGUI.indentLevel--;
          EditorGUILayout.BeginHorizontal();

          isTypeExpanded = EditorGUILayout.Foldout(isTypeExpanded, binding.type.Name);
          foreach (var target in targets) {
            target.SetBindingExpanded(binding, isTypeExpanded);
          }

          if (GUILayout.Button("Record All")) {
            shouldOverride = true;
            overrideValue = true;
          }

          if (GUILayout.Button("Clear All")) {
            shouldOverride = true;
            overrideValue = false;
          }

          EditorGUILayout.EndHorizontal();
          EditorGUI.indentLevel++;
        }

        if (isTypeExpanded) {
          EditorGUI.showMixedValue = !targets.Query().Select(t => t.IsBindingEnabled(binding)).AllEqual();
          bool isEnabled = target.IsBindingEnabled(binding);

          EditorGUI.BeginChangeCheck();
          isEnabled = EditorGUILayout.ToggleLeft(binding.propertyName, isEnabled);

          if (EditorGUI.EndChangeCheck()) {
            foreach (var target in targets) {
              target.SetBindingEnabled(binding, isEnabled);
            }
          }
        }

        if (shouldOverride) {
          foreach (var target in targets) {
            target.SetBindingEnabled(binding, overrideValue);
          }
        }
      }

      EditorGUI.indentLevel--;

      serializedObject.Update();
    }
  }

}
