using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  [TrackColor(0.2827586f, 1f, 0f)]
  [TrackClipType(typeof(RecordingClip))]
  [TrackBindingType(typeof(LeapPlayableProvider))]
  public class RecordingTrack : TrackAsset {

    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
      return ScriptPlayable<RecordingMixerBehaviour>.Create(graph, inputCount);
    }
  }
}
