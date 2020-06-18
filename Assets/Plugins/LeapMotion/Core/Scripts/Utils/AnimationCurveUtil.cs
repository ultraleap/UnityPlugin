/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;

namespace Leap.Unity {

  public static class DefaultCurve {

    public static AnimationCurve Zero {
      get {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0, 0);
        curve.AddKey(1, 0);
        return curve;
      }
    }

    public static AnimationCurve One {
      get {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0, 1);
        curve.AddKey(1, 1);
        return curve;
      }
    }

    public static AnimationCurve LinearUp {
      get {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0, 0, 1, 1));
        curve.AddKey(new Keyframe(1, 1, 1, 1));
        return curve;
      }
    }

    public static AnimationCurve LinearDown {
      get {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0, 1, -1, -1));
        curve.AddKey(new Keyframe(1, 0, -1, -1));
        return curve;
      }
    }

    public static AnimationCurve SigmoidUp {
      get {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0, 0, 0, 0));
        curve.AddKey(new Keyframe(1, 1, 0, 0));
        return curve;
      }
    }

    public static AnimationCurve SigmoidDown {
      get {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0, 1, 0, 0));
        curve.AddKey(new Keyframe(1, 0, 0, 0));
        return curve;
      }
    }

    public static AnimationCurve SigmoidUpDown {
      get {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0,    0, 0, 0));
        curve.AddKey(new Keyframe(0.5f, 1, 0, 0));
        curve.AddKey(new Keyframe(1,    0, 0, 0));
        return curve;
      }
    }
  }

  public static class AnimationCurveUtil {

    public static bool IsConstant(this AnimationCurve curve) {
      var keys = curve.keys;
      var first = keys[0];
      for (int i = 0; i < keys.Length; i++) {
        var key = keys[i];

        if (!Mathf.Approximately(first.value, key.value)) {
          return false;
        }

        if (!Mathf.Approximately(key.inTangent, 0) && !float.IsInfinity(key.inTangent)) {
          return false;
        }

        if (!Mathf.Approximately(key.outTangent, 0) && !float.IsInfinity(key.outTangent)) {
          return false;
        }
      }
      return true;
    }

    public static bool ContainsKeyAtTime(this AnimationCurve curve, float time, float tolerance = 0.0000001f) {
      return curve.keys.Query().Any(k => Mathf.Abs(k.time - time) < tolerance);
    }

    public static AnimationCurve GetCropped(this AnimationCurve curve, float start, float end, bool slideToStart = true) {
      AnimationCurve newCurve = new AnimationCurve();

      //Get a copy of the latest key before start
      Keyframe? latestBeforeStart = null;
      var keys = curve.keys;
      for (int i = 0; i < keys.Length; i++) {
        var key = keys[i];
        if (key.time >= start) break;

        latestBeforeStart = key;
      }

      //Remove all keys before start or after end
      for (int i = keys.Length; i-- != 0;) {
        if (keys[i].time < start || keys[i].time > end) {
          curve.RemoveKey(i);
        }
      }

      bool alreadyHasZero = false;
      for (int i = 0; i < keys.Length; i++) {
        var key = keys[i];
        if (key.time >= start && key.time <= end) {
          if (slideToStart) {
            key.time -= start;
          }

          if (Mathf.Approximately(key.time, 0)) {
            alreadyHasZero = true;
          }

          newCurve.AddKey(key);
        }
      }

      if (latestBeforeStart.HasValue && !alreadyHasZero) {
        var toInsert = latestBeforeStart.Value;
        toInsert.time = 0;
        newCurve.AddKey(toInsert);
      }

      return newCurve;
    }

#if UNITY_EDITOR
    public static AnimationCurve Compress(AnimationCurve curve, float maxDelta = 0.005f, int checkSteps = 8) {
      var curveArray = new AnimationCurve[] { curve };

      var result = CompressCurves(curveArray,
                                (src, dst, t) => {
                                  float originalValue = src[0].Evaluate(t);
                                  float compressedValue = dst[0].Evaluate(t);
                                  return Mathf.Abs(originalValue - compressedValue) < maxDelta;
                                },
                                checkSteps);

      return result[0];
    }

    public static void CompressRotations(AnimationCurve xCurve,
                                         AnimationCurve yCurve,
                                         AnimationCurve zCurve,
                                         AnimationCurve wCurve,
                                     out AnimationCurve compressedXCurve,
                                     out AnimationCurve compressedYCurve,
                                     out AnimationCurve compressedZCurve,
                                     out AnimationCurve compressedWCurve,
                                         float maxAngleError = 1,
                                         int checkSteps = 8) {
      var curveArray = new AnimationCurve[] {
        xCurve,
        yCurve,
        zCurve,
        wCurve
      };

      var result = CompressCurves(curveArray,
                                (src, dst, t) => {
                                  Quaternion srcRot;
                                  srcRot.x = src[0].Evaluate(t);
                                  srcRot.y = src[1].Evaluate(t);
                                  srcRot.z = src[2].Evaluate(t);
                                  srcRot.w = src[3].Evaluate(t);

                                  Quaternion dstRot;
                                  dstRot.x = dst[0].Evaluate(t);
                                  dstRot.y = dst[1].Evaluate(t);
                                  dstRot.z = dst[2].Evaluate(t);
                                  dstRot.w = dst[3].Evaluate(t);

                                  float angle;
                                  Vector3 axis;
                                  (Quaternion.Inverse(dstRot) * srcRot).ToAngleAxis(out angle, out axis);

                                  return angle < maxAngleError;
                                },
                                checkSteps);

      compressedXCurve = result[0];
      compressedYCurve = result[1];
      compressedZCurve = result[2];
      compressedWCurve = result[3];
    }

    public static void CompressPositions(AnimationCurve xCurve,
                                         AnimationCurve yCurve,
                                         AnimationCurve zCurve,
                                     out AnimationCurve compressedXCurve,
                                     out AnimationCurve compressedYCurve,
                                     out AnimationCurve compressedZCurve,
                                        float maxDistanceError = 0.005f,
                                        int checkSteps = 8) {
      var curveArray = new AnimationCurve[] {
      xCurve,
      yCurve,
      zCurve
    };

      var results = CompressCurves(curveArray,
                                 (src, dst, t) => {
                                   Vector3 srcPos;
                                   srcPos.x = src[0].Evaluate(t);
                                   srcPos.y = src[1].Evaluate(t);
                                   srcPos.z = src[2].Evaluate(t);

                                   Vector3 dstPos;
                                   dstPos.x = dst[0].Evaluate(t);
                                   dstPos.y = dst[1].Evaluate(t);
                                   dstPos.z = dst[2].Evaluate(t);

                                   return Vector3.Distance(srcPos, dstPos) < maxDistanceError;
                                 },
                                 checkSteps);

      compressedXCurve = results[0];
      compressedYCurve = results[1];
      compressedZCurve = results[2];
    }

    public static AnimationCurve CompressScale(AnimationCurve curve,
                                               float maxScaleFactor,
                                               int checkSteps = 8) {
      var curveArray = new AnimationCurve[] {
      curve,
    };

      var results = CompressCurves(curveArray,
                                 (src, dst, t) => {
                                   float srcValue = src[0].Evaluate(t);
                                   float dstValue = dst[0].Evaluate(t);

                                   if (Mathf.Sign(srcValue) == Mathf.Sign(dstValue)) {
                                     return srcValue / dstValue < maxScaleFactor
                                         && dstValue / srcValue < maxScaleFactor;
                                   } else {
                                     return false;
                                   }
                                 },
                                 checkSteps);

      return results[0];
    }

    public static void CompressColorsHSV(AnimationCurve rCurve,
                                         AnimationCurve gCurve,
                                         AnimationCurve bCurve,
                                     out AnimationCurve compressedRCurve,
                                     out AnimationCurve compressedGCurve,
                                     out AnimationCurve compressedBCurve,
                                         float maxHueError,
                                         float maxSaturationError,
                                         float maxValueError,
                                         int checkSteps = 8) {
      var curveArray = new AnimationCurve[] {
      rCurve,
      gCurve,
      bCurve
    };

      var results = CompressCurves(curveArray,
                                 (src, dst, t) => {
                                   Color srcColor;
                                   srcColor.r = src[0].Evaluate(t);
                                   srcColor.g = src[1].Evaluate(t);
                                   srcColor.b = src[2].Evaluate(t);
                                   srcColor.a = 1;

                                   Color dstColor;
                                   dstColor.r = dst[0].Evaluate(t);
                                   dstColor.g = dst[1].Evaluate(t);
                                   dstColor.b = dst[2].Evaluate(t);
                                   dstColor.a = 1;

                                   float sH, sS, sV;
                                   float dH, dS, dV;
                                   Color.RGBToHSV(srcColor, out sH, out sS, out sV);
                                   Color.RGBToHSV(dstColor, out dH, out dS, out dV);

                                   return Mathf.Abs(sH - dH) < maxHueError &&
                                          Mathf.Abs(sS - dS) < maxSaturationError &&
                                          Mathf.Abs(sV - dV) < maxValueError;
                                 },
                                 checkSteps);

      compressedRCurve = results[0];
      compressedGCurve = results[1];
      compressedBCurve = results[2];
    }

    public static AnimationCurve[] CompressCurves(AnimationCurve[] curves,
                                                  Func<AnimationCurve[], AnimationCurve[], float, bool> isGood,
                                                  int checkSteps = 8) {
      var keyframes = new Keyframe[curves.Length][];
      var position = new int[curves.Length];
      var nextFrame = new int[curves.Length];
      var compressedCurves = new AnimationCurve[curves.Length];

      for (int i = 0; i < curves.Length; i++) {
        var keys = curves[i].keys;

        compressedCurves[i] = new AnimationCurve(keys);

        for (int j = 0; j < keys.Length; j++) {
          var leftT = AnimationUtility.GetKeyLeftTangentMode(curves[i], j);
          var rightT = AnimationUtility.GetKeyRightTangentMode(curves[i], j);

          AnimationUtility.SetKeyLeftTangentMode(compressedCurves[i], j, leftT);
          AnimationUtility.SetKeyRightTangentMode(compressedCurves[i], j, rightT);
        }

        keyframes[i] = keys;
        position[i] = keys.Length - 2;
        nextFrame[i] = keys.Length - 1;
      }

      do {
        for (int i = 0; i < curves.Length; i++) {

          if (position[i] > 0) {
            Keyframe nextKeyframe = keyframes[i][nextFrame[i]];
            Keyframe currKeyframe = keyframes[i][position[i]];
            Keyframe prevKeyframe = keyframes[i][position[i] - 1];

            var leftT = AnimationUtility.GetKeyLeftTangentMode(compressedCurves[i], position[i]);
            var rightT = AnimationUtility.GetKeyRightTangentMode(compressedCurves[i], position[i]);
            compressedCurves[i].RemoveKey(position[i]);

            for (int k = 0; k < checkSteps; k++) {
              float percent = k / (checkSteps - 1.0f);

              float prevTime = Mathf.Lerp(currKeyframe.time, prevKeyframe.time, percent);
              float nextTime = Mathf.Lerp(currKeyframe.time, nextKeyframe.time, percent);

              bool isPrevGood = isGood(curves, compressedCurves, prevTime);
              bool isNextgood = isGood(curves, compressedCurves, nextTime);

              if (!isPrevGood || !isNextgood) {
                int index = compressedCurves[i].AddKey(currKeyframe);
                AnimationUtility.SetKeyLeftTangentMode(compressedCurves[i], index, leftT);
                AnimationUtility.SetKeyRightTangentMode(compressedCurves[i], index, rightT);

                nextFrame[i] = position[i];
                break;
              }
            }

            position[i]--;
          }
        }
      } while (position.Query().Any(p => p > 0));

      return compressedCurves;
    }
#endif

    public static void AddBooleanKey(this AnimationCurve curve, float time, bool value) {
      var keyframe = new Keyframe() { time = time, value = value ? 1 : 0 };

#if UNITY_EDITOR
      int keyframeIdx = curve.AddKey(keyframe);
      AnimationUtility.SetKeyBroken(curve, keyframeIdx, true);
      AnimationUtility.SetKeyLeftTangentMode(curve, keyframeIdx,
                                             AnimationUtility.TangentMode.Constant);
      AnimationUtility.SetKeyRightTangentMode(curve, keyframeIdx,
                                             AnimationUtility.TangentMode.Constant);
#else
      curve.AddKey(keyframe);
#endif
    }
  }


}
