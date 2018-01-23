/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Leap.Unity.Testing;


namespace Leap.Unity.GraphicalRenderer.Tests {

  public static class SetupGraphicRendererTests {
    [SetupLeapTests]
    private static void setupTests() {
      var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
      addScene("ShaderOutputTestScenes/StationaryBakedRendererShaderTestScene", scenes);
      addScene("ShaderOutputTestScenes/TranslationBakedRendererShaderTestScene", scenes);

      EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void addScene(string name, List<EditorBuildSettingsScene> scenes) {
      var sceneAsset = EditorResources.Load<SceneAsset>(name);
      if (sceneAsset == null) {
        Debug.LogWarning("Could not find scene " + name);
        return;
      }

      string path = AssetDatabase.GetAssetPath(sceneAsset);
      scenes.Add(new EditorBuildSettingsScene(path, true));
    }
  }
}
#endif
