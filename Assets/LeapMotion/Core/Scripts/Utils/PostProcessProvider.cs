/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using Leap.Unity.Attributes;
namespace Leap.Unity {
  public abstract class PostProcessProvider : LeapProvider {
    [Tooltip("The LeapProvider to augment with this post-process.")]
    [SerializeField]
    [OnEditorChange("inputLeapProvider")]
    protected LeapProvider _inputLeapProvider;
    public LeapProvider inputLeapProvider {
      get { return _inputLeapProvider; }
      set {
        OnValidate();

        if (_inputLeapProvider != null) {
          _inputLeapProvider.OnFixedFrame -= ProcessFixedFrame;
          _inputLeapProvider.OnUpdateFrame -= ProcessUpdateFrame;
        }

        _inputLeapProvider = value;

        if (_inputLeapProvider != null) {
          _inputLeapProvider.OnFixedFrame += ProcessFixedFrame;
          _inputLeapProvider.OnUpdateFrame += ProcessUpdateFrame;
        }
      }
    }

    [Tooltip("Whether this step should apply its post-process or " +
  "pass the frames through un-modified.")]
    public bool postProcessingEnabled = true;

    protected virtual void OnEnable() {
      if (_inputLeapProvider == null && Hands.Provider != this) {
        _inputLeapProvider = Hands.Provider;
      }

      _inputLeapProvider.OnUpdateFrame -= ProcessUpdateFrame;
      _inputLeapProvider.OnUpdateFrame += ProcessUpdateFrame;

      _inputLeapProvider.OnFixedFrame -= ProcessFixedFrame;
      _inputLeapProvider.OnFixedFrame += ProcessFixedFrame;
    }

    protected virtual void OnValidate() {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        if (checkForCycles()) {
          _inputLeapProvider = null;
          Debug.LogError("Post-Process Cycle Detected!  " +
            "Nulling "+gameObject.name+"'s InputProvider...", this);
        }
      }
#endif
    }

    bool checkForCycles() {
      LeapProvider providerA = _inputLeapProvider, providerB = _inputLeapProvider;
      while (providerA is PostProcessProvider) {
        providerB = (providerB as PostProcessProvider).inputLeapProvider;
        if (providerA == providerB) { return true; }
           else if(!(providerB is PostProcessProvider)) { return false; }
        providerA = (providerA as PostProcessProvider).inputLeapProvider;
        providerB = (providerB as PostProcessProvider).inputLeapProvider;
      }
      return false;
    }

    public override Frame CurrentFrame {
      get {
        #if UNITY_EDITOR
        if (!Application.isPlaying && _inputLeapProvider != null) {
          ProcessUpdateFrame(_inputLeapProvider.CurrentFrame);
        }
        #endif
        return _cachedUpdateFrame;
      }
    }
    public override Frame CurrentFixedFrame {
      get {
        #if UNITY_EDITOR
        if (!Application.isPlaying && _inputLeapProvider != null) {
          ProcessUpdateFrame(_inputLeapProvider.CurrentFixedFrame);
        }
        #endif
        return _cachedFixedFrame;
      }
    }

    private Frame _cachedUpdateFrame = new Frame();
    private Frame _cachedFixedFrame = new Frame();

    private void ProcessUpdateFrame(Frame inputFrame) {
      _cachedUpdateFrame.CopyFrom(inputFrame);
      if (postProcessingEnabled) { ProcessFrame(ref _cachedUpdateFrame); }
      DispatchUpdateFrameEvent(_cachedUpdateFrame);
    }
    private void ProcessFixedFrame(Frame inputFrame) {
      _cachedFixedFrame.CopyFrom(inputFrame);
      if (postProcessingEnabled) { ProcessFrame(ref _cachedFixedFrame); }
      DispatchFixedFrameEvent(_cachedFixedFrame);
    }

    public abstract void ProcessFrame(ref Frame inputFrame);
  }
}
