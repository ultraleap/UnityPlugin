using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class ColorTweenSwitch : TweenSwitch {

    public Color onTargetColor = Color.white;
    public Color offTargetColor = Color.white.WithAlpha(0f);

    [Header("Animation Curves")]

    [UnitCurve]
    public AnimationCurve colorCurve = DefaultCurve.SigmoidUp;

    [Header("Output")]

    public Renderer colorSetRenderer;

    protected override void updateSwitch(float time, bool immediately = false) {

      var finalColor = Color.Lerp(offTargetColor, onTargetColor, colorCurve.Evaluate(time));

      if (colorSetRenderer != null) {
        if (Application.isPlaying) {
          colorSetRenderer.material.color = finalColor;
        }
        else {
          colorSetRenderer.sharedMaterial.color = finalColor;
        }
      }
    }

  }

}
