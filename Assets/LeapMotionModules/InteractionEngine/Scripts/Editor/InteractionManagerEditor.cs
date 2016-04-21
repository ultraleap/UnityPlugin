using UnityEngine;
using System.Linq;
using UnityEditor;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(InteractionManager), true)]
  public class InteractionManagerEditor : CustomEditorBase {

    private IInteractionBehaviour[] _interactionBehaviours;
    private bool _anyBehavioursUnregistered;

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDecorator("_leapProvider", providerDectorator);

      _interactionBehaviours = FindObjectsOfType<IInteractionBehaviour>();
      for (int i = 0; i < _interactionBehaviours.Length; i++) {
        if (_interactionBehaviours[i].Manager == null) {
          _anyBehavioursUnregistered = true;
          break;
        }
      }
    }

    private void providerDectorator(SerializedProperty prop) {
      if (_anyBehavioursUnregistered) {
        EditorGUILayout.HelpBox("Some Interaction Behaviours do not have their manager assigned!  Do you want to assign them to this manager?", MessageType.Warning);
        if (GUILayout.Button("Assign To This Manager")) {
          for (int i = 0; i < _interactionBehaviours.Length; i++) {
            var behaviour = _interactionBehaviours[i];
            if (behaviour.Manager == null) {
              behaviour.Manager = target as InteractionManager;
              EditorUtility.SetDirty(behaviour);
            }
          }
          _anyBehavioursUnregistered = false;
        }
        GUILayout.Space(EditorGUIUtility.singleLineHeight);
      }
    }

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
