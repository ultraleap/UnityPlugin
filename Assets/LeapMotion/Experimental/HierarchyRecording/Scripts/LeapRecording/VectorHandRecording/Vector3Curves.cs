/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Recording {

  /// <summary> AnimationCurve data for an animated Vector3. </summary>
  [System.Serializable]
  public struct Vector3Curves {

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

    public void AddKeyframes(float time, Vector3 value) {
      xCurve.AddKey(time, value.x);
      yCurve.AddKey(time, value.y);
      zCurve.AddKey(time, value.z);
    }

    public Vector3 Evaluate(float time) {
      return new Vector3(xCurve.Evaluate(time),
                         yCurve.Evaluate(time),
                         zCurve.Evaluate(time));
    }

#if UNITY_EDITOR
    /// <summary>
    /// Compresses these Vector3Curves using AnimationCurveUtil.CompressPositions.
    /// </summary>
    public void Compress(float maxDistanceError = 0.005f) {
      AnimationCurve outXCurve, outYCurve, outZCurve;
      AnimationCurveUtil.CompressPositions(xCurve, yCurve, zCurve,
                                           out outXCurve, out outYCurve, out outZCurve,
                                           maxDistanceError: maxDistanceError);
      xCurve = outXCurve;
      yCurve = outYCurve;
      zCurve = outZCurve;
    }
#endif
  }

}
