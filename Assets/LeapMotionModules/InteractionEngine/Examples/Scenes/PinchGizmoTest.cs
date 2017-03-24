using Leap;
using Leap.Unity;
using Leap.Unity.Animation;
using Leap.Unity.Query;
using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinchGizmoTest : MonoBehaviour, IRuntimeGizmoComponent {

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    var rHand = Hands.Right;
    if (rHand != null) {
      drawer.color = Color.blue;
      Vector3 index = rHand.GetIndex().TipPosition.ToVector3();
      Vector3 thumb = rHand.GetThumb().TipPosition.ToVector3();
      drawer.DrawSphere(index, 0.005F);
      drawer.DrawSphere(thumb, 0.005F);
      drawer.DrawLine(index, thumb);
      drawer.color = new Color(0.8F, 0.4F, 0.2F);
      drawer.color = Color.Lerp(drawer.color, Color.white, rHand.PinchStrength);
      drawer.DrawSphere(rHand.AwesomePinchPosition(), 0.006F);
    }
  }

}

public static class AwesomeHandExtensions {

  private const float PINCH_STRENGTH_HEURISTIC_CUTOFF = 0.2F;
  /// <summary> Returns an approximation of the index-thumb pinch position of the hand.
  /// If the hand is not pinching, this will return an estimate of where the hand will pinch.
  /// As the hand pinches, the position this method returns will move to match the actual pinch
  /// position of the hand.
  /// Optionally, provide useRigidHeuristicPositionOnly=true to only return the heuristic
  /// pinch position, which is rigid with respect to the rotation of the hand. </summary>
  public static Vector3 AwesomePinchPosition(this Hand hand, bool useRigidHeuristicPositionOnly=false) {
    Vector3 indexTip = hand.GetIndex().TipPosition.ToVector3();
    Vector3 thumbTip = hand.GetThumb().TipPosition.ToVector3();

    // The so-called "naive" pinch point is the point a little past halfway to the thumb from the
    // tip of the index finger.
    Vector3 naivePinchPoint = Vector3.Lerp(indexTip, thumbTip, 0.5F);

    // The heuristic pinch point is a rigid point in hand-space linearly offset and scaled by
    // the index finger knuckle position and index finger length.
    Vector3 indexKnuckle = hand.Fingers[1].bones[1].PrevJoint.ToVector3();
    Vector3 heuristicPinchPoint = indexKnuckle; 
    float indexLength = hand.Fingers[1].bones.Query()
                                             .Skip(1) // skip metacarpal
                                             .Select(f => f.Length)
                                             .Fold((lengthSoFar, length) => lengthSoFar + length);
    Vector3 radialAxis = hand.RadialAxis();
    heuristicPinchPoint += hand.PalmarAxis() * indexLength * 0.80F
                         + hand.DistalAxis() * indexLength * 0.10F
                         +      radialAxis   * indexLength * 0.20F;
    float thumbInfluence = Vector3.Dot((thumbTip - indexKnuckle).normalized, radialAxis).Map(0F, 1F, 0.5F, 0F);
    heuristicPinchPoint = Vector3.Lerp(heuristicPinchPoint, thumbTip, thumbInfluence);

    // The heuristic pinch point is more accurate when the fingers are far away from pinching;
    // the naive pinch point is more accurate when the fingers are close to pinching.
    if (useRigidHeuristicPositionOnly) {
      return heuristicPinchPoint;
    }
    else {
      float pinchDistance = Vector3.Distance(indexTip, thumbTip) / indexLength;
      float pinchCloseness = pinchDistance.Map(PINCH_STRENGTH_HEURISTIC_CUTOFF, 1F, 1F, 0F);
      return Vector3.Lerp(heuristicPinchPoint, naivePinchPoint, Ease.Cubic.InOut(pinchCloseness));
    }
  }

}