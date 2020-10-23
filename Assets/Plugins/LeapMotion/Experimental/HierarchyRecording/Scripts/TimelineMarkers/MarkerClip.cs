/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
