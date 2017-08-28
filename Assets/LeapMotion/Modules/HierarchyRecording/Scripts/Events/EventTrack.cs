using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  [TrackColor(0.8F, 0.1F, 0.8F)]
  [TrackClipType(typeof(EventClip))]
  public class EventTrack : TrackAsset {

    private PlayableDirector _director = null;

    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
      var eventMixerPlayable = ScriptPlayable<EventPlayableMixerBehaviour>.Create(graph, inputCount);
      eventMixerPlayable.GetBehaviour().director = _director;
      eventMixerPlayable.GetBehaviour().eventTrack = this;

      return eventMixerPlayable;
    }

    public override void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
      base.GatherProperties(director, driver);

      _director = director;
    }

  }

}