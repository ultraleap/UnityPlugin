using UnityEngine;
using UnityEngine.Assertions;
using UnityTest;
using System;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticTest : TestComponent {
    public static SadisticInteractionBehaviour.SadisticDef currentDefinition;
    public static SadisticInteractionBehaviour.Callback allCallbacksRecieved;

    [Header("Test Settings")]
    public InteractionTestRecording recording;
    public SadisticInteractionBehaviour.SadisticDef sadisticDefinition;

    private InteractionManager _manager;
    private InteractionTestProvider _provider;

    void OnEnable() {
      _manager = FindObjectOfType<InteractionManager>();
      _provider = FindObjectOfType<InteractionTestProvider>();

      currentDefinition = sadisticDefinition;
      allCallbacksRecieved = 0;

      _provider.recording = recording;
      _provider.Play();

      Assert.raiseExceptions = true;
    }

    void OnDisable() {
      _provider.recording = null;
    }

    void Update() {
      try {
        _manager.Validate();
      } catch (Exception e) {
        IntegrationTest.Fail(e.Message);
      }

      //If we reach the end of the recording, we pass!
      if (!_provider.IsPlaying) {
        _provider.recording = null;

        if ((allCallbacksRecieved & sadisticDefinition.expectedCallbacks) == sadisticDefinition.expectedCallbacks) {
          IntegrationTest.Pass();
          return;
        }

        Debug.LogError("Expected callback with code " + sadisticDefinition.expectedCallbacks);
        Debug.LogError("Found object with callback code " + allCallbacksRecieved);

        IntegrationTest.Fail("Could not find an interaction behaviour that recieved all expected callbacks");
      }
    }
  }
}
