/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Recording {

  public class VectorHandRecording : LeapRecording {

    [SerializeField]
    private VectorFrameCurves _frameCurves;

    private Hand _leftHand = new Hand();
    private Hand _rightHand = new Hand();

    public override float length {
      get {
        return _maxKeyframeTime - _minKeyframeTime;
      }
    }

    [SerializeField]
    private float _minKeyframeTime = 0f;
    public float minKeyframeTime {
      get { return _minKeyframeTime; }
    }

    [SerializeField]
    private float _maxKeyframeTime = 0f;
    public float maxKeyframeTime {
      get { return _maxKeyframeTime; }
    }

    /// <summary>
    /// Loads the provided sequential frame data into this recording representation.
    /// 
    /// The provided frames' timestamps are expected to monotonically increase.
    /// </summary>
    public override void LoadFrames(List<Frame> frames) {
      _frameCurves = new VectorFrameCurves();

      Debug.Log("Loading " + frames.Count + " frames.");

      _minKeyframeTime = (float)(frames[0]               .Timestamp * NS_TO_S);
      _maxKeyframeTime = (float)(frames[frames.Count - 1].Timestamp * NS_TO_S);

      Debug.Log("Total recording length is " + length);

      // Add each frame as a keyframe naively.
      foreach (var frame in frames) {
        float keyframeTime = (float)(frame.Timestamp * NS_TO_S - _minKeyframeTime);

        _frameCurves.AddKeyframes(keyframeTime, frame);
      }

#if UNITY_EDITOR
      // Compress these keyframes.
      _frameCurves.CompressCurves();
#endif

      Debug.Log("Finished loading frames.");
    }

    public override bool Sample(float time, Frame toFill, bool clampTimeToValid = true) {
      float timeToSample = time + minKeyframeTime;

      if (!clampTimeToValid && outsideDataBounds(timeToSample)) {
        return false;
      }
      else {
        _frameCurves.Sample(timeToSample, toFill, _leftHand, _rightHand);
        return true;
      }
    }

    private bool outsideDataBounds(float time) {
      return time < minKeyframeTime || time > maxKeyframeTime;
    }

  }

}
