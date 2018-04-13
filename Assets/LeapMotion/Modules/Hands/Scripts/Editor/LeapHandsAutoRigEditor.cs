/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Leap.Unity {
  [CustomEditor(typeof(LeapHandsAutoRig))]
  public class ObjectBuilderEditor : Editor {
    public override void OnInspectorGUI() {
      GUILayout.Space(10);
      GUILayout.Label("Leap Hand Auto Rigger", EditorStyles.largeLabel);
      GUILayout.Label("Guidelines for creating FBX hand assets and instructions for auto rigging are at:", EditorStyles.wordWrappedLabel);
      GUILayout.Label("https://developer.leapmotion.com/documentation/unity/unity/Unity_Hand_Assets.html", EditorStyles.wordWrappedLabel);
      GUILayout.Space(10);

      DrawDefaultInspector();
      LeapHandsAutoRig autoRigger = (LeapHandsAutoRig)target;
      if (GUILayout.Button("AutoRig")) {
        autoRigger.AutoRig();
      }
      if (GUILayout.Button("Push Vector Values")) {
        autoRigger.PushVectorValues();
      }
    }
  }
}
