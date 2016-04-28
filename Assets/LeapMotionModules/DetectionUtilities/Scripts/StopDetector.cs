using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;

namespace Leap.Unity.DetectionUtilities {

  /** Detects a movement of the hand followed by a stop. */
  public class StopDetector : Detector {
    public LeapProvider Provider = null;
    private bool _velocityThresholdExceeded = false;
    private Vector3 _positionAtExceed = Vector3.zero;
    private Vector3 _directionAtExceed = Vector3.zero;
    private bool _capturedPosition = false;

//    public float Proximity = 0.1f; //meters
    public float VelocityThreshold = 0.1f; //meters/s
    public float MinimumVelocityThreshold = 0.05f; //meters/s
    public float MinimumDistance = .20f; //meters
    public Directions MovementDirection = Directions.Forward;
    public Vector3 CustomDirection = Vector3.forward;
    public float AngleTolerance = 45f; //degrees
    #if UNITY_EDITOR
    //For debugging --set Inspector to debug mode
    private float currentVelocity = 0;
    bool angleCheck;
    bool minSpeedCheck;
    bool minDistanceCheck;
    float computedAngle = 0;
    float distance = 0;
    #endif
  
    void Start () {
      if(Provider == null){
        Provider = GetComponentInParent<LeapServiceProvider>();
      }
    }
  
    void Update(){
      Hand thisHand;
      Frame frame = Provider.CurrentFrame;
      if(frame != null && frame.Hands.Count >= 1){
        thisHand = frame.Hands[0];
        if(thisHand != null){  
          #if UNITY_EDITOR
          //for debugging
          //Debug.DrawRay(thisHand.PalmPosition.ToVector3(), velocityDirection.ToVector3());
          currentVelocity = thisHand.PalmVelocity.MagnitudeSquared;
          #endif
  
          if(thisHand.PalmVelocity.MagnitudeSquared > VelocityThreshold){
            _velocityThresholdExceeded = true;
            _directionAtExceed = thisHand.PalmVelocity.Normalized.ToVector3();
            if(!_capturedPosition){
              _positionAtExceed = thisHand.PalmPosition.ToVector3();
              _capturedPosition = true;
            }
          }
        }
      }
    }
  
    void OnEnable () {
      StartCoroutine(stopWatcher());
    }
  
    void OnDisable () {
      StopCoroutine(stopWatcher());
      IsActive = false;
    }
  
    IEnumerator stopWatcher() {
      Hand thisHand;
      bool stopped = false;
      while(true){
        if(Provider){
          Frame frame = Provider.CurrentFrame;
          if(frame != null && frame.Hands.Count >= 1){
            thisHand = frame.Hands[0];
            if(thisHand != null){
              #if UNITY_EDITOR
              //for debugging
              computedAngle = Vector3.Angle(_directionAtExceed, selectedDirection());
              distance = Vector3.Distance(_positionAtExceed, thisHand.PalmPosition.ToVector3());
              angleCheck= Vector3.Angle(_directionAtExceed, selectedDirection()) < AngleTolerance;
              minSpeedCheck = thisHand.PalmVelocity.MagnitudeSquared < MinimumVelocityThreshold;
              minDistanceCheck = Vector3.Distance(_positionAtExceed, thisHand.PalmPosition.ToVector3()) > MinimumDistance;
              #endif

              //decide if clapped
              stopped = _velocityThresholdExceeded && //went fast enough
                        thisHand.PalmVelocity.MagnitudeSquared < MinimumVelocityThreshold && //Then slowed down
                        Vector3.Distance(_positionAtExceed, thisHand.PalmPosition.ToVector3()) > MinimumDistance && //went far enough
                        Vector3.Angle(_directionAtExceed, selectedDirection()) < AngleTolerance; //right direction
              if(stopped & !IsActive){
                Activate();
              } else if(stopped & IsActive){
                Deactivate();
              }
            }
          }
        }
        _velocityThresholdExceeded = false;
        _capturedPosition = false;
        yield return new WaitForSeconds(Period);
      }
    }

    private Vector3 selectedDirection(){
      switch(MovementDirection){
        case Directions.Backward:
          return -Camera.main.transform.forward;
        case Directions.Down:
          return -Camera.main.transform.up;
        case Directions.Forward:
          return Camera.main.transform.forward;
        case Directions.Left:
          return -Camera.main.transform.right;
        case Directions.Right:
          return Camera.main.transform.right;
        case Directions.Up:
          return Camera.main.transform.up;
        case Directions.CustomVector:
          return CustomDirection;
        default:
          return Camera.main.transform.forward;
      }
    }

  }
}
