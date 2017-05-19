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
using UnityTest;
using System;
using Leap.Unity.Attributes;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticTestManager : TestComponent {
    [Header("Test Definition")]
    [Tooltip("The recordings to be used in the tests.")]
    [SerializeField]
    protected InteractionTestRecording[] _recordings;

    [EnumFlags]
    [Tooltip("The callbacks to be used as triggers for actions.")]
    [SerializeField]
    protected InteractionCallback _callbacks;

    [EnumFlags]
    [Tooltip("The actions to be dispatched when a callback is triggered.")]
    [SerializeField]
    protected SadisticAction _actions;

    [Tooltip("How long after start should the AfterDelay callback be dispatched.")]
    [SerializeField]
    protected float _actionDelay = 0;

    [SerializeField]
    protected int[] _activationDepths = { 3 };

    [SerializeField]
    protected float[] _scales = { 1 };

    [SerializeField]
    protected bool _contactEnabled = true;

    [SerializeField]
    protected bool _graspEnabled = true;

    [Header("Test Conditions")]
    [EnumFlags]
    [Tooltip("If any of these callbacks has not been dispatched by the time the test has finished, the test will fail.")]
    [SerializeField]
    protected InteractionCallback _expectedCallbacks;

    [EnumFlags]
    [Tooltip("If any of these callbacks is dispatched, the test will fail.")]
    [SerializeField]
    protected InteractionCallback _forbiddenCallbacks;

    [Header("Spawn Settings")]
    [Tooltip("Under what condition should the objects be spawned")]
    [SerializeField]
    protected SpawnObjectsTime _spawnObjectTime = SpawnObjectsTime.AtStart;

    [MinValue(0)]
    [Tooltip("Once the spawn condition is met, how long before the objects are spawned.")]
    [Units("Seconds")]
    [SerializeField]
    protected float _spawnObjectDelay = 0;

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

#if UNITY_EDITOR
    [ContextMenu("Update tests")]
    public void UpdateChildrenTests() {
      Transform[] transforms = GetComponentsInChildren<Transform>(true);
      foreach (Transform child in transforms) {
        if (child != transform) {
          Undo.DestroyObjectImmediate(child.gameObject);
        }
      }

      var actionType = typeof(SadisticAction);
      var callbackType = typeof(InteractionCallback);

      int[] actionValues = (int[])Enum.GetValues(actionType);
      int[] callbackValues = (int[])Enum.GetValues(callbackType);
      string[] actionNames = Enum.GetNames(actionType);
      string[] callbackNames = Enum.GetNames(callbackType);

      foreach (int activationDepth in _activationDepths) {
        foreach (float scale in _scales) {
          if (_callbacks == 0 || _actions == 0) {
            createSubTest(ObjectNames.NicifyVariableName(name), 0, 0, activationDepth, scale);
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
                createSubTest(niceName, callbackValue, actionValue, activationDepth, scale);
              }
            }
          }
        }
      }
    }

    private void createSubTest(string name, int callbackValue, int actionValue, int activationDepth, float scale) {
      for (int i = 0; i < _recordings.Length; i++) {
        GameObject testObj = new GameObject(name);
        if (scale != 1) {
          testObj.name = testObj.name + " " + scale + "x";
        }

        Undo.RegisterCreatedObjectUndo(testObj, "Created automatic test");

        Undo.RecordObject(testObj.transform, "Reparenting automatic test object");
        testObj.transform.parent = transform;

        var test = testObj.AddComponent<SadisticTest>();
        Undo.RegisterCreatedObjectUndo(test, "Created autoamtic test component");

        Undo.RecordObject(test, "Set test settings");

        test.timeout = timeout;
        test.ignored = ignored;
        test.succeedAfterAllAssertionsAreExecuted = succeedAfterAllAssertionsAreExecuted;
        test.expectException = expectException;
        test.expectedExceptionList = expectedExceptionList;
        test.succeedWhenExceptionIsThrown = succeedWhenExceptionIsThrown;
        test.includedPlatforms = includedPlatforms;
        test.platformsToIgnore = platformsToIgnore;

        test.recording = _recordings[i];
        test.callback = (InteractionCallback)callbackValue;
        test.expectedCallbacks = _expectedCallbacks;
        test.forbiddenCallbacks = _forbiddenCallbacks;
        test.action = (SadisticAction)actionValue;
        test.actionDelay = _actionDelay;
        test.activationDepth = activationDepth;
        test.scale = scale;
        test.contactEnabled = _contactEnabled;
        test.graspEnabled = _graspEnabled;
      }
    }
#endif

    public enum SpawnObjectsTime {
      AtStart,
      UponFirstHand
    }
  }
}
