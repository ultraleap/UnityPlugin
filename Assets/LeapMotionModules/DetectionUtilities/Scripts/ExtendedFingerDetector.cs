using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;

namespace Leap.Unity.DetectionUtilities {

  public class ExtendedFingerDetector : Detector {
    public IHandModel handModel = null;
  
    public PointingState Thumb = PointingState.Either;
    public PointingState Index = PointingState.Either;
    public PointingState Middle = PointingState.Either;
    public PointingState Ring = PointingState.Either;
    public PointingState Pinky = PointingState.Either;
  
    void Start () {
      if(handModel == null){
        handModel = gameObject.GetComponentInParent<IHandModel>();
      }
    }
  
    void OnEnable () {
      StartCoroutine(extendedFingerWatcher());
    }
  
    void OnDisable () {
      StopCoroutine(extendedFingerWatcher());
    }
  
    IEnumerator extendedFingerWatcher() {
      Hand hand;
      while(true){
        bool fingerState = false;
        if(handModel != null){
          hand = handModel.GetLeapHand();
          if(hand != null){
            fingerState = matchFingerState(hand.Fingers[0], 0)
              && matchFingerState(hand.Fingers[1], 1)
              && matchFingerState(hand.Fingers[2], 2)
              && matchFingerState(hand.Fingers[3], 3)
              && matchFingerState(hand.Fingers[4], 4);
            if(fingerState){
              Activate();
            } else {
              Deactivate();
            }
          }
        }
        yield return new WaitForSeconds(Period);
      }
    }
  
    private bool matchFingerState(Finger finger, int ordinal){
      switch(ordinal){
        case 0:
          if(Thumb == PointingState.Either) return true;
          if(Thumb == PointingState.Extended && finger.IsExtended) return true;
          if(Thumb == PointingState.NotExtended && !finger.IsExtended) return true;
          return false;
        case 1:
          if(Index == PointingState.Either) return true;
          if(Index == PointingState.Extended && finger.IsExtended) return true;
          if (Index == PointingState.NotExtended && !finger.IsExtended) return true;
          return false;
        case 2:
          if(Middle == PointingState.Either) return true;
          if(Middle == PointingState.Extended && finger.IsExtended) return true;
          if (Middle == PointingState.NotExtended && !finger.IsExtended) return true;
          return false;
        case 3:
          if(Ring == PointingState.Either) return true;
          if(Ring == PointingState.Extended && finger.IsExtended) return true;
          if (Ring == PointingState.NotExtended && !finger.IsExtended) return true;
          return false;
        case 4:
          if(Pinky == PointingState.Either) return true;
          if(Pinky == PointingState.Extended && finger.IsExtended) return true;
          if (Pinky == PointingState.NotExtended && !finger.IsExtended) return true;
          return false;
        default:
          return false;
      }
    }
  }
  
  public enum PointingState{Extended, NotExtended, Either}
}
