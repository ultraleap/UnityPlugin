using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(InteractionBehaviour), true)]
  public class InteractionBehaviourEditor : CustomEditorBase {
    private InteractionBehaviour _interactionBehaviour;

    protected override void OnEnable() {
      base.OnEnable();

      _interactionBehaviour = target as InteractionBehaviour;

      specifyCustomDecorator("_manager", kinematicDecorator);
      specifyCustomDecorator("_graphicalAnchor", graphicalAnchor);
      specifyCustomDrawer("_pushingEnabled", pushingDrawer);
    }

    private void kinematicDecorator(SerializedProperty prop) {
      Rigidbody rigidbody = _interactionBehaviour.GetComponent<Rigidbody>();

      if (rigidbody == null) {
        EditorGUILayout.HelpBox("This component requires a Rigidbody", MessageType.Error);
      } else {
        if (rigidbody.interpolation == RigidbodyInterpolation.None) {
          EditorGUILayout.HelpBox("It is recommended to use interpolation on Rigidbodies to improve interaction fidelity.", MessageType.Warning);
        }

        if (rigidbody.isKinematic) {
          if (rigidbody.useGravity) {
            EditorGUILayout.HelpBox("Rigidbody is set as Kinematic but has gravity enabled.", MessageType.Warning);
          } else {
            EditorGUILayout.HelpBox("Will be simulated as Kinematic.", MessageType.Info);
          }
        } else {
          if (rigidbody.useGravity) {
            EditorGUILayout.HelpBox("Will be simulated with gravity.", MessageType.Info);
          } else {
            EditorGUILayout.HelpBox("Will be simulated without gravity.", MessageType.Info);
          }
        }
      }
    }

    private void graphicalAnchor(SerializedProperty prop) {
      if (prop.objectReferenceValue == null) {
        EditorGUILayout.HelpBox("It is recommended to use the graphical anchor to improve interaction fidelity.", MessageType.Warning);
      }
    }

    private void pushingDrawer(SerializedProperty prop) {
      Rigidbody rigidbody = _interactionBehaviour.GetComponent<Rigidbody>();

      if (!rigidbody.isKinematic) {
        EditorGUILayout.PropertyField(prop);
      }
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (Application.isPlaying) {
        EditorGUILayout.Space();
        InteractionBehaviourBase behaviour = target as InteractionBehaviourBase;

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
