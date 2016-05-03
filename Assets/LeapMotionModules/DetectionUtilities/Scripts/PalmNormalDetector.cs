using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;

namespace Leap.Unity.DetectionUtilities {
  [ExecuteInEditMode]
  public class PalmNormalDetector : BinaryDetector {
  
    public Directions TargetDirection = Directions.Backward;
    public Vector3 CustomDirection = Vector3.back;
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
      if(handModel != null){
        Hand hand = handModel.GetLeapHand();
        this.DrawCone(hand.PalmPosition.ToVector3(), hand.PalmNormal.ToVector3(), OnAngle, hand.PalmWidth, Color.blue, 8);
        this.DrawCone(hand.PalmPosition.ToVector3(), hand.PalmNormal.ToVector3(), OffAngle, hand.PalmWidth, Color.red, 8);
        Debug.DrawRay(hand.PalmPosition.ToVector3(), selectedDirection(), Color.green, 0, true);
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
            //Vector3 lookForward = Camera.main.transform.forward;
            float angleTo = Vector3.Angle(normal, selectedDirection());
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

    private Vector3 selectedDirection(){
      switch(TargetDirection){
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