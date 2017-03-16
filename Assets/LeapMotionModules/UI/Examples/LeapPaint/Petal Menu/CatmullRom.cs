using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  public static class CatmullRom {

    private static List<Vector3> s_pointsCache = new List<Vector3>(32);
    /// <summary> Performs centripetal Catmull-Rom interpolation between positions[1] and
    /// positions[2] and returns the points in a temporary list cache. positions[1]
    /// will be the first of the points returned; the last point will be just before
    /// positions[2].</summary>
    /// <param name="positions">Input positions array of length 4.</param>
    /// <param name="times">Times of each position, length 4.</param>
    /// <param name="numPoints">How many interpolated points to return.</param>
    public static List<Vector3> InterpolatePoints(Vector3[] positions, float[] times, int numPoints=12) {
      if (positions.Length < 4) {
        Debug.LogError("Must provide four points in order to perform Catmull-Rom interpolation.");
        return null;
      }
      if (positions.Length != times.Length) {
        Debug.LogError("Times array length must match positions array length for Catmull-Rom interpolation.");
        return null;
      }

      numPoints = Mathf.Max(3, numPoints);

      s_pointsCache.Clear();
      s_pointsCache.Add(positions[0]);
      for (int i = 1; i < numPoints; i++) {
        s_pointsCache.Add(Interpolate(positions, times, i * (1F / numPoints)));
      }
      return s_pointsCache;
    }

    /// <summary> Performs centripetal Catmull-Rom interpolation between positions[1] and positions[2]
    /// and returns the resulting points in the outPoints array, as evaluated evenly-spaced from [0, 1]. </summary>
    public static void InterpolatePoints(Vector3[] positions, float[] times, Vector3[] outPoints) {
      for (int i = 0; i < outPoints.Length; i++) {
        outPoints[i] = Interpolate(positions, times, i * (1F / (outPoints.Length - 1)));
      }
    }

    //http://stackoverflow.com/questions/9489736/catmull-rom-curve-with-no-cusps-and-no-self-intersections
    /// <summary> Evaluates the Catmull-Rom spline between p1 and p2 given points P and times T at time t. </summary>
    private static Vector3 Interpolate(Vector3[] P, float[] T, float t) {
      Vector3 L01 = P[0] * (T[1] - t) / (T[1] - T[0]) + P[1] * (t - T[0]) / (T[1] - T[0]);
      Vector3 L12 = P[1] * (T[2] - t) / (T[2] - T[1]) + P[2] * (t - T[1]) / (T[2] - T[1]);
      Vector3 L23 = P[2] * (T[3] - t) / (T[3] - T[2]) + P[3] * (t - T[2]) / (T[3] - T[2]);
      Vector3 L012 = L01 * (T[2] - t) / (T[2] - T[0]) + L12  * (t - T[0]) / (T[2] - T[0]);
      Vector3 L123 = L12 * (T[3] - t) / (T[3] - T[1]) + L23  * (t - T[1]) / (T[3] - T[1]);
      Vector3 C12 = L012 * (T[2] - t) / (T[2] - T[1]) + L123 * (t - T[1]) / (T[2] - T[1]);
      return C12;
    }

  }

}