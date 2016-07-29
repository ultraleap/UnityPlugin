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
