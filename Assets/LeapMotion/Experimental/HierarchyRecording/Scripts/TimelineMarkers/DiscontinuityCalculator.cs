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
  using Attributes;
  using RuntimeGizmos;

  [RequireComponent(typeof(PlayableDirector))]
  public class DiscontinuityCalculator : MonoBehaviour {

    public List<string> tracksToWatch = new List<string>();

    public bool drawGizmoOnDiscontinuity = true;

    [DisableIf("drawGizmoOnDiscontinuity", isEqualTo: false)]
    public Transform gizmoLocation;

    public Action<bool> OnUpdate;

    private PlayableDirector _director;
    private List<TimelineClip> _clipsToWatch = new List<TimelineClip>();

    private PlayableAsset _prevAsset;
    private double _prevTime;
    private PlayState _prevPlayState;

    private bool _willBeDiscontinuousNextFrame = false;

    private float _gizmoRadius = 0;

    private void Awake() {
      _director = GetComponent<PlayableDirector>();
      _prevPlayState = _director.state;
    }

    private void LateUpdate() {
      bool isDiscontinuity = false;

      //Check if the previous frame said we were going to be discontinuous
      if (_willBeDiscontinuousNextFrame) {
        isDiscontinuity = true;
        _willBeDiscontinuousNextFrame = false;
      }

      //If we switched to a different asset
      if (_director.playableAsset != _prevAsset) {
        isDiscontinuity = true;
        recalculateClips();
      }

      //If we moved back in time
      if (_director.time < _prevTime) {
        isDiscontinuity = true;
        _willBeDiscontinuousNextFrame = true;
      }

      //Or if we moved forward in time more than we should have
      if (_director.time - _prevTime > Time.deltaTime * 3) {
        isDiscontinuity = true;
      }

      //Or if the current state is different from the previous state
      if (_director.state != _prevPlayState) {
        isDiscontinuity = true;
        _willBeDiscontinuousNextFrame = true;
      }

      //Or if we entered or left any clips that we are watching
      foreach (var clip in _clipsToWatch) {
        if (didEnterOrLeaveClip(clip)) {
          isDiscontinuity = true;
          break;
        }
      }

      if (drawGizmoOnDiscontinuity) {
        if (isDiscontinuity) {
          _gizmoRadius += 0.1f;
        } else {
          _gizmoRadius *= 0.95f;
        }

        RuntimeGizmoDrawer drawer;
        if (RuntimeGizmoManager.TryGetGizmoDrawer(out drawer)) {
          drawer.color = Color.red;
          drawer.DrawSphere(gizmoLocation.position, _gizmoRadius);
        }
      }

      if (OnUpdate != null) {
        OnUpdate(isDiscontinuity);
      }

      _prevTime = _director.time;
      _prevAsset = _director.playableAsset;
      _prevPlayState = _director.state;
    }

    private void recalculateClips() {
      _clipsToWatch.Clear();

      var timeline = _director.playableAsset as TimelineAsset;
      if (timeline == null) {
        return;
      }

      for (int i = 0; i < timeline.outputTrackCount; i++) {
        var track = timeline.GetOutputTrack(i);
        Debug.Log(track.name);
        if (tracksToWatch.Contains(track.name)) {
          _clipsToWatch.AddRange(track.GetClips());
        }
      }
    }

    private bool didEnterOrLeaveClip(TimelineClip clip) {
      bool wasInside = _prevTime > clip.start && _prevTime < clip.end;
      bool isInside = _director.time > clip.start && _director.time < clip.end;
      return wasInside != isInside;
    }
  }
}
