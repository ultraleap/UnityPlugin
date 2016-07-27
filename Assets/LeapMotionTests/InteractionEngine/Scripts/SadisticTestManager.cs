using UnityEngine;
using UnityEditor;
using UnityTest;
using System;
using Leap.Unity.Attributes;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticTestManager : TestComponent {
    [Header("Sadistic Settings")]
    [Tooltip("The recordings to be used in the tests.")]
    [SerializeField]
    private InteractionTestRecording[] _recordings;

    [EnumFlags]
    [Tooltip("The callbacks to be used as triggers for actions.")]
    [SerializeField]
    private SadisticInteractionBehaviour.Callback _callbacks;

    [EnumFlags]
    [Tooltip("The actions to be dispatched when a callback is triggered.")]
    [SerializeField]
    private SadisticInteractionBehaviour.SadisticAction _actions;

    [EnumFlags]
    [Tooltip("If any of these callbacks has not been dispatched by the time the test has finished, the test will fail.")]
    [SerializeField]
    private SadisticInteractionBehaviour.Callback _expectedCallbacks;

    [Tooltip("How long after start should the AfterDelay callback be dispatched.")]
    [SerializeField]
    private float _actionDelay = 0;

    [Header("Spawn Settings")]
    [Tooltip("Under what condition should the objects be spawned")]
    [SerializeField]
    private SpawnObjectsTime _spawnObjectTime = SpawnObjectsTime.AtStart;

    [MinValue(0)]
    [Tooltip("Once the spawn condition is met, how long before the objects are spawned.")]
    [Units("Seconds")]
    [SerializeField]
    private float _spawnObjectDelay = 0;

    private bool _update = false;

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

      var actionType = typeof(SadisticInteractionBehaviour.SadisticAction);
      var callbackType = typeof(SadisticInteractionBehaviour.Callback);

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
        test.sadisticDefinition = new SadisticInteractionBehaviour.SadisticDef(
                                         (SadisticInteractionBehaviour.Callback)callbackValue,
                                         _expectedCallbacks,
                                         (SadisticInteractionBehaviour.SadisticAction)actionValue,
                                         _actionDelay);

        test.recording = _recordings[i];
        test.timeout = timeout;
      }
    }

    public enum SpawnObjectsTime {
      AtStart,
      UponFirstHand
    }
  }
}
