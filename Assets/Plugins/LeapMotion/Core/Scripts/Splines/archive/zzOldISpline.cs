using UnityEngine;

namespace Leap.Unity.Splines {

  public interface zzOldISpline {

    /// <summary>
    /// Gets the number of control points in this spline.
    /// </summary>
    int numControlPoints { get; }

    /// <summary>
    /// Gets the position of the control point at the specified index.
    /// </summary>
    Vector3 GetControlPosition(int controlPointIdx);

    /// <summary>
    /// Gets the tangent vector of the control point at the specified index.
    /// </summary>
    Vector3 GetControlTangent(int controlPointIdx);

    /// <summary>
    /// Evaluates the spline between control points at idxA and idxB at 0-1 position t.
    /// 
    /// Hint: SplineUtility.CatmullRom.Interpolate is a pretty useful function.
    /// </summary>
    Vector3 Evaluate(int idxA, int idxB, float t);

  }

}