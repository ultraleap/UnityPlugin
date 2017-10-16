using UnityEngine;
using System.Collections;
using Leap.Unity;
public class OnAnimatorIKCaller : MonoBehaviour {
  public WristLeapToIKBlend wristLeapToIKBlend_L;
  public WristLeapToIKBlend wristLeapToIKBlend_R;

  // Use this for initialization
  void Start() {

  }
  void OnAnimatorIK() {
    //pass the animatorIK message down to the IKLeapHandController
    //Debug.Log ("sending message");
    wristLeapToIKBlend_L.OnAnimatorIK(0);
    wristLeapToIKBlend_R.OnAnimatorIK(0);
  }
}
