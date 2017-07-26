using System;
using UnityEngine;

public static class AnimationCurveUtil {

  public static AnimationCurve Compress(AnimationCurve curve, float maxDelta = 0.005f, int checkSteps = 8) {
    AnimationCurve compressedCurve = new AnimationCurve(curve.keys);

    var keys = curve.keys;
    Keyframe nextKeyFrame = keys[keys.Length - 1];

    for (int i = compressedCurve.keys.Length - 2; i > 0; i--) {
      Keyframe currKeyframe = keys[i];
      Keyframe prevKeyframe = keys[i - 1];

      compressedCurve.RemoveKey(i);

      for (int k = 0; k < checkSteps; k++) {
        float percent = k / (checkSteps - 1.0f);
        float prevTime = Mathf.Lerp(currKeyframe.time, prevKeyframe.time, percent);
        float nextTime = Mathf.Lerp(currKeyframe.time, nextKeyFrame.time, percent);

        float prevValue_c = compressedCurve.Evaluate(prevTime);
        float prevValue_o = curve.Evaluate(prevTime);

        float nextValue_c = compressedCurve.Evaluate(nextTime);
        float nextValue_o = curve.Evaluate(nextTime);

        if (Mathf.Abs(prevValue_c - prevValue_o) > maxDelta || Mathf.Abs(nextValue_c - nextValue_o) > maxDelta) {
          compressedCurve.AddKey(currKeyframe);
          nextKeyFrame = currKeyframe;
          break;
        }
      }
    }

    return compressedCurve;
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

    return compressedCurves;
  }

}
