using Leap.Unity.Attributes;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class RotationSwitch : TweenSwitch {

    #region Inspector

    [Header("Rotation")]

    public Transform localRotationTarget;

    [QuickButton("Use Current",
                 "setOnLocalRotation",
                 "Sets this property with the object's current local rotation.")]
    public Quaternion onLocalRotation;

    [QuickButton("Use Current",
                 "setOffLocalRotation",
                 "Sets this property with the object's current local rotation.")]
    public Quaternion offLocalRotation;

    [Header("Animation Curves")]

    [UnitCurve]
    public AnimationCurve movementCurve = DefaultCurve.SigmoidUp;

    #endregion

    #region Unity Events

    protected virtual void Reset() {
      if (localRotationTarget == null) localRotationTarget = this.transform;
      onLocalRotation = localRotationTarget.localRotation;
      offLocalRotation = Quaternion.AngleAxis(90f, Vector3.up)
                         * localRotationTarget.localRotation;
    }

    protected override void Start() {
      base.Start();

      if (localRotationTarget == null) localRotationTarget = this.transform;
    }

    #endregion

    #region Switch Implementation

    protected override void updateSwitch(float time, bool immediately = false) {
      localRotationTarget.localRotation = Quaternion.SlerpUnclamped(
                                                       offLocalRotation,
                                                       onLocalRotation,
                                                       movementCurve.Evaluate(time));
    }

    #endregion

    #region Support

    /// <summary>
    /// Sets the "onLocalRotation" field with the transform's current local rotation.
    /// </summary>
    private void setOnLocalRotation() {
      this.onLocalRotation = localRotationTarget.localRotation;
    }

    /// <summary>
    /// Sets the "setOffLocalRotation" field with the transform's current local rotation.
    /// </summary>
    private void setOffLocalRotation() {
      this.offLocalRotation = localRotationTarget.localRotation;
    }

    #endregion

  }

}
