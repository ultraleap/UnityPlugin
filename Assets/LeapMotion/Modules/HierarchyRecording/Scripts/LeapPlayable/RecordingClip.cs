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
