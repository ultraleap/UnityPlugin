using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Interaction {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(InteractionBehaviour), true)]
  public class InteractionBehaviourEditor : InteractionBehaviourBaseEditor {

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDecorator("_graphicalAnchor", graphicalAnchor);
      specifyCustomDrawer("_pushingEnabled", pushingDrawer);
    }

    private void graphicalAnchor(SerializedProperty prop) {
      if (prop.objectReferenceValue == null) {
        EditorGUILayout.HelpBox("It is recommended to use the graphical anchor to improve interaction fidelity.", MessageType.Warning);
      }
    }

    private void pushingDrawer(SerializedProperty prop) {
      bool shouldShow = false;

      if (_interactionBehaviour == null) {
        shouldShow = true;
      } else {
        Rigidbody rigidbody = _interactionBehaviour.GetComponent<Rigidbody>();
        if (!rigidbody.isKinematic) {
          shouldShow = true;
        }
      }

      if (shouldShow) {
        EditorGUILayout.PropertyField(prop);
      }
    }
  }
}
