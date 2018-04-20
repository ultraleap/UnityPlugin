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

  public class MarkerClip : PlayableAsset, ITimelineClipAsset {

    public string markerName;

    public ClipCaps clipCaps {
      get {
        return ClipCaps.None;
      }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
      return ScriptPlayable<MarkerPlayable>.Create(graph);
    }
  }

  public class MarkerPlayable : PlayableBehaviour { }
}
