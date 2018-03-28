/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  [Serializable]
  public class RecordingClip : PlayableAsset, ITimelineClipAsset {

    public LeapRecording recording;

    public ClipCaps clipCaps {
      get {
        return ClipCaps.Extrapolation | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;
      }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
      var playable = ScriptPlayable<RecordingBehaviour>.Create(graph);
      playable.GetBehaviour().recording = recording;
      return playable;
    }
  }
}
