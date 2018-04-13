/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Playables;

namespace Leap.Unity.Recording {

  public class MethodRecordingPlayable : PlayableBehaviour {
    private double _prevTime = double.NaN;

    public bool invokeAtEditTime;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
      if (!Application.isPlaying && !invokeAtEditTime) {
        return;
      }

      var recording = playerData as MethodRecording;
      if (recording == null) {
        return;
      }

      if (recording.mode != MethodRecording.Mode.Playback) {
        recording.EnterPlaybackMode();
      }

      float prevTime = (float)playable.GetPreviousTime();
      float nowTime = (float)playable.GetTime();
      bool didSeek = _prevTime != playable.GetPreviousTime() || nowTime < prevTime;

      if (!didSeek) {
        recording.SweepTime(prevTime, nowTime);
      }

      _prevTime = playable.GetTime();
    }
  }
}
