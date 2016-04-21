using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;

public class OpenHandActivator : Activator {
  private IHandModel handModel = null;

  public int OnCount = 5;
  public int OffCount = 3;

  void Start () {
    handModel = gameObject.GetComponentInParent<IHandModel>();
  }

  void OnEnable () {
    StartCoroutine(openHandWatcher());
  }

  void OnDisable () {
    StopCoroutine(openHandWatcher());
  }

  IEnumerator openHandWatcher() {
    int extended = 0;
    Hand hand;
    while(true){
      if(handModel != null){
        hand = handModel.GetLeapHand();
        if(hand != null){
          extended = 0;
          for(int f = 0; f < HandModel.NUM_FINGERS; f++){
            if(hand.Fingers[f].IsExtended){
              extended++;
            }
          }
          if(extended >= OnCount){
            Activate();
          } else if(extended <= OffCount){
            Deactivate();
          }
        }
      }
      yield return new WaitForSeconds(Period);
    }
  }
}
