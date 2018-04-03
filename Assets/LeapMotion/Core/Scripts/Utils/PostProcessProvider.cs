using UnityEngine;
using Leap.Unity.Attributes;
namespace Leap.Unity {
  public abstract class PostProcessProvider : LeapProvider {
    [Tooltip("The LeapProvider to use to drive hand representations in the defined "
           + "model pool groups.")]
    [SerializeField]
    [OnEditorChange("inputLeapProvider")]
    private LeapProvider _leapProvider;
    public LeapProvider inputLeapProvider {
      get { return _leapProvider; }
      set {
        if (_leapProvider != null) {
          _leapProvider.OnFixedFrame -= ProcessFixedFrame;
          _leapProvider.OnUpdateFrame -= ProcessUpdateFrame;
        }

        _leapProvider = value;

        if (_leapProvider != null) {
          _leapProvider.OnFixedFrame += ProcessFixedFrame;
          _leapProvider.OnUpdateFrame += ProcessUpdateFrame;
        }
      }
    }

    protected virtual void OnEnable() {
      if (_leapProvider == null) {
        _leapProvider = Hands.Provider;
      }

      _leapProvider.OnUpdateFrame -= ProcessUpdateFrame;
      _leapProvider.OnUpdateFrame += ProcessUpdateFrame;

      _leapProvider.OnFixedFrame -= ProcessFixedFrame;
      _leapProvider.OnFixedFrame += ProcessFixedFrame;
    }

    public override Frame CurrentFrame {
      get {
        #if UNITY_EDITOR
        if (!Application.isPlaying) {
          ProcessUpdateFrame(_leapProvider.CurrentFrame);
        }
        #endif
        return _cachedUpdateFrame;
      }
    }
    public override Frame CurrentFixedFrame {
      get {
        #if UNITY_EDITOR
        if (!Application.isPlaying) {
          ProcessUpdateFrame(_leapProvider.CurrentFixedFrame);
        }
        #endif
        return _cachedFixedFrame;
      }
    }

    private Frame _cachedUpdateFrame = new Frame();
    private Frame _cachedFixedFrame = new Frame();

    private void ProcessUpdateFrame(Frame inputFrame) {
      _cachedUpdateFrame.CopyFrom(inputFrame);
      ProcessFrame(ref _cachedUpdateFrame);
      DispatchUpdateFrameEvent(_cachedUpdateFrame);
    }
    private void ProcessFixedFrame(Frame inputFrame) {
      _cachedFixedFrame.CopyFrom(inputFrame);
      ProcessFrame(ref _cachedFixedFrame);
      DispatchFixedFrameEvent(_cachedFixedFrame);
    }

    public abstract void ProcessFrame(ref Frame inputFrame);
  }
}
