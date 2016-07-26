using UnityEngine;
using System;

namespace Leap.Unity.Playback {

  public class PlaybackProvider : LeapProvider {

    public override Frame CurrentFrame {
      get {
        throw new NotImplementedException();
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
    protected bool _autoPlay = true;

    protected bool _isPlaying = false;
    protected int _currentFrameIndex = 0;
    protected int _lastFrameUpdated = -1;

    public Recording recording {
      get {
        return _recording;
      }
      set {
        Stop();
        _recording = value;
      }
    }

    public void Play() {
      _isPlaying = true;
    }

    public void Pause() {
      _isPlaying = false;
    }

    public void Stop() {
      Pause();
      Seek(0);
    }

    public void Seek(int newFrameIndex) {
      newFrameIndex = Mathf.Clamp(newFrameIndex, 0, _recording.frames.Count - 1);
      if (newFrameIndex == _currentFrameIndex) {
        return;
      }

      _currentFrameIndex = newFrameIndex;
    }

    void Start() {
      if (_autoPlay) {
        Play();
      }
    }

    void Update() {
      if (_isPlaying) {
        DispatchUpdateFrameEvent(_recording.frames[_currentFrameIndex]);
      }
    }

    void LateUpdate() {
      if (_isPlaying) {
        if (_currentFrameIndex == _recording.frames.Count - 1) {
          Pause();
        } else {
          _currentFrameIndex++;
        }
      }
    }

    void FixedUpdate() {
      if (_isPlaying) {
        DispatchUpdateFrameEvent(_recording.frames[_currentFrameIndex]);
      }
    }
  }
}
