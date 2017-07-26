using System;
using UnityEngine;
using Leap.Unity.Query;

public static class AnimationCurveUtil {

  public static AnimationCurve Compress(AnimationCurve curve, float maxDelta = 0.005f, int checkSteps = 8) {
    var curveArray = new AnimationCurve[] { curve };

    var result = CompressCurves(curveArray,
                                (src, dst, t) => {
                                  float originalValue = src[0].Evaluate(t);
                                  float compressedValue = dst[0].Evaluate(t);
                                  return Mathf.Abs(originalValue - compressedValue);
                                },
                                maxDelta,
                                checkSteps);
    return result[0];
  }

  //public static void CompressPositions(AnimationCurve xCurve,
  //                                     AnimationCurve yCurve,
  //                                     AnimationCurve zCurve,
  //                                 out AnimationCurve compressedXCurve,
  //                                 out AnimationCurve compressedYCurve,
  //                                 out AnimationCurve compressedZCurve,
  //                                     float maxDelta = 0.005f, int checkSteps = 8) {
  //}

  public static AnimationCurve[] CompressCurves(AnimationCurve[] curves,
                                                Func<AnimationCurve[], AnimationCurve[], float, float> costFunc,
                                                float maxCost,
                                                int checkSteps = 8) {
    var keyframes = new Keyframe[curves.Length][];
    var position = new int[curves.Length];
    var nextFrame = new int[curves.Length];
    var compressedCurves = new AnimationCurve[curves.Length];

    for (int i = 0; i < curves.Length; i++) {
      var keys = curves[i].keys;

      compressedCurves[i] = new AnimationCurve(keys);
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

          compressedCurves[i].RemoveKey(position[i]);

          for (int k = 0; k < checkSteps; k++) {
            float percent = k / (checkSteps - 1.0f);

            float prevTime = Mathf.Lerp(currKeyframe.time, prevKeyframe.time, percent);
            float nextTime = Mathf.Lerp(currKeyframe.time, nextKeyframe.time, percent);

            float prevCost = costFunc(curves, compressedCurves, prevTime);
            float nextCost = costFunc(curves, compressedCurves, nextTime);

            if (prevCost > maxCost || nextCost > maxCost) {
              compressedCurves[i].AddKey(currKeyframe);
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

}
