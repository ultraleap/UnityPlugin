using UnityEngine;
using System.Collections;
using Leap.Unity;

namespace Leap.Unity {
  public class WristLeapToIKBlend : HandTransitionBehavior {
    public Animator animator;
    public Transform ElbowIKTarget;
    private HandModel handModel;
    private Vector3 armDirection;
    public float ElbowOffset;

    private Vector3 startingPalmPosition;
    private Quaternion startingOrientation;
    private Transform palm;

    public Vector3 PalmPositionAtLateUpdate;
    private Quaternion PalmRotationAtLateUpdate;
    public Chirality Handedness;
    private float positionIKWeight;
    private float rotationIKWeight;

    public Transform Shoulder_L;
    public Transform Shoulder_R;


    protected override void Awake() {
      base.Awake();
      animator = transform.root.GetComponentInChildren<Animator>();
      handModel = transform.GetComponent<HandModel>();
      palm = GetComponent<HandModel>().palm;
      startingPalmPosition = palm.localPosition;
      startingOrientation = palm.localRotation;
    }

    protected override void HandFinish() {
      StartCoroutine(LerpToStart());
      positionIKWeight = 0;
      rotationIKWeight = 0;
    }
    protected override void HandReset() {
      StopAllCoroutines();
      positionIKWeight = 1;
      rotationIKWeight = 1;
    }
    void Update() {
      //get Arm Directions and set elbow target position
      armDirection = handModel.GetArmDirection();
      ElbowIKTarget.position = palm.position + (armDirection * ElbowOffset);
    }

    void LateUpdate() {
      PalmPositionAtLateUpdate = palm.position;
      PalmRotationAtLateUpdate = palm.rotation;
    }

    public void OnAnimatorIK(int layerIndex) {
      //Debug.Log("IK");
      if (Handedness == Chirality.Left) {
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, positionIKWeight);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, PalmPositionAtLateUpdate);
        //Debug.Log(palm.position);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, rotationIKWeight);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, PalmRotationAtLateUpdate);
        animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, positionIKWeight);
        animator.SetIKHintPosition(AvatarIKHint.LeftElbow, ElbowIKTarget.position);
        if (ElbowIKTarget.position.y >= Shoulder_L.position.y) {
          animator.SetFloat("shoulder_up_left", 2.0f);
        }
        else animator.SetFloat("shoulder_up_left", 0.0f);
      }
      //Debug.Log("Behaviour Frame: " + Time.frameCount);
      if (Handedness == Chirality.Right) {
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, positionIKWeight);
        animator.SetIKPosition(AvatarIKGoal.RightHand, PalmPositionAtLateUpdate);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rotationIKWeight);
        animator.SetIKRotation(AvatarIKGoal.RightHand, PalmRotationAtLateUpdate);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, positionIKWeight);
        animator.SetIKHintPosition(AvatarIKHint.RightElbow, ElbowIKTarget.position);
        if (ElbowIKTarget.position.y >= Shoulder_R.position.y) {
          animator.SetFloat("shoulder_up_right", 2.0f);
        }
        else animator.SetFloat("shoulder_up_right", 0.0f);
      }
    }
    private IEnumerator LerpPositionIKWeight(float destinationWeight, float duration) {
      return null;
    }
    private IEnumerator LerpToStart() {
      Vector3 droppedPosition = palm.localPosition;
      Quaternion droppedOrientation = palm.localRotation;
      float duration = .25f;
      float startTime = Time.time;
      float endTime = startTime + duration;

      while (Time.time <= endTime) {
        float t = (Time.time - startTime) / duration;
        palm.localPosition = Vector3.Lerp(droppedPosition, startingPalmPosition, t);
        palm.localRotation = Quaternion.Lerp(droppedOrientation, startingOrientation, t);
        yield return null;
      }
    }
    void OnValidate() {
      Handedness = GetComponent<IHandModel>().Handedness;
    }
  }
}
