using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;

public class PinchActivator : Activator {
  private IHandModel handModel = null;

  [Range (0.0f, 1.0f)]
  public float PinchOn = .7f;
  [Range (0.0f, 1.0f)]
  public float PinchOff = .5f;

  void Start () {
    handModel = gameObject.GetComponentInParent<IHandModel>();
  }

  void OnEnable () {
    StartCoroutine(pinchWatcher());
  }

  void OnDisable () {
    StopCoroutine(pinchWatcher());
  }

  IEnumerator pinchWatcher() {
    Hand hand;
    while(true){
      if(handModel != null){
        hand = handModel.GetLeapHand();
        if(hand != null){
          if(hand.PinchStrength >= PinchOn){
            Activate();
          } else if ( hand.PinchStrength <= PinchOff){
            Deactivate();
          }
        }
      }
      yield return new WaitForSeconds(Period);
    }
  }
}