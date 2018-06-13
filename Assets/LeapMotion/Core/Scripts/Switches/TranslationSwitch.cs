using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class TranslationSwitch : TweenSwitch {

    #region Inspector

    [Header("Translation")]

    public Transform localTranslateTarget;

    [QuickButton("Use Current",
                 "setOnLocalPosition",
                 "Sets this property with the object's current local position.")]
    public Vector3 onLocalPosition;

    [QuickButton("Use Current",
                 "setOffLocalPosition",
                 "Sets this property with the object's current local position.")]
    public Vector3 offLocalPosition;

    [Header("Animation Curves")]

    [UnitCurve]
    public AnimationCurve movementCurve = DefaultCurve.SigmoidUp;

    #endregion

    #region Unity Events

    protected virtual void Reset() {
      if (localTranslateTarget == null) localTranslateTarget = this.transform;
      onLocalPosition  = localTranslateTarget.localPosition;
      offLocalPosition = localTranslateTarget.localPosition + Vector3.back * 0.20f;
    }

    protected override void Start() {
      base.Start();

      if (localTranslateTarget == null) localTranslateTarget = this.transform;
    }

    #endregion

    #region Switch Implementation

    protected override void updateSwitch(float time, bool immediately = false) {
      localTranslateTarget.localPosition = Vector3.LerpUnclamped(
                                                     offLocalPosition,
                                                     onLocalPosition,
                                                     movementCurve.Evaluate(time));
    }

    #endregion

    #region Support

    /// <summary>
    /// Sets the "onLocalPosition" field with the transform's current local position.
    /// </summary>
    private void setOnLocalPosition() {
      this.onLocalPosition = localTranslateTarget.localPosition;
    }

    /// <summary>
    /// Sets the "setOffLocalPosition" field with the transform's current local position.
    /// </summary>
    private void setOffLocalPosition() {
      this.offLocalPosition = localTranslateTarget.localPosition;
    }

    #endregion

  }

}
