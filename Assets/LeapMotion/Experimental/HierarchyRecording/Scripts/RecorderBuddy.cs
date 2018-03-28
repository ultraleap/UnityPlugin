/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
