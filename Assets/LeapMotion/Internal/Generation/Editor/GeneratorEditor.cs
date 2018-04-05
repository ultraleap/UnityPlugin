/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
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
      int successfulGenerators = 0;
      int failedGenerators = 0;
      foreach (var gen in EditorResources.FindAllAssetsOfType<GeneratorBase>()) {
        try {
          gen.Generate();
          successfulGenerators++;
        } catch (Exception e) {
          Debug.LogException(e);
          failedGenerators++;
        }
      }

      if (successfulGenerators == 1) {
        Debug.Log("Successfully ran 1 generator.");
      } else if (successfulGenerators > 1) {
        Debug.Log("Successfully ran " + successfulGenerators + " generators.");
      }

      if (failedGenerators == 1) {
        Debug.LogError("1 generator failed to run.");
      } else if (failedGenerators > 1) {
        Debug.LogError(failedGenerators + " generators failed to run.");
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
