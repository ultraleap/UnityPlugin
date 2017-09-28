/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
