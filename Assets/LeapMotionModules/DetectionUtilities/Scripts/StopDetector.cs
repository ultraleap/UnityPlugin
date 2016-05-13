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
    private bool _didStop = false;
    private Vector3 stopPosition = Vector3.zero;

    public float VelocityThreshold = 0.1f; //meters/s
    public float StopVelocityThreshold = 0.05f; //meters/s
    public float MinimumDistance = .20f; //meters
    public bool CheckDirection = true;
    public Directions MovementDirection = Directions.Forward;
    public Vector3 CustomDirection = Vector3.forward;
    public float AngleTolerance = 45f; //degrees
    public float Quiescence = .01f; //seconds

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
  
          if(thisHand.PalmVelocity.MagnitudeSquared > VelocityThreshold && !_velocityThresholdExceeded){
            _velocityThresholdExceeded = true;
            _directionAtExceed = thisHand.PalmVelocity.Normalized.ToVector3();
              _positionAtExceed = thisHand.PalmPosition.ToVector3();
          }
        }
      }
    }
  
    void OnEnable () {
      StartCoroutine(stopWatcher());
    }
  
    void OnDisable () {
      StopCoroutine(stopWatcher());
    }
  
    IEnumerator stopWatcher() {
      Hand thisHand;
      bool stopped = false;
      while(true){
//        if(_didStop){
//          _didStop = false;
//          _velocityThresholdExceeded = false;
//          Debug.Log("Quiescence");
//          yield return new WaitForSeconds(Quiescence);
//        }
        if(Provider){
          Frame frame = Provider.CurrentFrame;
          if(frame != null && frame.Hands.Count >= 1){
            thisHand = frame.Hands[0];
            if(thisHand != null){
              #if UNITY_EDITOR
              //for debugging
              computedAngle = Vector3.Angle(_directionAtExceed, selectedDirection());
              distance = Vector3.Distance(_positionAtExceed, thisHand.PalmPosition.ToVector3());
              angleCheck= !CheckDirection || Vector3.Angle(_directionAtExceed, selectedDirection()) < AngleTolerance;
              minSpeedCheck = thisHand.PalmVelocity.MagnitudeSquared < StopVelocityThreshold;
              minDistanceCheck = Vector3.Distance(_positionAtExceed, thisHand.PalmPosition.ToVector3()) > MinimumDistance;
              if(_velocityThresholdExceeded){
                Debug.DrawLine(_positionAtExceed, thisHand.PalmPosition.ToVector3(), Color.white, Period * 2);
                Debug.DrawRay(_positionAtExceed, _directionAtExceed, Color.blue, Period * 2);
                Debug.DrawLine(_positionAtExceed, (thisHand.PalmPosition.ToVector3() - _positionAtExceed).normalized * MinimumDistance, Color.red, Period * 2);
              }
              Debug.DrawRay(thisHand.PalmPosition.ToVector3(), selectedDirection(), Color.green, Period);
              #endif

              //decide if clapped
              stopped = _velocityThresholdExceeded && //went fast enough
                        thisHand.PalmVelocity.MagnitudeSquared < StopVelocityThreshold && //Then slowed down
                        Vector3.Distance(_positionAtExceed, thisHand.PalmPosition.ToVector3()) > MinimumDistance && //went far enough
                        (!CheckDirection || Vector3.Angle(_directionAtExceed, selectedDirection()) < AngleTolerance); //right direction
              if(stopped){
                stopped = false;
                _didStop = true;
                stopPosition = transform.position;
              }
            }
          }
        }
        _velocityThresholdExceeded = false;
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
    #if UNITY_EDITOR
    void OnDrawGizmos () {
      if (ShowGizmos) {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(stopPosition, .05f);
      }
    }
    #endif
  }

  public enum Directions { Up, Down, Right, Left, Forward, Backward, CustomVector }

}
