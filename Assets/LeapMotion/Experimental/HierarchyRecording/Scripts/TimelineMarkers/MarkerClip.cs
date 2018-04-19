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
