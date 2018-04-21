using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Interaction.Internal {
  using UnityObject = UnityEngine.Object;
  using Query;

  public static class InteractionChecks {
    private const string CHECK_KEY = "LeapInteractionEngineCheckKey";

#if UNITY_EDITOR
    [LeapProjectCheck("Interaction Engine", 10)]
    public static bool CheckInteractionSettings() {
      EditorGUILayout.LabelField("Interaction Engine", EditorStyles.boldLabel);

      bool allChecksPassed = true;

      allChecksPassed &= checkForGravity();

      allChecksPassed &= checkForTimeStep();

      allChecksPassed &= checkForRigidHands();

      if (allChecksPassed) {
        EditorGUILayout.HelpBox("All settings are good for the Interaction Engine!", MessageType.Info);
      }

      return true;
    }

    private static bool checkForTimeStep() {
      if (Time.fixedDeltaTime > InteractionPreferences.MAX_TIMESTEP + Mathf.Epsilon) {
        float roundedTimestep = (float)Math.Round(InteractionPreferences.MAX_TIMESTEP, 4);
        EditorGUILayout.HelpBox("Your fixed timestep is " + Time.fixedDeltaTime +
                                ", which is slower than the recommended value " +
                                "of " + roundedTimestep + ".\n\nGo to Edit/Project Settings/Time " +
                                "to change the fixed timestep.", MessageType.Warning);
        return false;
      } else {
        return true;
      }
    }

    private static bool checkForGravity() {
      float magnitude = Physics.gravity.y;
      if (Mathf.Abs(magnitude) > InteractionPreferences.MAX_GRAVITY_MAGNITUDE) {
        EditorGUILayout.HelpBox("Your gravity magnitude is " + magnitude
                              + " which is stronger than the recommended value "
                              + "of -4.905!\n\nGo to Edit/Project Settings/Physics "
                              + "to change the magnitude.", MessageType.Warning);
        return false;
      } else {
        return true;
      }
    }

    private static bool checkForRigidHands() {
      var rigidHandObjects = UnityObject.FindObjectsOfType<RigidHand>().Query().Select(x => x.gameObject).ToArray();
      if (rigidHandObjects.Length != 0 && UnityObject.FindObjectOfType<InteractionManager>() != null) {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.HelpBox("Rigid Hands AND an Interaction Manager are present in your scene. " +
                                "Rigid Hands are not compatible with the Interaction Engine and should " +
                                 "never be used in tandem with it. You should remove them " +
                                 "from your scene.", MessageType.Warning);
        if (GUILayout.Button(new GUIContent("Select Rigid Hands", "Select RigidHand objects in the current scene."), GUILayout.ExpandHeight(true), GUILayout.MaxHeight(40F))) {
          Selection.objects = rigidHandObjects;
        }

        EditorGUILayout.EndHorizontal();
        return false;
      } else {
        return true;
      }
    }
#endif
  }
}
