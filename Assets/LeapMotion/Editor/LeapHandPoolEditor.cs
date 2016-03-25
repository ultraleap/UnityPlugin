using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Leap.Unity {
  [CustomEditor(typeof(HandPool))]
  public class LeapHandPoolEditor : Editor {
    public override void OnInspectorGUI() {
      serializedObject.Update();
      LeapEditorList.Show(serializedObject.FindProperty("ModelCollection"));
      EditorGUILayout.PropertyField(serializedObject.FindProperty("EnforceHandedness"));
      if (Application.isPlaying) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ModelPool"), true);
      }
      serializedObject.ApplyModifiedProperties();
    }

  }
}
