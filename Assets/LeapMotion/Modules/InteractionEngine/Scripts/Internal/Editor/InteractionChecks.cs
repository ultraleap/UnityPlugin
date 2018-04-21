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

    private const string CHECK_KEY = "LeapInteractionEngineCheckKey";
    private const string SHOULD_LAUNCH_FOR_IE = "LeapWindowPanelShouldLaunchForIE";

    [InitializeOnLoadMethod]
    private static void init() {
      EditorApplication.delayCall += () => {
        if (EditorPrefs.GetBool(SHOULD_LAUNCH_FOR_IE, defaultValue: true)) {
          EditorPrefs.SetBool(SHOULD_LAUNCH_FOR_IE, false);
          LeapUnityWindow.Init();
        }
      };
    }

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
      if (Time.fixedDeltaTime > MAX_TIMESTEP + Mathf.Epsilon) {
        float roundedTimestep = (float)Math.Round(MAX_TIMESTEP, 4);
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
      if (Mathf.Abs(magnitude) > MAX_GRAVITY_MAGNITUDE) {
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
  }
}
