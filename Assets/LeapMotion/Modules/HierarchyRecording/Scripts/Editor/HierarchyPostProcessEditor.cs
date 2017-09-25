/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;
using Leap.Unity.GraphicalRenderer;

namespace Leap.Unity.Recording {

  [CustomEditor(typeof(HierarchyPostProcess))]
  public class HierarchyPostProcessEditor : CustomEditorBase<HierarchyPostProcess> {

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      bool isPrefab = PrefabUtility.GetPrefabType(target) == PrefabType.Prefab;
      EditorGUI.BeginDisabledGroup(isPrefab);

      if (GUILayout.Button("Clear")) {
        target.ClearComponents();
      }

      if (GUILayout.Button(new GUIContent("Build Playback Prefab",
                                          isPrefab ? "Draw this object into the scene "
                                                   + "before converting its raw recording "
                                                   + "data into AnimationClip data."
                                                   : ""))) {
        target.BuildPlaybackPrefab(new ProgressBar());
      }

      EditorGUI.EndDisabledGroup();
    }
  }


}
