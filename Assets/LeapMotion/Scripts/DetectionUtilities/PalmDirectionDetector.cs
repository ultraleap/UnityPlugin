using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {

  /**
  * Detects whether the palm of the hnad is pointing toward the specified direction.
  * The detector activates when the palm direction is within OnAngle degrees of the
  * desired direction and deactivates when it becomes more than OffAngle degrees.
  *
  * 
  */
  public class PalmDirectionDetector : Detector {
    [Tooltip("The interval in seconds at which to check this detector's conditions.")]
    public float Period = .1f; //seconds
    [Tooltip("The hand model to watch. Set automatically if detector is on a hand.")]
    public IHandModel handModel = null;
    [Tooltip("The target direction.")]
    public Vector3 PointingDirection = Vector3.forward;
    [Tooltip("How to treat the target direction.")]
    public PointingType PointingType = PointingType.RelativeToHorizon;
    [Tooltip("A target object(optional). Use PointingType.AtTarget")]
    public Transform TargetObject = null;
    [Tooltip("The angle in degrees from the target direction at which to turn on.")]
    [Range(0, 360)]
    public float OnAngle = 45; // degrees
    [Tooltip("The angle in degrees from the target direction at which to turn off.")]
    [Range(0, 360)]
    public float OffAngle = 65; //degrees

    private void OnValidate(){
      if( OffAngle < OnAngle){
        OffAngle = OnAngle;
      }
    }

    private void Start () {
      if(handModel == null){
        handModel = gameObject.GetComponentInParent<IHandModel>();
      }
    }

    private void OnEnable () {
      StartCoroutine(palmWatcher());
    }

    private void OnDisable () {
      StopCoroutine(palmWatcher());
    }

    private IEnumerator palmWatcher() {
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

    #if UNITY_EDITOR
    private void OnDrawGizmos(){
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
  }
}