using UnityEngine;
using Leap.Unity.Interaction;
using Leap;

public class BrushHandPostProcess : MonoBehaviour {
  public InteractionBrushHand leftHand;
  public InteractionBrushHand rightHand;
  public void Start() {}
  public void setHeldHand(Hand inHand) {
    if (enabled) {
      if (inHand.IsLeft) {
        leftHand.fillBones(inHand);
      } else {
        rightHand.fillBones(inHand);
      }
    }
  }
}
