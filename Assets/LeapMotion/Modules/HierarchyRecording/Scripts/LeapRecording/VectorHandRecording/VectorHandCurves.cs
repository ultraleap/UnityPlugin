/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Recording {

  /// <summary> AnimationCurve data for an animated VectorHand representation. </summary>
  [System.Serializable]
  public struct VectorHandCurves {

    [SerializeField]
    private AnimationCurve _backingIsTrackedCurve;
    /// <summary> AnimationCurve for whether the hand is tracked. </summary>
    public AnimationCurve isTrackedCurve {
      get {
        if (_backingIsTrackedCurve == null) {
          _backingIsTrackedCurve = new AnimationCurve();
        }
        return _backingIsTrackedCurve;
      }
      set {
        _backingIsTrackedCurve = value;
      }
    }

    /// <summary> AnimationCurve data for palm position. </summary>
    public Vector3Curves palmPosCurves;

    /// <summary> AnimationCurve data for palm rotation. </summary>
    public QuaternionCurves palmRotCurves;

    [SerializeField]
    private Vector3Curves[] _backingJointPositionCurves;
    /// <summary> AnimationCurve data for the hand's fingers. </summary>
    public Vector3Curves[] jointPositionCurves {
      get {
        if (_backingJointPositionCurves == null) {
          _backingJointPositionCurves = new Vector3Curves[VectorHand.NUM_JOINT_POSITIONS];
          for (int i = 0; i < _backingJointPositionCurves.Length; i++) {
            _backingJointPositionCurves[i] = new Vector3Curves();
          }
        }
        return _backingJointPositionCurves;
      }
      set {
        _backingJointPositionCurves = value;
      }
    }

    /// <summary>
    /// The data for the last hand these VectorHandCurves sampled. This is used to
    /// generate velocity data for hands sampled in sequence. </summary>
    private Hand _lastHand;

    /// <summary>
    /// Adds keyframe data into these VectorHandCurves at the specified time using the
    /// provided hand data.
    /// </summary>
    public void AddKeyframes(float time, Hand hand) {
      bool isTracked = hand != null;
      
      isTrackedCurve.AddBooleanKey(time, isTracked);

      if (isTracked) {
        VectorHand vectorHand = Pool<VectorHand>.Spawn();
        try {
          VectorHand.Encode(hand, ref vectorHand);

          palmPosCurves.AddKeyframes(time, vectorHand.palmPos);
          palmRotCurves.AddKeyframes(time, vectorHand.palmRot);

          for (int i = 0; i < VectorHand.NUM_JOINT_POSITIONS; i++) {
            jointPositionCurves[i].AddKeyframes(time, vectorHand.jointPositions[i]);
          }
        }
        finally {
          Pool<VectorHand>.Recycle(vectorHand);
        }
      }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Compresses the AnimationCurve data currently held in these VectorHandCurves.
    /// </summary>
    public void Compress() {
      AnimationCurveUtil.Compress(isTrackedCurve, maxDelta: 0.001f);

      palmPosCurves.Compress();
      palmRotCurves.Compress();

      for (int i = 0; i < jointPositionCurves.Length; i++) {
        jointPositionCurves[i].Compress(maxDistanceError: 0.001f);
      }
    }
#endif

    /// <summary>
    /// Samples hand curve data into the provided hand object at the specified time.
    /// 
    /// If the hand is not tracked at the specified time, the function returns false,
    /// although hand data is still copied (via interpolation).
    /// </summary>
    public bool Sample(float time, Hand intoHand, bool isLeft) {
      bool isTracked = isTrackedCurve.Evaluate(time) > 0.5f;

      VectorHand vectorHand = Pool<VectorHand>.Spawn();
      try {
        vectorHand.isLeft  = isLeft;
        vectorHand.palmPos = palmPosCurves.Evaluate(time);
        vectorHand.palmRot = palmRotCurves.Evaluate(time);
        
        for (int i = 0; i < VectorHand.NUM_JOINT_POSITIONS; i++) {
          vectorHand.jointPositions[i] = jointPositionCurves[i].Evaluate(time);
        }

        VectorHand.Decode(ref vectorHand, intoHand);

        // Fill temporal data if we have a hand from the previous sampling.
        if (_lastHand != null) {
          intoHand.FillTemporalData(_lastHand, Time.deltaTime);

          _lastHand.CopyFrom(intoHand);
        }
        else {
          _lastHand = new Hand();
        }
      }
      finally {
        Pool<VectorHand>.Recycle(vectorHand);
      }

      return isTracked;
    }
    
  }

}
