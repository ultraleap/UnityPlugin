using UnityEngine;
using Leap.Unity.Playback;

namespace Leap.Unity.Interaction.Testing {

  public class InteractionTestRecorder : PlaybackRecorder {
    public override void StartRecording() {
      var testRecording = ScriptableObject.CreateInstance<InteractionTestRecording>();
      testRecording.CaptureCurrentShapes();
      _currentRecording = testRecording;
    }
  }
}
