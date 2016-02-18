using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(LeapVRTemporalWarping))]
public class LeapTemporalWarpingEditor : Editor {

  public override void OnInspectorGUI() {
    serializedObject.Update();
    SerializedProperty properties = serializedObject.GetIterator();

    bool useEnterChildren = true;
    while (properties.NextVisible(useEnterChildren) == true) {
      useEnterChildren = false;
      EditorGUILayout.PropertyField(properties, true);
    }
    serializedObject.ApplyModifiedProperties();
  }
}

  