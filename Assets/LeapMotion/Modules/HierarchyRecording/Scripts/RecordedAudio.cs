/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Recording {

  [RecordingFriendly]
  public class RecordedAudio : MonoBehaviour {

    public float recordingStartTime;
    public AudioSource target;
    public List<ClipData> data = new List<ClipData>();

    private bool _prevWasPlaying = false;
    private float _prevTime = 0;
    private AudioClip _prevClip = null;

    private void LateUpdate() {
      bool didStartNewClip = false;

      if (target.isPlaying && !_prevWasPlaying) {
        didStartNewClip = true;
      }

      if (target.time < _prevTime && target.isPlaying) {
        didStartNewClip = true;
      }

      if (target.clip != null && target.clip != _prevClip && target.isPlaying) {
        didStartNewClip = true;
      }

      if (didStartNewClip) {
        data.Add(new ClipData() {
          clip = target.clip,
          startTime = Time.time - recordingStartTime,
          pitch = target.pitch,
          volume = target.volume
        });
      }

      _prevWasPlaying = target.isPlaying;
      _prevTime = target.time;
      _prevClip = target.clip;
    }

    [Serializable]
    public class ClipData {
      public AudioClip clip;
      public float startTime;
      public float pitch;
      public float volume;
    }
  }
}
