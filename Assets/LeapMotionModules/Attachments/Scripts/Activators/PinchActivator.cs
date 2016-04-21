using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;

public class PinchActivator : Activator {
  private IHandModel handModel = null;

  [Range (0.0f, 70.0f)]
  public float PinchOn = 10.0f;
  [Range (0.0f, 70.0f)]
  public float PinchOff = 20.0f;
  public float CurrentValue = 99f;
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
          CurrentValue = hand.PinchDistance;
          if(hand.PinchDistance <= PinchOn){
            Activate();
          } else if ( hand.PinchDistance >= PinchOff){
            Deactivate();
          }
        }
      }
      yield return new WaitForSeconds(Period);
    }
  }
}