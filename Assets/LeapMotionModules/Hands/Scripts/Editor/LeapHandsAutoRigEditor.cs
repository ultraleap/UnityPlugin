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
      if (GUILayout.Button("1: AutoRigHands")) {
        autoRigger.AutoRigHands();
      }
      if (GUILayout.Button("2: AutoRigArms")) {
        autoRigger.AutoRigArms();
      }
      if (GUILayout.Button("3: AutoRigUpperBody")) {
        autoRigger.AutoRigUpperBody();
      }
      if (GUILayout.Button("Push Vector Values")) {
        autoRigger.PushVectorValues();
      }
    }
  }
}
