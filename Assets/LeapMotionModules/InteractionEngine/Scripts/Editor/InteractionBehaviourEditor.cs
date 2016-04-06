using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(InteractionBehaviour), true)]
  public class InteractionBehaviourEditor : Editor {

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (Application.isPlaying) {
        EditorGUILayout.Space();
        InteractionBehaviour behaviour = target as InteractionBehaviour;

        if (!behaviour.IsInteractionEnabled) {
          EditorGUILayout.LabelField("Interaction Disabled", EditorStyles.boldLabel);
        } else {
          EditorGUILayout.LabelField("Interaction Info", EditorStyles.boldLabel);
          using (new EditorGUI.DisabledGroupScope(true)) {
            EditorGUILayout.IntField("Grasping Hand Count", behaviour.GraspingHandCount);
            EditorGUILayout.IntField("Untracked Hand Count", behaviour.UntrackedHandCount);
          }
        }
      }
    }
  }
}
