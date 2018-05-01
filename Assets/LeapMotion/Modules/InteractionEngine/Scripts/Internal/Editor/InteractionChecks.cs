/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Interaction.Internal {

  using UnityObject = UnityEngine.Object;
  using Query;

  public static class InteractionChecks {
    public const float MAX_GRAVITY_MAGNITUDE = 4.905f;

#if UNITY_ANDROID
    public const float MAX_TIMESTEP = 1.0f / 60.0f;
#else
    public const float MAX_TIMESTEP = 1.0f / 90.0f;
#endif
    
    private const string IGNORE_GRAVITY_CHECK_KEY = "LeapIEIgnoreGravityCheck";
    private const string IGNORE_TIMESTEP_CHECK_KEY = "LeapIEIgnoreTimestepCheck";
    private const string IGNORE_RIGID_HANDS_CHECK_KEY = "LeapIEIgnoreRigidHandsCheck";

    private const string SHOULD_LAUNCH_FOR_IE = "LeapWindowPanelShouldLaunchForIE";

    [InitializeOnLoadMethod]
    private static void init() {
      EditorApplication.delayCall += () => {
        if (EditorPrefs.GetBool(SHOULD_LAUNCH_FOR_IE, defaultValue: true)) {
          EditorPrefs.SetBool(SHOULD_LAUNCH_FOR_IE, false);
          LeapUnityWindow.Init();
        }
      };

      EditorApplication.playModeStateChanged -= onPlayModeStateChanged;
      EditorApplication.playModeStateChanged += onPlayModeStateChanged;
    }

    private static void onPlayModeStateChanged(PlayModeStateChange stateChange) {
      if (stateChange == PlayModeStateChange.EnteredPlayMode) {
        bool allChecksPassed = runAllChecks_NoGUI();

        if (!allChecksPassed) {
          EditorApplication.delayCall += LeapUnityWindow.Init;
        }
      }
    }

    [LeapProjectCheck("Interaction Engine", 10)]
    public static bool CheckInteractionSettings() {
      EditorGUILayout.LabelField("Interaction Engine", EditorStyles.boldLabel);

      bool allChecksPassed = runAllChecks_WithGUI();

      EditorGUILayout.Space();

      if (allChecksPassed) {
        EditorGUILayout.HelpBox("All settings are good for the Interaction Engine!",
          MessageType.Info);
      }

      return true;
    }

    #region Run All Checks

    private static bool runAllChecks_NoGUI() {
      bool allChecksPassed = true;
      
      allChecksPassed &= checkGravity();

      allChecksPassed &= checkTimeStep();

      allChecksPassed &= checkRigidHands();

      return allChecksPassed;
    }

    private static bool runAllChecks_WithGUI() {
      bool allChecksPassed = true;
      
      allChecksPassed &= checkGravityAndDrawGUI();

      allChecksPassed &= checkTimeStepAndDrawGUI();

      allChecksPassed &= checkRigidHandsAndDrawGUI();

      if (!allChecksPassed && Application.isPlaying) {
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("Editor is currently in play-mode, so any project setting "
          + "changes will be reverted upon returning to edit-mode. You should return to "
          + "edit-mode if you want auto-fixes to stick.", MessageType.Warning);
      }

      return allChecksPassed;
    }

    #endregion

    #region Check Gravity

    private static bool checkGravity() {
      if (LeapProjectChecks.CheckIgnoredKey(IGNORE_GRAVITY_CHECK_KEY)) {
        return true;
      }
      
      if (Mathf.Abs(Physics.gravity.y) > MAX_GRAVITY_MAGNITUDE) {
        return false;
      }
      else {
        return true;
      }
    }

    private static bool checkGravityAndDrawGUI() {
      var gravityOK = checkGravity();

      if (!gravityOK) {

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope()) {
          EditorGUILayout.HelpBox("Your gravity magnitude is " + Physics.gravity.y
                              + " which is stronger than the recommended value "
                              + "of -4.905!\n\nGo to Edit/Project Settings/Physics "
                              + "to change the magnitude.", MessageType.Warning);

          if (GUILayout.Button("Auto-fix")) {
            Physics.gravity = Vector3.down * MAX_GRAVITY_MAGNITUDE;
          }

          if (GUILayout.Button("Ignore")) {
            LeapProjectChecks.SetIgnoredKey(IGNORE_GRAVITY_CHECK_KEY, ignore: true);
          }
        }
      }

      return gravityOK;
    }

    #endregion

    #region Check Timestep

    private static bool checkTimeStep() {
      if (LeapProjectChecks.CheckIgnoredKey(IGNORE_TIMESTEP_CHECK_KEY)) {
        return true;
      }

      if (Time.fixedDeltaTime > MAX_TIMESTEP + Mathf.Epsilon) {
        return false;
      }
      else {
        return true;
      }
    }

    private static bool checkTimeStepAndDrawGUI() {
      var timeStepOK = checkTimeStep();

      if (!timeStepOK) {
        float roundedTimestep = (float)Math.Round(MAX_TIMESTEP, 4);

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope()) {
          EditorGUILayout.HelpBox(
            "Your fixed timestep is " + Time.fixedDeltaTime +
            ", which is slower than the recommended value " +
            "of " + roundedTimestep + ".\n\nGo to Edit/Project Settings/Time " +
            "to change the fixed timestep.", MessageType.Warning);

          if (GUILayout.Button("Auto-fix")) {
            Time.fixedDeltaTime = roundedTimestep;
          }

          if (GUILayout.Button("Ignore")) {
            LeapProjectChecks.SetIgnoredKey(IGNORE_TIMESTEP_CHECK_KEY, ignore: true);
          }
        }
      }

      return timeStepOK;
    }

    #endregion

    #region Check Rigid Hands

    private static bool checkRigidHands() {
      var unusedRigidHandObjects = (GameObject[])null;
      return checkRigidHands(out unusedRigidHandObjects);
    }

    private static bool checkRigidHands(out GameObject[] rigidHandObjects) {
      if (LeapProjectChecks.CheckIgnoredKey(IGNORE_RIGID_HANDS_CHECK_KEY)) {
        rigidHandObjects = null;
        return true;
      }

      rigidHandObjects = UnityObject.FindObjectsOfType<RigidHand>().Query()
        .Select(x => x.gameObject).ToArray();
      if (rigidHandObjects.Length != 0
            && UnityObject.FindObjectOfType<InteractionManager>() != null) {
        return false;
      }
      else {
        return true;
      }
    }

    private static bool checkRigidHandsAndDrawGUI() {
      var rigidHandObjects = (GameObject[])null;
      var rigidHandsOK = checkRigidHands(out rigidHandObjects);

      if (!rigidHandsOK) {

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope()) {
          EditorGUILayout.HelpBox(
            "Rigid Hands AND an Interaction Manager are present in your scene. " +
            "Rigid Hands are not compatible with the Interaction Engine and should " +
             "never be used in tandem with it. You should remove them " +
             "from your scene.", MessageType.Warning);

          if (GUILayout.Button(new GUIContent("Select Rigid Hands",
                "Select RigidHand objects in the current scene."),
                GUILayout.ExpandHeight(true), GUILayout.MaxHeight(40F))) {
            Selection.objects = rigidHandObjects;
          }

          if (GUILayout.Button("Ignore")) {
            LeapProjectChecks.SetIgnoredKey(IGNORE_RIGID_HANDS_CHECK_KEY, ignore: true);
          }
        }
      }

      return rigidHandsOK;
    }

    #endregion

  }
}
