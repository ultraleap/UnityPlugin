using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(InteractionButton), editorForChildClasses: true)]
  public class InteractionButtonEditor : InteractionBehaviourEditor {

    public override void OnInspectorGUI() {
      EditorGUILayout.BeginHorizontal();

      InteractionButton button = target as InteractionButton;

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
      base.OnInspectorGUI();
    }
  }
}
