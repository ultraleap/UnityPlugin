/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  [RequireComponent(typeof(PlayableDirector))]
  public class MarkerController : MonoBehaviour {

    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private Dictionary<string, TimelineClip> _markers = new Dictionary<string, TimelineClip>();
    private Dictionary<string, Action> _actions = new Dictionary<string, Action>();

    private void Awake() {
      _director = GetComponent<PlayableDirector>();
    }

    public bool TestMarker(string markerName, MarkerTest test) {
      TimelineClip clip;
      if (_markers.TryGetValue(markerName, out clip)) {
        switch (test) {
          case MarkerTest.BeforeMarkerStarts:
            return _director.time < clip.start;
          case MarkerTest.AfterMarkerStarts:
            return _director.time > clip.start;
          case MarkerTest.BeforeMarkerEnds:
            return _director.time < clip.end;
          case MarkerTest.AfterMarkerEnds:
            return _director.time > clip.end;
          case MarkerTest.InsideMarker:
            return _director.time > clip.start && _director.time < clip.end;
          case MarkerTest.OutsideMarker:
            return _director.time < clip.start || _director.time > clip.end;
          default:
            throw new ArgumentException();
        }
      } else {
        return false;
      }
    }

    public void LoopAtMarker(string markerName) {
      _actions[markerName] = Action.Loop;
    }

    public void PauseAtMarker(string markerName) {
      _actions[markerName] = Action.Pause;
    }

    public void SkipMarker(string markerName) {
      _actions[markerName] = Action.Skip;
    }

    public void ClearMarker(string markerName) {
      if (_actions.ContainsKey(markerName) &&
          _actions[markerName] == Action.Pause &&
          TestMarker(markerName, MarkerTest.InsideMarker)) {
        _director.Resume();
      }

      _actions[markerName] = Action.None;
    }

    private void Update() {
      updateMarkersIfNeeded();

      foreach (var action in _actions) {
        TimelineClip clip;
        if (!_markers.TryGetValue(action.Key, out clip)) {
          continue;
        }

        switch (action.Value) {
          case Action.Pause:
            if (_director.time >= clip.start) {
              _director.Pause();
            }
            break;
          case Action.Loop:
            if (_director.time >= clip.end) {
              _director.time = clip.start;
            }
            break;
          case Action.Skip:
            if (_director.time >= clip.start && _director.time < clip.end) {
              _director.time = clip.end;
            }
            break;
        }
      }
    }

    public void updateMarkersIfNeeded() {
      if (_timeline == _director.playableAsset) {
        return;
      }

      _timeline = _director.playableAsset as TimelineAsset;

      _markers.Clear();
      var timeline = _director.playableAsset as TimelineAsset;
      for (int i = 0; i < timeline.outputTrackCount; i++) {
        var track = timeline.GetOutputTrack(i);
        if (track is MarkerTrack) {
          foreach (var clip in track.GetClips()) {
            var marker = clip.asset as MarkerClip;
            _markers[marker.markerName] = clip;
          }
        }
      }
    }

    private enum Action {
      None,
      Pause,
      Loop,
      Skip
    }

    public enum MarkerTest {
      BeforeMarkerStarts,
      AfterMarkerStarts,
      BeforeMarkerEnds,
      AfterMarkerEnds,
      InsideMarker,
      OutsideMarker
    }
  }
}
