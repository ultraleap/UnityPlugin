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

  /// <summary> AnimationCurve data for an animated Quaternion. </summary>
  [System.Serializable]
  public struct QuaternionCurves {

    [SerializeField]
    private AnimationCurve _xCurve;
    public AnimationCurve xCurve {
      get { if (_xCurve == null) { _xCurve = new AnimationCurve(); } return _xCurve; }
      set { _xCurve = value; }
    }

    [SerializeField]
    private AnimationCurve _yCurve;
    public AnimationCurve yCurve {
      get { if (_yCurve == null) { _yCurve = new AnimationCurve(); } return _yCurve; }
      set { _yCurve = value; }
    }

    [SerializeField]
    private AnimationCurve _zCurve;
    public AnimationCurve zCurve {
      get { if (_zCurve == null) { _zCurve = new AnimationCurve(); } return _zCurve; }
      set { _zCurve = value; }
    }

    [SerializeField]
    private AnimationCurve _wCurve;
    public AnimationCurve wCurve {
      get { if (_wCurve == null) { _wCurve = new AnimationCurve(); } return _wCurve; }
      set { _wCurve = value; }
    }

    public void AddKeyframes(float time, Quaternion value) {
      // Normalize the quaternion.
      value = Quaternion.Lerp(value, value, 1.0f);
      // Make sure the quaternion always stays on the same hemisphere of the hypersphere.
      if (value.w < 0f) {                     
        value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
      }
      // Normalize again!
      value = Quaternion.Lerp(value, value, 1.0f);

      xCurve.AddKey(time, value.x);
      yCurve.AddKey(time, value.y);
      zCurve.AddKey(time, value.z);
      wCurve.AddKey(time, value.w);
    }

    public Quaternion Evaluate(float time) {
      var x = xCurve.Evaluate(time);
      var y = yCurve.Evaluate(time);
      var z = zCurve.Evaluate(time);
      var w = wCurve.Evaluate(time);

      Quaternion evaluated = new Quaternion(x, y, z, w);
      return evaluated.ToNormalized();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Compresses the curves in these QuaternionCurves using
    /// AnimationCurveUtil.CompressRotations.
    /// </summary>
    public void Compress(float maxAngleError = 1f) {
      AnimationCurve outXCurve, outYCurve, outZCurve, outWCurve;
      AnimationCurveUtil.CompressRotations(xCurve, yCurve, zCurve, wCurve,
                                           out outXCurve, out outYCurve,
                                           out outZCurve, out outWCurve,
                                           maxAngleError: maxAngleError);
      xCurve = outXCurve;
      yCurve = outYCurve;
      zCurve = outZCurve;
      wCurve = outWCurve;
    }
#endif
  }

}
