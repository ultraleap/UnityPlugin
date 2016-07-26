using UnityEngine;
using Leap.Unity.Playback;

namespace Leap.Unity.Interaction.Testing {

  public class InteractionTestRecorder : PlaybackRecorder {

    public override void StartRecording() {
      var testRecording = ScriptableObject.CreateInstance<InteractionTestRecording>();
      testRecording.CaptureCurrentShapes();
      _currentRecording = testRecording;
    }

    public override Recording EndRecording() {
      var testRecording = _currentRecording as InteractionTestRecording;
      testRecording.TrimStartOfEmptyFrames(2);
      testRecording.TrimEndOfEmptyFrames(2);

      return base.EndRecording();
    }
  }
}
