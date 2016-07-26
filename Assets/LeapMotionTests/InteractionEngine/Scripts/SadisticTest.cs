using UnityEngine;
using UnityTest;

namespace Leap.Unity.Interaction.Testing {

  public class SadisticTest : TestComponent {

    [Header("Test Settings")]
    public InteractionTestRecording recording;
    public SadisticInteractionBehaviour.SadisticDef sadisticDefinition;

    private InteractionTestProvider _provider;

    void Start() {
      _provider = FindObjectOfType<InteractionTestProvider>();
      _provider.recording = recording;
      _provider.Play();
    }

    void Update() {
      //If we reach the end of the recording, we pass!
      if (!_provider.IsPlaying) {
        IntegrationTest.Pass();
      }
    }
  }
}
