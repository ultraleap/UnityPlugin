/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Interaction.Internal {

#if UNITY_EDITOR
  public static class InteractionPreferences {
    public const float MAX_GRAVITY_MAGNITUDE = 4.905f;

#if UNITY_ANDROID
    public const float MAX_TIMESTEP = 1.0f / 60.0f;
#else
    public const float MAX_TIMESTEP = 1.0f / 90.0f;
#endif

    public const string PROMPT_FOR_GRAVITY_KEY = "InteractionEngine_ShouldPrompForGravity";
    public const string PROMPT_FOR_PHYSICS_TIMESTEP = "InteractionEngine_ShouldPrompForTimestep";
    public const string CHECK_RIGID_HANDS_KEY = "InteractionEngine_CheckForRigidHands";

    private static GUIContent _gravityPrompContent;
    private static GUIContent _timestepPrompContent;
    private static GUIContent _rigidHandsCheckContent;

    static InteractionPreferences() {
      _gravityPrompContent = new GUIContent("Validate Gravity Magnitude", "Should prompt the user if the magnitude of the gravity vector is higher than the recommended amount?");
      _timestepPrompContent = new GUIContent("Validate Physics Timestep", "Should prompt the user if the physics timestep is larger then the recommended value?");
      _rigidHandsCheckContent = new GUIContent("Warn against Rigid Hands", "Whether to display a warning in the Interaction Manager inspector if Rigid Hands are detected in the scene.");
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

    public static bool shouldCheckForRigidHands {
      get {
        return EditorPrefs.GetBool(CHECK_RIGID_HANDS_KEY, defaultValue: true);
      }
      set {
        EditorPrefs.SetBool(CHECK_RIGID_HANDS_KEY, value);
      }
    }

    [LeapPreferences("Interaction Engine", 10)]
    private static void preferencesGUI() {
      bool newGravityValue = EditorGUILayout.Toggle(_gravityPrompContent, shouldPrompForGravity);
      if (newGravityValue != shouldPrompForGravity) {
        shouldPrompForGravity = newGravityValue;
      }

      bool newTimestepValue = EditorGUILayout.Toggle(_timestepPrompContent, shouldPrompForPhysicsTimestep);
      if (newTimestepValue != shouldPrompForPhysicsTimestep) {
        shouldPrompForPhysicsTimestep = newTimestepValue;
      }

      bool newRigidHandsValue = EditorGUILayout.Toggle(_rigidHandsCheckContent, shouldCheckForRigidHands);
      if (newRigidHandsValue != shouldCheckForRigidHands) {
        shouldCheckForRigidHands = newRigidHandsValue;
      }
    }
  }
#endif
}
