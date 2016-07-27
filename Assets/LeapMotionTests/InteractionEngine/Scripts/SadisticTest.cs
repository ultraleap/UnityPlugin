using UnityEngine;
using UnityEngine.Assertions;
using UnityTest;
using System;
using System.Collections;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticTest : TestComponent {
    public static SadisticInteractionBehaviour.SadisticDef currentDefinition;
    public static SadisticInteractionBehaviour.Callback allCallbacksRecieved;

    [Header("Test Settings")]
    public InteractionTestRecording recording;
    public SadisticInteractionBehaviour.SadisticDef sadisticDefinition;

    private InteractionManager _manager;
    private InteractionTestProvider _provider;
    private SadisticTestManager _testManager;

    void OnEnable() {
      _manager = FindObjectOfType<InteractionManager>();
      _provider = FindObjectOfType<InteractionTestProvider>();
      _testManager = GetComponentInParent<SadisticTestManager>();

      currentDefinition = sadisticDefinition;
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

        if ((allCallbacksRecieved & sadisticDefinition.expectedCallbacks) == sadisticDefinition.expectedCallbacks) {
          IntegrationTest.Pass();
          return;
        }

        var callbackType = typeof(SadisticInteractionBehaviour.Callback);
        int[] callbackValues = (int[])Enum.GetValues(callbackType);
        string[] callbackNames = Enum.GetNames(callbackType);

        string errorMessage = "Expected callbacks:";
        for (int i = 0; i < callbackValues.Length; i++) {
          if ((callbackValues[i] & (int)sadisticDefinition.expectedCallbacks) != 0) {
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
}
