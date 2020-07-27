/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
