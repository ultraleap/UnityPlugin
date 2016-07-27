using UnityEngine;
using UnityEditor;
using UnityTest;
using System;
using Leap.Unity.Attributes;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticTestManager : TestComponent {
    [Header("Test Definition")]
    [Tooltip("The recordings to be used in the tests.")]
    [SerializeField]
    private InteractionTestRecording[] _recordings;

    [EnumFlags]
    [Tooltip("The callbacks to be used as triggers for actions.")]
    [SerializeField]
    private InteractionCallback _callbacks;

    [EnumFlags]
    [Tooltip("The actions to be dispatched when a callback is triggered.")]
    [SerializeField]
    private SadisticAction _actions;

    [Tooltip("How long after start should the AfterDelay callback be dispatched.")]
    [SerializeField]
    private float _actionDelay = 0;

    [Header("Test Conditions")]
    [EnumFlags]
    [Tooltip("If any of these callbacks has not been dispatched by the time the test has finished, the test will fail.")]
    [SerializeField]
    private InteractionCallback _expectedCallbacks;

    [EnumFlags]
    [Tooltip("If any of these callbacks is dispatched, the test will fail.")]
    [SerializeField]
    private InteractionCallback _forbiddenCallbacks;

    [Header("Spawn Settings")]
    [Tooltip("Under what condition should the objects be spawned")]
    [SerializeField]
    private SpawnObjectsTime _spawnObjectTime = SpawnObjectsTime.AtStart;

    [MinValue(0)]
    [Tooltip("Once the spawn condition is met, how long before the objects are spawned.")]
    [Units("Seconds")]
    [SerializeField]
    private float _spawnObjectDelay = 0;

    public SpawnObjectsTime spawnObjectTime {
      get {
        return _spawnObjectTime;
      }
    }

    public float spawnObjectDelay {
      get {
        return _spawnObjectDelay;
      }
    }

    [ContextMenu("Update tests")]
    public void UpdateChildrenTests() {
      Transform[] transforms = GetComponentsInChildren<Transform>(true);
      foreach (Transform child in transforms) {
        if (child != transform) {
          DestroyImmediate(child.gameObject);
        }
      }

      var actionType = typeof(SadisticAction);
      var callbackType = typeof(InteractionCallback);

      int[] actionValues = (int[])Enum.GetValues(actionType);
      int[] callbackValues = (int[])Enum.GetValues(callbackType);
      string[] actionNames = Enum.GetNames(actionType);
      string[] callbackNames = Enum.GetNames(callbackType);

      if (_callbacks == 0 || _actions == 0) {
        createSubTest(ObjectNames.NicifyVariableName(name), 0, 0);
      } else {
        for (int i = actionValues.Length; i-- != 0;) {
          var actionValue = actionValues[i];
          if (((int)_actions & actionValue) != actionValue) continue;

          for (int j = callbackValues.Length; j-- != 0;) {
            var callbackValue = callbackValues[j];
            if (((int)_callbacks & callbackValue) != callbackValue) continue;

            string niceName = ObjectNames.NicifyVariableName(callbackNames[j]) +
                              " " +
                              ObjectNames.NicifyVariableName(actionNames[i]);
            createSubTest(niceName, callbackValue, actionValue);
          }
        }
      }
    }

    private void createSubTest(string name, int callbackValue, int actionValue) {
      for (int i = 0; i < _recordings.Length; i++) {
        GameObject testObj = new GameObject(name);
        testObj.transform.parent = transform;

        var test = testObj.AddComponent<SadisticTest>();
        test.timeout = timeout;

        test.recording = _recordings[i];
        test.callback = (InteractionCallback)callbackValue;
        test.expectedCallbacks = _expectedCallbacks;
        test.forbiddenCallbacks = _forbiddenCallbacks;
        test.action = (SadisticAction)actionValue;
        test.actionDelay = _actionDelay;

        EditorUtility.SetDirty(test.gameObject);
      }
    }

    public enum SpawnObjectsTime {
      AtStart,
      UponFirstHand
    }
  }
}
