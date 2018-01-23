/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Packaging {

  [CustomEditor(typeof(BuildDefinition))]
  public class BuildDefinitionEditor : DefinitionBaseEditor<BuildDefinition> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDecorator("_options", prop => drawExportFolder(prop, "Build", "Build Folder"));
      specifyCustomDrawer("_options", drawOptions);

      createList("_scenes", drawScene);
      createList("_targets", drawBuildTarget);
    }

    protected override void OnBuild() {
      target.Build();
    }

    protected override int GetBuildMenuPriority() {
      return 20;
    }

    protected override string GetBuildMethodName() {
      return "Build";
    }

    private void drawScene(Rect rect, SerializedProperty property) {
      float originalWidth = EditorGUIUtility.labelWidth;
      EditorGUIUtility.labelWidth *= 0.2f;

      string label = new string(property.displayName.Where(c => char.IsDigit(c)).ToArray());
      EditorGUI.PropertyField(rect, property, new GUIContent(label));

      EditorGUIUtility.labelWidth = originalWidth;
    }

    private void drawBuildTarget(Rect rect, SerializedProperty property) {
      EditorGUI.PropertyField(rect, property, GUIContent.none);
    }

    private void drawOptions(SerializedProperty prop) {
      EditorGUILayout.BeginHorizontal();

      EditorGUILayout.PropertyField(prop);

      if (GUILayout.Button("Debug", GUILayout.ExpandWidth(false))) {
        prop.intValue = (int)(BuildOptions.AllowDebugging |
                              BuildOptions.ConnectWithProfiler |
                              BuildOptions.Development |
                              BuildOptions.ForceEnableAssertions);
      }

      EditorGUILayout.EndHorizontal();
    }
  }
}
