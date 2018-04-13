/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;

namespace Leap.Unity.Playback {

  public class PlaybackProvider : LeapProvider {

    public override Frame CurrentFrame {
      get {
        return _transformedFrame;
      }
    }

    public override Frame CurrentFixedFrame {
      get {
        return CurrentFrame;
      }
    }

    [SerializeField]
    protected Recording _recording;

    [SerializeField]
    protected PlaybackTimeline _playbackTimeline = PlaybackTimeline.Graphics;

    [SerializeField]
    protected bool _autoPlay = true;

    protected bool _isPlaying = false;
    protected int _currentFrameIndex = 0;
    protected float _startTime = 0;

    protected Frame _transformedFrame = new Frame();

    public virtual bool IsPlaying {
      get {
        return _isPlaying;
      }
    }

    public virtual Recording recording {
      get {
        return _recording;
      }
      set {
        Stop();
        _recording = value;
      }
    }

    public virtual void Play() {
      float delta = _recording.frameTimes[_currentFrameIndex] - _recording.frameTimes[0];

      switch (_playbackTimeline) {
        case PlaybackTimeline.Graphics:
          _startTime = Time.time - delta;
          break;
        case PlaybackTimeline.Physics:
          _startTime = Time.fixedTime - delta;
          break;
      }

      _isPlaying = true;
    }

    public virtual void Pause() {
      _isPlaying = false;
    }

    public virtual void Stop() {
      Pause();
      if (_recording != null) {
        Seek(0);
      }
    }

    public virtual void Seek(int newFrameIndex) {
      newFrameIndex = Mathf.Clamp(newFrameIndex, 0, _recording.frames.Count - 1);
      if (newFrameIndex == _currentFrameIndex) {
        return;
      }

      _currentFrameIndex = newFrameIndex;

      _transformedFrame.CopyFrom(_recording.frames[_currentFrameIndex]).Transform(new LeapTransform(transform.position.ToVector(), transform.rotation.ToLeapQuaternion(), transform.lossyScale.ToVector()));
    }

    protected virtual void Start() {
      if (_autoPlay) {
        Play();
      }
    }

    protected virtual void Update() {
      if (_isPlaying) {
        if (_playbackTimeline == PlaybackTimeline.Graphics) {
          stepRecording(Time.time - _startTime);
        }
        DispatchUpdateFrameEvent(_transformedFrame);
      }
    }

    protected virtual void FixedUpdate() {
      if (_isPlaying) {
        if (_playbackTimeline == PlaybackTimeline.Physics) {
          stepRecording(Time.fixedTime - _startTime);
        }
        DispatchFixedFrameEvent(_transformedFrame);
      }
    }

    private void stepRecording(float time) {
      while (true) {
        if (_currentFrameIndex >= _recording.frames.Count - 1) {
          Pause();
          break;
        }

        float crossover = (_recording.frameTimes[_currentFrameIndex + 1] + _recording.frameTimes[_currentFrameIndex]) / 2.0f;
        if (time > crossover) {
          Seek(_currentFrameIndex + 1);
        } else {
          break;
        }
      }
    }

    public enum PlaybackTimeline {
      Graphics,
      Physics
    }
  }
}
