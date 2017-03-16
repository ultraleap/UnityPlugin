using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.LeapPaint.PetalMenu {

  public class SmoothStem {

    private static Vector3[] s_valueCache = new Vector3[4];
    private static float[] s_timeCache = new float[4];
    private static List<Vector3> s_stemPointsCache = new List<Vector3>();
    public static List<Vector3> GetStemPoints(Vector3 preStart, Vector3 start, Vector3 end, Vector3 postEnd, int numPoints=12) {
      numPoints = Mathf.Max(3, numPoints);

      s_valueCache[0] = preStart;
      s_valueCache[1] = start;
      s_valueCache[2] = end;
      s_valueCache[3] = postEnd;

      //s_timeCache[0] = -Vector3.Distance(s_valueCache[0], s_valueCache[1]);
      //s_timeCache[1] = 0F;
      //s_timeCache[2] = Vector3.Distance(s_valueCache[1], s_valueCache[2]);
      //s_timeCache[3] = (Vector3.Distance(s_valueCache[1], s_valueCache[2]) + Vector3.Distance(s_valueCache[2], s_valueCache[3]));

      s_timeCache[0] = -0.035F / Vector3.Distance(s_valueCache[1], s_valueCache[2]);
      s_timeCache[1] = 0F;
      s_timeCache[2] = 1F;
      s_timeCache[3] = 1F + 0.035F / Vector3.Distance(s_valueCache[1], s_valueCache[2]);

      s_stemPointsCache.Clear();
      s_stemPointsCache.Add(start);
      for (int i = 1; i < numPoints; i++) {
        s_stemPointsCache.Add(Interpolate(s_valueCache, s_timeCache, i * (1F / numPoints)));
      }
      //s_stemPointsCache.Add(end);
      return s_stemPointsCache;
    }

    //http://stackoverflow.com/questions/9489736/catmull-rom-curve-with-no-cusps-and-no-self-intersections
    private static Vector3 Interpolate(Vector3[] p, float[] time, float t) {
      Vector3 L01 = p[0] * (time[1] - t) / (time[1] - time[0]) + p[1] * (t - time[0]) / (time[1] - time[0]);
      Vector3 L12 = p[1] * (time[2] - t) / (time[2] - time[1]) + p[2] * (t - time[1]) / (time[2] - time[1]);
      Vector3 L23 = p[2] * (time[3] - t) / (time[3] - time[2]) + p[3] * (t - time[2]) / (time[3] - time[2]);
      Vector3 L012 = L01 * (time[2] - t) / (time[2] - time[0]) + L12 * (t - time[0]) / (time[2] - time[0]);
      Vector3 L123 = L12 * (time[3] - t) / (time[3] - time[1]) + L23 * (t - time[1]) / (time[3] - time[1]);
      Vector3 C12 = L012 * (time[2] - t) / (time[2] - time[1]) + L123 * (t - time[1]) / (time[2] - time[1]);
      return C12;
    }

  }

  public class SmoothRandomVector3 {
    System.Random randomState;
    Vector3[] Values = new Vector3[4];
    float[] Times = { -1f, 0f, 1f, 2f };
    float lastTimeUpdated = 0f;
    Vector3 MinValue, MaxValue;
    bool Centripetal;
    bool GaussianDistribution = false;

    public SmoothRandomVector3(Vector3 Min, Vector3 Max, int randomSeed = 0, bool ConstVel = false, float time = 0f) {
      randomState = new System.Random(randomSeed);
      MinValue = Min;
      MaxValue = Max;
      lastTimeUpdated = time;
      Centripetal = ConstVel;

      for (int i = 0; i < 4; i++) {
        Values[i] = (MinValue + MaxValue) / 2f;
      }
    }

    public SmoothRandomVector3(bool Gaussian = false, int randomSeed = 0, bool ConstVel = false, float time = 0f) {
      randomState = new System.Random(randomSeed);
      GaussianDistribution = Gaussian;
      MinValue = Vector3.one * -0.5f;
      MaxValue = Vector3.one * 0.5f;
      lastTimeUpdated = time;
      Centripetal = ConstVel;

      for (int i = 0; i < 4; i++) {
        Values[i] = (MinValue + MaxValue) / 2f;//MinValue + (((float)i) / 4f * (MinValue - MaxValue));
      }
    }

    public Vector3 GetRandomValue(float time = 0f) {
      if (time == 0f) { time = Time.time; }

      while (time - lastTimeUpdated > (Centripetal ? Times[2] : 1f)) {
        AddNewRandomValue();
      }

      return interpolate(Values, Times, time - lastTimeUpdated);
    }

    private void AddNewRandomValue() {
      lastTimeUpdated += Centripetal ? Times[2] : 1f;

      Values[0] = Values[1];
      Values[1] = Values[2];
      Values[2] = Values[3];
      if (!GaussianDistribution) {
        Values[3] = new Vector3(Mathf.Lerp(MinValue.x, MaxValue.x, (float)randomState.NextDouble()), Mathf.Lerp(MinValue.y, MaxValue.y, (float)randomState.NextDouble()), Mathf.Lerp(MinValue.z, MaxValue.z, (float)randomState.NextDouble()));
      }
      else {
        Values[3] = (UnityEngine.Random.rotation * Vector3.one * Mathf.Sqrt(-2f * Mathf.Log(UnityEngine.Random.value)) * 0.6f / 10f);
      }

      if (Centripetal) {
        Times[0] = -Vector3.Distance(Values[0], Values[1]);
        Times[1] = 0f;
        Times[2] = Vector3.Distance(Values[1], Values[2]);
        Times[3] = (Vector3.Distance(Values[1], Values[2]) + Vector3.Distance(Values[2], Values[3]));
      }
    }

    //Returns a position between 4 Floats with Catmull-Rom Spline algorithm
    //http://www.iquilezles.org/www/articles/minispline/minispline.htm
    Vector3 CatmullRom(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
      Vector3 a = 0.5f * (2f * p1);
      Vector3 b = 0.5f * (p2 - p0);
      Vector3 c = 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3);
      Vector3 d = 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3);

      return a + (b * t) + (c * t * t) + (d * t * t * t);
    }

    //http://stackoverflow.com/questions/9489736/catmull-rom-curve-with-no-cusps-and-no-self-intersections
    Vector3 interpolate(Vector3[] p, float[] time, float t) {
      Vector3 L01 = p[0] * (time[1] - t) / (time[1] - time[0]) + p[1] * (t - time[0]) / (time[1] - time[0]);
      Vector3 L12 = p[1] * (time[2] - t) / (time[2] - time[1]) + p[2] * (t - time[1]) / (time[2] - time[1]);
      Vector3 L23 = p[2] * (time[3] - t) / (time[3] - time[2]) + p[3] * (t - time[2]) / (time[3] - time[2]);
      Vector3 L012 = L01 * (time[2] - t) / (time[2] - time[0]) + L12 * (t - time[0]) / (time[2] - time[0]);
      Vector3 L123 = L12 * (time[3] - t) / (time[3] - time[1]) + L23 * (t - time[1]) / (time[3] - time[1]);
      Vector3 C12 = L012 * (time[2] - t) / (time[2] - time[1]) + L123 * (t - time[1]) / (time[2] - time[1]);
      return C12;
    }

  }

}