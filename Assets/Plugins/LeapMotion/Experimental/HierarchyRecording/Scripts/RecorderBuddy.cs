/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Recording;
using UnityEngine;

public class RecorderBuddy : MonoBehaviour {

  public HierarchyRecorder recorder;

  public float recordingDuration = 1f;
  public KeyCode recordButton = KeyCode.Space;

  protected float _curDuration = 0f;

#if UNITY_EDITOR
  void Reset() {
    if (recorder == null) {
      recorder = GetComponent<HierarchyRecorder>();
    }
  }

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
#endif
}
