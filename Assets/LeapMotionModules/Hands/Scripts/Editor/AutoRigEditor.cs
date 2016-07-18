using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Leap.Unity {
  [CustomEditor(typeof(LeapHandsAutoRig))]
  public class ObjectBuilderEditor : Editor {
    public override void OnInspectorGUI() {
      DrawDefaultInspector();

      LeapHandsAutoRig autoRigger = (LeapHandsAutoRig)target;
      if (GUILayout.Button("AutoRig")) {
        autoRigger.AutoRig();
      }
      if (GUILayout.Button("Store Start Pose")) {
        autoRigger.StoreStartPose();
      }
      if (GUILayout.Button("Reset To Start Pose")) {
        autoRigger.ResetStartPose();
      }
    }
  }
}
