using UnityEngine;
using System.Linq;
using UnityEditor;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(InteractionManager))]
  public class InteractionManagerEditor : Editor {

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (Application.isPlaying) {
        EditorGUILayout.Space();
        InteractionManager manager = target as InteractionManager;

        EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledGroupScope(true)) {
          EditorGUILayout.IntField("Registered Count", manager.RegisteredObjects.Count());
          EditorGUILayout.IntField("Grasped Count", manager.GraspedObjects.Count());
        }
      }
    }
  }
}
