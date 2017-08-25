using Leap.Unity.Recording;
using UnityEngine;

public class RecorderBuddy : MonoBehaviour {

  public HierarchyRecorder recorder;

  public float recordingDuration = 1f;
  public KeyCode recordButton = KeyCode.Space;

  private float _curDuration = 0f;

  void Update() {
    if (_curDuration > recordingDuration) {
      recorder.FinishRecording();
    }

      if (Input.GetKeyDown(recordButton)) {
      recorder.BeginRecording();
    }

    if (recorder.isRecording) {
      _curDuration += Time.deltaTime;
    }
  }

}
