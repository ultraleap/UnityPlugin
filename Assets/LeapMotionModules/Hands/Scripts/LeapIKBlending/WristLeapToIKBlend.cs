using UnityEngine;
using System.Collections;
using Leap.Unity;

namespace Leap.Unity {
  public class WristLeapToIKBlend : HandTransitionBehavior {
    public Animator animator;


    private Vector3 startingPalmPosition;
    private Quaternion startingOrientation;
    private Transform palm;

    protected override void Awake() {


      base.Awake();
      animator = transform.root.GetComponentInChildren<Animator>();
      palm = GetComponent<HandModel>().palm;
      startingPalmPosition = palm.localPosition;
      startingOrientation = palm.localRotation;
    }

    protected override void HandFinish() {
      StartCoroutine(LerpToStart());
    }
    protected override void HandReset() {
      StopAllCoroutines();
    }

    public void OnAnimatorIK(int layerIndex) {
      Debug.Log("IK");
      animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
      animator.SetIKPosition(AvatarIKGoal.LeftHand, palm.position);
      //Debug.Log(palm.position);
      animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
      animator.SetIKRotation(AvatarIKGoal.LeftHand, palm.rotation);

      animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
      animator.SetIKPosition(AvatarIKGoal.RightHand, palm.position);
      animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
      animator.SetIKRotation(AvatarIKGoal.RightHand, palm.rotation);
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
  }
}
