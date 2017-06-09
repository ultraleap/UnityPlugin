using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Interaction.Internal {

#if UNITY_EDITOR
  public static class InteractionPreferences {
    public const float MAX_GRAVITY_MAGNITUDE = 4.905f;
    public const float MAX_TIMESTEP = 1.0f / 90.0f;

    public const string PROMPT_FOR_GRAVITY_KEY = "InteractionEngine_ShouldPrompForGravity";
    public const string PROMPT_FOR_PHYSICS_TIMESTEP = "InteractionEngine_ShouldPrompForTimestep";

    private static GUIContent _gravityPrompContent;
    private static GUIContent _timestepPrompContent;

    static InteractionPreferences() {
      _gravityPrompContent = new GUIContent("Validate Gravity Magnitude", "Should prompt the user if the magnitude of the gravity vector is higher than the recommended amount?");
      _timestepPrompContent = new GUIContent("Validate Physics Timestep", "Should prompt the user if the physics timestep is larger then the recommended value?");
    }

    public static bool shouldPrompForGravity {
      get {
        return EditorPrefs.GetBool(PROMPT_FOR_GRAVITY_KEY, defaultValue: true);
      }
      set {
        EditorPrefs.SetBool(PROMPT_FOR_GRAVITY_KEY, value);
      }
    }

    public static bool shouldPrompForPhysicsTimestep {
      get {
        return EditorPrefs.GetBool(PROMPT_FOR_PHYSICS_TIMESTEP, defaultValue: true);
      }
      set {
        EditorPrefs.SetBool(PROMPT_FOR_PHYSICS_TIMESTEP, value);
      }
    }

    [PreferenceItem("Leap Interaction")]
    private static void preferencesGUI() {
      bool newGravityValue = EditorGUILayout.Toggle(_gravityPrompContent, shouldPrompForGravity);
      if (newGravityValue != shouldPrompForGravity) {
        shouldPrompForGravity = newGravityValue;
      }

      bool newTimestepValue = EditorGUILayout.Toggle(_timestepPrompContent, shouldPrompForPhysicsTimestep);
      if (newTimestepValue != shouldPrompForPhysicsTimestep) {
        shouldPrompForPhysicsTimestep = newTimestepValue;
      }
    }
  }
#endif
}
