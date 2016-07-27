using UnityEngine;
using UnityEngine.Assertions;
using UnityTest;
using System;
using System.Collections;

using Leap.Unity.Attributes;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticTest : TestComponent {
    public static SadisticTest current;
    public static InteractionCallback allCallbacksRecieved;

    [Header("Test Settings")]
    public InteractionTestRecording recording;
    public InteractionCallback callback;
    [EnumFlags]
    public InteractionCallback expectedCallbacks;
    public SadisticAction action;
    public float actionDelay;

    private InteractionManager _manager;
    private InteractionTestProvider _provider;
    private SadisticTestManager _testManager;

    void OnEnable() {
      _manager = FindObjectOfType<InteractionManager>();
      _provider = FindObjectOfType<InteractionTestProvider>();
      _testManager = GetComponentInParent<SadisticTestManager>();

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
      try {
        _manager.Validate();
      } catch (Exception e) {
        Debug.LogException(e);
        IntegrationTest.Fail(e.Message);
      }

      //If we reach the end of the recording, we pass!
      if (!_provider.IsPlaying) {
        _provider.DestroyShapes();

        if ((allCallbacksRecieved & expectedCallbacks) == expectedCallbacks) {
          IntegrationTest.Pass();
          return;
        }

        var callbackType = typeof(InteractionCallback);
        int[] callbackValues = (int[])Enum.GetValues(callbackType);
        string[] callbackNames = Enum.GetNames(callbackType);

        string errorMessage = "Expected callbacks:";
        for (int i = 0; i < callbackValues.Length; i++) {
          if ((callbackValues[i] & (int)expectedCallbacks) != 0) {
            errorMessage += "\n" + callbackNames[i];
          }
        }

        string recievedMessage = "Recieved callbacks:";
        for (int i = 0; i < callbackValues.Length; i++) {
          if ((callbackValues[i] & (int)allCallbacksRecieved) != 0) {
            recievedMessage += "\n" + callbackNames[i];
          }
        }

        Debug.LogError(errorMessage);
        Debug.LogError(recievedMessage);

        IntegrationTest.Fail("Could not find an interaction behaviour that recieved all expected callbacks");
      }
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
    ForceRelease = 0x0080
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
  }
}
