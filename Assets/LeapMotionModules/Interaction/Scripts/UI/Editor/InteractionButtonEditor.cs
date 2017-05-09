using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(InteractionButton), editorForChildClasses: true)]
  public class InteractionButtonEditor : InteractionBehaviourEditor {

    public override void OnInspectorGUI() {
      specifyConditionalDrawing(() => false, "graspedMovementType");

      InteractionButton button = target as InteractionButton;

      EditorGUILayout.BeginHorizontal();
      if (button.transform.localRotation != Quaternion.identity) {
        EditorGUILayout.HelpBox("It looks like this button's local rotation is non-zero; would you like to add a base transform so it depresses along its z-axis?", MessageType.Warning);
        if (GUILayout.Button("Add Button\nBase Transform")) {

          GameObject buttonBaseTransform = new GameObject(button.gameObject.name + " Base");
          Undo.RegisterCreatedObjectUndo(buttonBaseTransform, "Created Button Base for "+ button.gameObject.name);
          Undo.SetTransformParent(buttonBaseTransform.transform, button.transform.parent, "Child "+ button.gameObject.name+ "'s Base to " + button.gameObject.name + "'s Parent");

          Undo.RecordObject(buttonBaseTransform, "Set "+target.gameObject.name+"'s Base's Transform's Properties");
          buttonBaseTransform.transform.localPosition = button.transform.localPosition;
          buttonBaseTransform.transform.localRotation = button.transform.localRotation;
          buttonBaseTransform.transform.localScale = button.transform.localScale;

          Undo.SetTransformParent(button.transform, buttonBaseTransform.transform, "Child " + button.gameObject.name + " to its Base");
        }
      }
      EditorGUILayout.EndHorizontal();

      Rigidbody currentBody = button.GetComponent<Rigidbody>();
      RigidbodyConstraints constraints = currentBody.constraints;

      EditorGUILayout.BeginHorizontal();
      if (constraints != RigidbodyConstraints.FreezeRotation) {
        EditorGUILayout.HelpBox("It looks like this button can freely rotate around one or more axes; would you like to constrain its rotation?", MessageType.Warning);
        if (GUILayout.Button("Freeze\nRotation")) {
          Undo.RecordObject(currentBody, "Set " + target.gameObject.name + "'s Rigidbody's Rotation Constraints to be frozen");
          currentBody.constraints = RigidbodyConstraints.FreezeRotation;
        }
      }
      EditorGUILayout.EndHorizontal();

      base.OnInspectorGUI();
    }
  }
}
