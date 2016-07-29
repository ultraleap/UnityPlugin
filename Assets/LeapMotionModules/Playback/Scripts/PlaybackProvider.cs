using UnityEngine;
using System;

namespace Leap.Unity.Playback {

  public class PlaybackProvider : LeapProvider {

    public override Frame CurrentFrame {
      get {
        if (_recording != null) {
          return _recording.frames[_currentFrameIndex];
        } else {
          return new Frame();
        }
      }
    }

    public override Frame CurrentFixedFrame {
      get {
        return CurrentFrame;
      }
    }

    public override Image CurrentImage {
      get {
        return null;
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
    protected float _residualFrame = 0;

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
    }

    protected virtual void Start() {
      if (_autoPlay) {
        Play();
      }
    }

    protected virtual void Update() {
      if (_isPlaying) {
        if (_playbackTimeline == PlaybackTimeline.Graphics) {
          stepRecording(Time.deltaTime);
        }
        DispatchUpdateFrameEvent(_recording.frames[_currentFrameIndex]);
      }
    }

    protected virtual void FixedUpdate() {
      if (_isPlaying) {
        if (_playbackTimeline == PlaybackTimeline.Physics) {
          stepRecording(Time.fixedDeltaTime);
        }
        DispatchFixedFrameEvent(_recording.frames[_currentFrameIndex]);
      }
    }

    private void stepRecording(float deltaTime) {
      _residualFrame += deltaTime;
      float delta = 1.0f / _recording.framesPerSecond;
      while (_residualFrame >= 1.0f / _recording.framesPerSecond) {
        _residualFrame -= delta;

        if (_currentFrameIndex >= _recording.frames.Count - 1) {
          Pause();
        } else {
          Seek(_currentFrameIndex + 1);
        }
      }
    }

    public enum PlaybackTimeline {
      Graphics,
      Physics
    }
  }
}
