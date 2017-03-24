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

  /// <summary> Returns an approximation of the index-thumb pinch position of the hand. </summary>
  public static Vector3 AwesomePinchPosition(this Hand hand) {
    Vector3 indexTip = hand.GetIndex().TipPosition.ToVector3();
    Vector3 thumbTip = hand.GetThumb().TipPosition.ToVector3();

    // The heuristic pinch point is a rigid point in hand-space linearly offset and scaled by
    // the index finger knuckle position and index finger length, and lightly influenced by the
    // actual thumb and index tip positions.
    Vector3 indexKnuckle = hand.Fingers[1].bones[1].PrevJoint.ToVector3();
    Vector3 heuristicPinchPoint = indexKnuckle; 
    float indexLength = hand.Fingers[1].bones.Query()
                                             .Skip(1) // skip metacarpal
                                             .Select(f => f.Length)
                                             .Fold((lengthSoFar, length) => lengthSoFar + length);
    Vector3 radialAxis = hand.RadialAxis();
    heuristicPinchPoint += hand.PalmarAxis() * indexLength * 0.85F
                         + hand.DistalAxis() * indexLength * 0.20F
                         +      radialAxis   * indexLength * 0.20F;
    float thumbInfluence = Vector3.Dot((thumbTip - indexKnuckle).normalized, radialAxis).Map(0F, 1F, 0.5F, 0F);
    heuristicPinchPoint = Vector3.Lerp(heuristicPinchPoint, thumbTip, thumbInfluence);
    heuristicPinchPoint = Vector3.Lerp(heuristicPinchPoint, indexTip, 0.15F);

    return heuristicPinchPoint;
  }

}