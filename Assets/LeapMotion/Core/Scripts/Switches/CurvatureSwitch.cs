using Leap.Unity.Attributes;
using Leap.Unity.Space;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class CurvatureSwitch : TweenSwitch {

    #region Inspector

    [Header("Curvature Control")]

    [Tooltip("The radial space whose radius this switch will drive.")]
    public LeapRadialSpace leapRadialSpace;

    [Tooltip("The radius of the space when the object is fully switched on.")]
    public float onRadius = 1f;

    [Tooltip("The radius of the space when the object is fully switched off.")]
    public float offRadius = 0.10f;

    [UnitCurve]
    [Tooltip("The animation curve for the switch's animation.")]
    public AnimationCurve radiusUnitCurve = DefaultCurve.SigmoidUp;

    #endregion

    #region Switch Implementation

    protected override void updateSwitch(float time, bool immediately = false) {
#if UNITY_EDITOR
      // Support undo operations and properly serializing radius data at edit-time.
      // The Editor for all TweenSwitches automatically collapses Undo operations
      // that occur due to OnNow() or OffNow() calls at edit-time.
      if (!Application.isPlaying && leapRadialSpace != null) {
        UnityEditor.Undo.RecordObject(leapRadialSpace, "Update Radial Space Switch");
      }
#endif

      leapRadialSpace.radius = radiusUnitCurve.Evaluate(time)
                                              .Map(0f, 1f, offRadius, onRadius);
    }

    #endregion

  }

}
