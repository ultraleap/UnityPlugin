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

}
