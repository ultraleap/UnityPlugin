using UnityEditor;
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
      }
      finally {
        Pool<VectorHand>.Recycle(vectorHand);
      }

      return isTracked;
    }
    
  }

  public static class VectorHandCurvesExtensions {

    public static void AddBooleanKey(this AnimationCurve curve, float time, bool value) {
      var keyframe = new Keyframe() { time = time, value = value ? 1 : 0 };
      int keyframeIdx = curve.AddKey(keyframe);

      AnimationUtility.SetKeyBroken(curve, keyframeIdx, true);
      AnimationUtility.SetKeyLeftTangentMode(curve, keyframeIdx,
                                             AnimationUtility.TangentMode.Constant);
      AnimationUtility.SetKeyRightTangentMode(curve, keyframeIdx,
                                             AnimationUtility.TangentMode.Constant);
    }

  }

}