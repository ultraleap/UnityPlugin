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
      Quaternion evaluated = new Quaternion(xCurve.Evaluate(time),
                                            yCurve.Evaluate(time),
                                            zCurve.Evaluate(time),
                                            wCurve.Evaluate(time));
      return Quaternion.Lerp(evaluated, evaluated, 1.0f);
    }

    /// <summary>
    /// Compresses the curves in these QuaternionCurves using
    /// AnimationCurveUtil.CompressRotations.
    /// </summary>
    public void Compress() {
      AnimationCurve outXCurve, outYCurve, outZCurve, outWCurve;
      AnimationCurveUtil.CompressRotations(xCurve, yCurve, zCurve, wCurve,
                                           out outXCurve, out outYCurve,
                                           out outZCurve, out outWCurve);
      xCurve = outXCurve;
      yCurve = outYCurve;
      zCurve = outZCurve;
      wCurve = outWCurve;
    }

  }

}