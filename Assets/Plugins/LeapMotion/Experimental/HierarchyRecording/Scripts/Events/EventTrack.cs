/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  [TrackColor(0.8F, 0.1F, 0.8F)]
  [TrackClipType(typeof(EventClip))]
  public class EventTrack : TrackAsset {

    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
      var eventMixerPlayable = ScriptPlayable<EventPlayableMixerBehaviour>.Create(graph, inputCount);
      
      eventMixerPlayable.GetBehaviour().eventTrack = this;

      return eventMixerPlayable;
    }

  }

}
