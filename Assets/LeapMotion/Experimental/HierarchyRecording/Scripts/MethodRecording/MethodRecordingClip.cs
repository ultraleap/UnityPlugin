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
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  public class MethodRecordingClip : PlayableAsset, ITimelineClipAsset {

    public bool invokeAtEditTime = false;

    public ClipCaps clipCaps {
      get {
        return ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;
      }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
      var playable = ScriptPlayable<MethodRecordingPlayable>.Create(graph);
      playable.GetBehaviour().invokeAtEditTime = invokeAtEditTime;
      return playable;
    }
  }
}
