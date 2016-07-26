using UnityEngine;
using UnityEngine.Assertions;
using UnityTest;
using System;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticTest : TestComponent {
    public static SadisticInteractionBehaviour.SadisticDef currentDefinition;

    [Header("Test Settings")]
    public InteractionTestRecording recording;
    public SadisticInteractionBehaviour.SadisticDef sadisticDefinition;

    private InteractionManager _manager;
    private InteractionTestProvider _provider;

    void Start() {
      _manager = FindObjectOfType<InteractionManager>();
      _provider = FindObjectOfType<InteractionTestProvider>();

      currentDefinition = sadisticDefinition;

      _provider.recording = recording;
      _provider.Play();

      Assert.raiseExceptions = true;
    }

    void Update() {
      try {
        _manager.Validate();
      } catch (Exception e) {
        IntegrationTest.Fail(e.Message);
      }

      //If we reach the end of the recording, we pass!
      if (!_provider.IsPlaying) {
        foreach (var obj in FindObjectsOfType<SadisticInteractionBehaviour>()) {
          if ((obj.allCallbacksRecieved & sadisticDefinition.expectedCallbacks) == sadisticDefinition.expectedCallbacks) {
            IntegrationTest.Pass();
          }
        }

        Debug.LogError("Expected callback with code " + sadisticDefinition.expectedCallbacks);
        foreach (var obj in FindObjectsOfType<SadisticInteractionBehaviour>()) {
          Debug.LogError("Found object with callback code " + obj.allCallbacksRecieved);
        }

        IntegrationTest.Fail("Could not find an interaction behaviour that recieved all expected callbacks");
      }
    }
  }
}
