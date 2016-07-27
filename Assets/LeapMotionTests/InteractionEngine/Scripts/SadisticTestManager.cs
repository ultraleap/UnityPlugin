using UnityEngine;
using UnityEditor;
using UnityTest;
using System;
using Leap.Unity.Attributes;

namespace Leap.Unity.Interaction.Testing {

  [ExecuteInEditMode]
  public class SadisticTestManager : TestComponent {

    [Header("Sadistic Settings")]
    [SerializeField]
    private InteractionTestRecording[] _recordings;

    [EnumFlags]
    [SerializeField]
    private SadisticInteractionBehaviour.Callback _callbacks;

    [EnumFlags]
    [SerializeField]
    private SadisticInteractionBehaviour.SadisticAction _actions;

    [EnumFlags]
    [SerializeField]
    private SadisticInteractionBehaviour.Callback _expectedCallbacks;

    [SerializeField]
    private float _actionDelay = 0;

    [Header("Spawn Settings")]
    [SerializeField]
    private SpawnObjectsTime _spawnObjectTime = SpawnObjectsTime.AtStart;

    [MinValue(0)]
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

    public override void OnValidate() {
      base.OnValidate();

      if (!Application.isPlaying) {
        //_update = true;
        //gameObject.SetActive(true);
      }
    }

    void Update() {
      if (_update) {
        updateChildrenTests();
        _update = false;
      }
    }

    [ContextMenu("Update tests")]
    private void updateChildrenTests() {
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
