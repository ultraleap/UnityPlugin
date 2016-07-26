using UnityEngine;
using UnityEngine.Assertions;
using UnityTest;
using System;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticTest : TestComponent {

    [Header("Test Settings")]
    public InteractionTestRecording recording;
    public SadisticInteractionBehaviour.SadisticDef sadisticDefinition;

    private InteractionManager _manager;
    private InteractionTestProvider _provider;

    void Start() {
      _manager = FindObjectOfType<InteractionManager>();
      _provider = FindObjectOfType<InteractionTestProvider>();

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
        IntegrationTest.Pass();
      }
    }
  }
}
