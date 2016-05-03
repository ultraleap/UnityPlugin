using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;

namespace Leap.Unity.DetectionUtilities {

  public class PalmNormalDetector : BinaryDetector {
  
    public float OnAngle = 45; // degrees
    public float OffAngle = 65;
    public IHandModel handModel = null;
  
    void Start () {
      if(handModel == null){
        handModel = gameObject.GetComponentInParent<IHandModel>();
      }
    }
  
    void OnEnable () {
      StartCoroutine(palmWatcher());
    }
  
    void OnDisable () {
      StopCoroutine(palmWatcher());
    }
  
    IEnumerator palmWatcher() {
      Hand hand;
      Vector3 normal;
      while(true){
        if(handModel != null){
          hand = handModel.GetLeapHand();
          if(hand != null){
            normal = hand.PalmNormal.ToVector3();
            Vector3 lookForward = Camera.main.transform.forward;
            float angleTo = Vector3.Angle(normal, lookForward);
            if(angleTo >= 180 - OnAngle){
              Activate();
            } else if(angleTo < 180 - OffAngle) {
              Deactivate();
            }
          }
        }
        yield return new WaitForSeconds(Period);
      }
    }
  }
}