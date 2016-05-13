using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {
  [ExecuteInEditMode]
  public class PalmDirectionDetector : Detector {

    public Vector3 PointingDirection = Vector3.forward;
    public PointingType PointingType = PointingType.RelativeToHorizon;
    public Transform TargetObject = null;
    public float OnAngle = 45; // degrees
    public float OffAngle = 65; //degrees
    
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
  
    #if UNITY_EDITOR
    void OnDrawGizmos(){
      if(ShowGizmos && handModel != null){
        Color centerColor;
        if (IsActive) {
          centerColor = Color.green;
        } else {
          centerColor = Color.red;
        }
        Hand hand = handModel.GetLeapHand();
        Utils.DrawCone(hand.PalmPosition.ToVector3(), hand.PalmNormal.ToVector3(), OnAngle, hand.PalmWidth, centerColor, 8);
        Utils.DrawCone(hand.PalmPosition.ToVector3(), hand.PalmNormal.ToVector3(), OffAngle, hand.PalmWidth, Color.blue, 8);
        Debug.DrawRay(hand.PalmPosition.ToVector3(), selectedDirection(hand.PalmPosition.ToVector3()), Color.grey, 0, true);
      }
    }
    #endif

    IEnumerator palmWatcher() {
      Hand hand;
      Vector3 normal;
      while(true){
        if(handModel != null){
          hand = handModel.GetLeapHand();
          if(hand != null){
            normal = hand.PalmNormal.ToVector3();
            float angleTo = Vector3.Angle(normal, selectedDirection(hand.PalmPosition.ToVector3()));
            if(angleTo <= OnAngle){
              Activate();
            } else if(angleTo > OffAngle) {
              Deactivate();
            }
          }
        }
        yield return new WaitForSeconds(Period);
      }
    }

    private Vector3 selectedDirection (Vector3 tipPosition) {
      switch (PointingType) {
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
  }
}