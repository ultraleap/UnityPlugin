/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using UnityTest;
using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Leap.Unity.Attributes;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticTest : TestComponent {
    public static SadisticTest current;

    [Header("Test Settings")]
    [Disable]
    public InteractionTestRecording recording;

    [Disable]
    [EnumFlags]
    public InteractionCallback callback;

    [Disable]
    [EnumFlags]
    public InteractionCallback expectedCallbacks;

    [Disable]
    [EnumFlags]
    public InteractionCallback forbiddenCallbacks;

    [Disable]
    public SadisticAction action;

    [Disable]
    public float actionDelay;

    [Disable]
    public int activationDepth;

    [Disable]
    public float scale;

    [Disable]
    public bool contactEnabled;

    [Disable]
    public bool graspEnabled;

    protected InteractionManager _manager;
    protected InteractionTestProvider _provider;
    protected SadisticTestManager _testManager;
    protected InteractionTestRunner _testRunner;

    protected InteractionCallback allCallbacksRecieved;

    void OnEnable() {
      _testManager = GetComponentInParent<SadisticTestManager>();
      _testRunner = FindObjectOfType<InteractionTestRunner>();

      _testRunner.SpawnObjects(scale);

      _manager = FindObjectOfType<InteractionManager>();
      _provider = FindObjectOfType<InteractionTestProvider>();

      _manager.MaxActivationDepth = activationDepth;
      _manager.ContactEnabled = contactEnabled;
      _manager.GraspingEnabled = graspEnabled;
      _manager.UpdateSceneInfo();

      current = this;
      allCallbacksRecieved = 0;

      _provider.recording = recording;

      if (_testManager.spawnObjectTime == SadisticTestManager.SpawnObjectsTime.AtStart) {
        StartCoroutine(spawnObjectsCoroutine());
      } else {
        StartCoroutine(waitForHandCoroutine());
      }

      _provider.Play();

      Assert.raiseExceptions = true;
    }

    public void ReportCallback(InteractionCallback callback) {
      if ((callback & forbiddenCallbacks) != 0) {
        IntegrationTest.Fail("Recieved a forbidden callback " + callback);
        return;
      }

      allCallbacksRecieved |= callback;
    }

    IEnumerator waitForHandCoroutine() {
      while (true) {
        Frame frame = _provider.CurrentFrame;
        if (frame != null && frame.Hands.Count != 0) {
          break;
        }
        yield return null;
      }

      StartCoroutine(spawnObjectsCoroutine());
    }

    IEnumerator spawnObjectsCoroutine() {
      if (_testManager.spawnObjectDelay > 0) {
        yield return new WaitForSeconds(_testManager.spawnObjectDelay);
      }
      _provider.SpawnShapes();
    }

    void Update() {
      _manager.Validate();

      //If we reach the end of the recording, we pass!
      if (!_provider.IsPlaying) {
        _provider.DestroyShapes();

        if ((allCallbacksRecieved & expectedCallbacks) == expectedCallbacks) {
          IntegrationTest.Pass();
          return;
        }

        Debug.LogError(getEnumMessage("Expected callbacks: " + expectedCallbacks, expectedCallbacks));
        Debug.LogError(getEnumMessage("Recieved callbacks: " + allCallbacksRecieved, allCallbacksRecieved));

        IntegrationTest.Fail("Could not find an interaction behaviour that recieved all expected callbacks");
      }
#if UNITY_EDITOR
      else {
        // Show Gizmos for InteractionBrushBone.
        InteractionBrushBone[] bb = FindObjectsOfType(typeof(InteractionBrushBone)) as InteractionBrushBone[];
        GameObject[] objs = new GameObject[bb.Length];
        for (int i = 0; i < bb.Length; i++) {
          objs[i] = bb[i].gameObject;
        }
        Selection.objects = objs;
      }
#endif
    }

    private string getEnumMessage(string message, InteractionCallback values) {
      var callbackType = typeof(InteractionCallback);
      int[] callbackValues = (int[])Enum.GetValues(callbackType);
      string[] callbackNames = Enum.GetNames(callbackType);

      for (int i = 0; i < callbackValues.Length; i++) {
        if ((callbackValues[i] & (int)values) != 0) {
          message += "\n" + callbackNames[i];
        }
      }

      return message;
    }
  }

  public enum SadisticAction {
    DisableComponent = 0x0001,
    DestroyComponent = 0x0002,
    DestroyComponentImmediately = 0x0004,
    DisableGameObject = 0x0008,
    DestroyGameObject = 0x0010,
    DestroyGameObjectImmediately = 0x0020,
    ForceGrab = 0x0040,
    ForceRelease = 0x0080,
    DisableGrasping = 0x0100,
    DisableContact = 0x0200
  }

  public enum InteractionCallback {
    OnRegister = 0x0001,
    OnUnregister = 0x0002,
    OnCreateInstance = 0x0004,
    OnDestroyInstance = 0x0008,
    OnGrasp = 0x0010,
    OnRelease = 0x0020,
    OnSuspend = 0x0040,
    OnResume = 0x0080,
    AfterDelay = 0x0100,
    RecieveVelocityResults = 0x0200,
  }
}
