using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;

namespace Leap.Unity.DetectionUtilities {

  public class HandClapToggleDetector : BinaryDetector {
    public LeapProvider Provider = null;
    private bool velocityThresholdExceeded = false;
    private bool _didClap = false;

    public float Proximity = 0.1f; //meters
    public float VelocityThreshold = 0.1f; //meters/s
    public float PalmAngleLimit = 75; //degrees
    public float Quiescence = .05f; //seconds between claps

    #if UNITY_EDITOR
    //For debugging --set Inspector to debug mode
    private float currentAngle = 0;
    private float currentVelocityVectorAngle = 0;
    private float currentDistance = 0;
    private float currentVelocity = 0;
    #endif
  
    void Start () {
      if(Provider == null){
        Provider = GetComponentInParent<LeapServiceProvider>();
      }
    }
  
    void Update(){
      Hand thisHand;
      Hand thatHand;
      Frame frame = Provider.CurrentFrame;
      if(frame != null && frame.Hands.Count >= 2){
        thisHand = frame.Hands[0];
        thatHand = frame.Hands[1];
        if(thisHand != null && thatHand != null){
          Vector velocityDirection = thisHand.PalmVelocity.Normalized;
          Vector otherhandDirection = (thisHand.PalmPosition - thatHand.PalmPosition).Normalized;
  
          #if UNITY_EDITOR
          //for debugging
          if (ShowGizmos) {
            Debug.DrawRay(thisHand.PalmPosition.ToVector3(), velocityDirection.ToVector3());
            Debug.DrawRay(thatHand.PalmPosition.ToVector3(), otherhandDirection.ToVector3());
          }
          currentAngle = thisHand.PalmNormal.AngleTo(thatHand.PalmNormal) * Constants.RAD_TO_DEG;
          currentDistance = thisHand.PalmPosition.DistanceTo(thatHand.PalmPosition);
          currentVelocity = thisHand.PalmVelocity.MagnitudeSquared + thatHand.PalmVelocity.MagnitudeSquared;
          currentVelocityVectorAngle = velocityDirection.AngleTo(otherhandDirection) * Constants.RAD_TO_DEG;
          #endif
  
          if( thisHand.PalmVelocity.MagnitudeSquared + thatHand.PalmVelocity.MagnitudeSquared > VelocityThreshold &&
            velocityDirection.AngleTo(otherhandDirection) >= (180 - PalmAngleLimit) * Constants.DEG_TO_RAD){
            velocityThresholdExceeded = true;
          }
        }
      }
    }
  
    void OnEnable () {
      StartCoroutine(clapWatcher());
    }
  
    void OnDisable () {
      StopCoroutine(clapWatcher());
      IsActive = false;
    }
  
    IEnumerator clapWatcher() {
      Hand thisHand;
      Hand thatHand;
      bool clapped = false;
      while(true){
        if(_didClap){
          _didClap = false;
          yield return new WaitForSeconds(Quiescence);
        }
        if(Provider){
          Frame frame = Provider.CurrentFrame;
          if(frame != null && frame.Hands.Count >= 2){
            thisHand = frame.Hands[0];
            thatHand = frame.Hands[1];
            if(thisHand != null && thatHand != null){
              //decide if clapped
              clapped = velocityThresholdExceeded && //went fast enough
                        thisHand.PalmPosition.DistanceTo(thatHand.PalmPosition) < Proximity && // and got close 
                        thisHand.PalmNormal.AngleTo(thatHand.PalmNormal) >= (180 - PalmAngleLimit) * Constants.DEG_TO_RAD; //while facing each other
    
              if(clapped & !IsActive){
                Activate();
                _didClap = true;
              } else if(clapped & IsActive){
                Deactivate();
                _didClap = true;
              }
            }
          }
        }
        velocityThresholdExceeded = false;
        yield return new WaitForSeconds(Period);
      }
    }
  }
}
