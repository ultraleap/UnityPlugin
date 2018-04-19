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
using System;
using System.Linq;
using Leap.Unity.GraphicalRenderer;

namespace Leap.Unity.Recording {
  using Query;

  [CustomEditor(typeof(HierarchyPostProcess))]
  public class HierarchyPostProcessEditor : CustomEditorBase<HierarchyPostProcess> {
    private const string PREF_PREFIX = "HierarchyPostProcess_ShouldDelete_";

    private bool _expandComponentTypes = false;

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (target == null) {
        return;
      }

      bool isPrefab = PrefabUtility.GetPrefabType(target) == PrefabType.Prefab;
      EditorGUI.BeginDisabledGroup(isPrefab);

      EditorGUILayout.Space();
      if (GUILayout.Button(new GUIContent("Build Playback Prefab",
                                          isPrefab ? "Draw this object into the scene "
                                                   + "before converting its raw recording "
                                                   + "data into AnimationClip data."
                                                   : ""))) {
        EditorApplication.delayCall += () => {
          target.BuildPlaybackPrefab(new ProgressBar());
        };
      }

      EditorGUI.EndDisabledGroup();

      EditorGUILayout.Space();
      _expandComponentTypes = EditorGUILayout.Foldout(_expandComponentTypes, "Component List");
      if (_expandComponentTypes) {
        EditorGUI.indentLevel++;

        var components = target.GetComponentsInChildren<Component>(includeInactive: true).
                                Where(c => !(c is Transform || c is RectTransform)).
                                Select(c => c.GetType()).
                                Distinct().
                                Where(m => m.Namespace != "Leap.Unity.Recording").
                                OrderBy(m => m.Name);

        var groups = components.GroupBy(t => t.Namespace).OrderBy(g => g.Key);

        if (GUILayout.Button("Delete All Checked Components", GUILayout.Height(EditorGUIUtility.singleLineHeight * 2))) {
          foreach (var c in components.Where(t => EditorPrefs.GetBool(getTypePrefKey(t), defaultValue: false)).
                                       SelectMany(c => target.GetComponentsInChildren(c, includeInactive: true))) {
            Undo.DestroyObjectImmediate(c);
          }
        }

        foreach (var group in groups) {
          EditorGUILayout.Space();

          using (new GUILayout.VerticalScope(EditorStyles.helpBox)) {
            using (new GUILayout.HorizontalScope()) {
              var uniform = group.Query().
                                  Select(getTypePrefKey).
                                  Select(k => EditorPrefs.GetBool(k, defaultValue: false)).
                                  UniformOrNone();

              bool valueToUse = false;
              uniform.Match(val => {
                valueToUse = val;
              },
              () => {
                valueToUse = false;
                EditorGUI.showMixedValue = true;
              });

              EditorGUI.BeginChangeCheck();
              valueToUse = EditorGUILayout.ToggleLeft(group.Key ?? "Dev", valueToUse, EditorStyles.boldLabel);
              if (EditorGUI.EndChangeCheck()) {
                foreach (var t in group) {
                  EditorPrefs.SetBool(getTypePrefKey(t), valueToUse);
                }
              }

              EditorGUI.showMixedValue = false;
            }

            EditorGUILayout.Space();
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

            foreach (var type in group) {
              using (new GUILayout.HorizontalScope()) {
                string prefKey = getTypePrefKey(type);
                bool curValue = EditorPrefs.GetBool(prefKey, defaultValue: false);

                EditorGUI.BeginChangeCheck();
                curValue = EditorGUILayout.ToggleLeft(type.Name, curValue);
                if (EditorGUI.EndChangeCheck()) {
                  EditorPrefs.SetBool(prefKey, curValue);
                }

                if (GUILayout.Button("Select", GUILayout.ExpandWidth(false))) {
                  Selection.objects = target.GetComponentsInChildren(type, includeInactive: true).Select(t => t.gameObject).ToArray();
                }

                if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false))) {
                  var toDelete = target.GetComponentsInChildren(type, includeInactive: true);
                  foreach (var t in toDelete) {
                    Undo.DestroyObjectImmediate(t);
                  }
                }
              }
            }
          }
        }
      }
    }

    private static string getTypePrefKey(Type type) {
      return PREF_PREFIX + type.Namespace + "_" + type.Name;
    }
  }
}
