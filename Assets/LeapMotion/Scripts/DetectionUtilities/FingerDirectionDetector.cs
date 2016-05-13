using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {

  public class FingerDirectionDetector : Detector {
    public IHandModel handModel = null;
  
    public Finger.FingerType FingerName = Finger.FingerType.TYPE_INDEX;
    public Vector3 PointingDirection = Vector3.forward;
    public PointingType PointingType = PointingType.RelativeToHorizon;
    public Transform TargetObject = null;
    public float OnAngle = 45f; //degrees
    public float OffAngle = 65f; //degrees
  
    void Start () {
      if(handModel == null){
        handModel = gameObject.GetComponentInParent<IHandModel>();
      }
    }

    void OnEnable () {
      StartCoroutine(fingerPointingWatcher());
    }
  
    void OnDisable () {
      StopCoroutine(fingerPointingWatcher());
      Deactivate();
    }
  
    IEnumerator fingerPointingWatcher() {
      Hand hand;
      Vector3 fingerDirection;
      Vector3 targetDirection;
      int selectedFinger = selectedFingerOrdinal();
      while(true){
        if(handModel != null){
          hand = handModel.GetLeapHand();
          if(hand != null){
            targetDirection = selectedDirection(hand.Fingers[selectedFinger].TipPosition.ToVector3());
            fingerDirection = hand.Fingers[selectedFinger].Direction.ToVector3();
            float angleTo = Vector3.Angle(fingerDirection, targetDirection);
            if(handModel.IsTracked && angleTo <= OnAngle){
              Activate();
            } else if (!handModel.IsTracked || angleTo >= OffAngle) {
              Deactivate();
            }
          }
        }
        yield return new WaitForSeconds(Period);
      }
    }
  
    private Vector3 selectedDirection(Vector3 tipPosition){
      switch(PointingType){
        case PointingType.RelativeToHorizon:
          Quaternion cameraRot = Camera.main.transform.rotation;
          float cameraYaw = cameraRot.eulerAngles.y;
          Quaternion rotator = Quaternion.AngleAxis(cameraYaw, Vector3.up);
          return rotator * PointingDirection;
        case PointingType.RelativeToCamera:
          return Camera.main.transform.TransformDirection(PointingDirection);
        case PointingType.RelativeToWorld:
          return PointingDirection;
        case PointingType.AtTarget:
          return TargetObject.position - tipPosition;
        default:
          return PointingDirection;
      }
    }
  
    private int selectedFingerOrdinal(){
      switch(FingerName){
        case Finger.FingerType.TYPE_INDEX:
          return 1;
        case Finger.FingerType.TYPE_MIDDLE:
          return 2;
        case Finger.FingerType.TYPE_PINKY:
          return 4;
        case Finger.FingerType.TYPE_RING:
          return 3;
        case Finger.FingerType.TYPE_THUMB:
          return 0;
        default:
          return 1;
      }
    }

  #if UNITY_EDITOR
    void OnDrawGizmos () {
      if (ShowGizmos && handModel != null) {
        Color innerColor;
        if (IsActive) {
          innerColor = Color.green;
        } else {
          innerColor = Color.blue;
        }
        Finger finger = handModel.GetLeapHand().Fingers[selectedFingerOrdinal()];
        Utils.DrawCone(finger.TipPosition.ToVector3(), finger.Direction.ToVector3(), OnAngle, finger.Length, innerColor);
        Utils.DrawCone(finger.TipPosition.ToVector3(), finger.Direction.ToVector3(), OffAngle, finger.Length, Color.red);
        Debug.DrawRay(finger.TipPosition.ToVector3(), selectedDirection(finger.TipPosition.ToVector3()), Color.grey);
      }
    }
  #endif
  }
  
}
