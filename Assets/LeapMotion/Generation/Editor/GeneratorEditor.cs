/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Generation {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(GeneratorBase), editorForChildClasses: true)]
  public class GeneratorEditor : CustomEditorBase<GeneratorBase> {

    [MenuItem("Assets/Run All Generators")]
    public static void TriggerGeneration() {
      foreach (var gen in EditorResources.FindAllAssetsOfType<GeneratorBase>()) {
        try {
          gen.Generate();
        } catch (Exception e) {
          Debug.LogException(e);
        }
      }
      AssetDatabase.Refresh();
      AssetDatabase.SaveAssets();
    }

    protected override void OnEnable() {
      base.OnEnable();

      dontShowScriptField();
    }

    public override void OnInspectorGUI() {
      drawScriptField();

      if (GUILayout.Button("Generate")) {
        foreach (var target in targets) {
          target.Generate();
        }

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
      }

      base.OnInspectorGUI();
    }
  }
}
