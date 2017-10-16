using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Leap.Unity;

public class CenterOnIdle : StateMachineBehaviour {
  private bool hasRun;
  float startTime;
  float elapsedTime = 0;
  Vector3 startPos;
  float percentComplete = 0;

  private LocomotionAvatar locoMotionAvatar;
  // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
  override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
    locoMotionAvatar = animator.transform.GetComponent<LocomotionAvatar>();
    startTime = 0;
    hasRun = false;
    startPos = animator.transform.position;
    percentComplete = 0;
  }

  //OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
  override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
    if (locoMotionAvatar != null) {
      elapsedTime += Time.deltaTime;
      Vector3 flatCamPosition = Camera.main.transform.position;
      flatCamPosition.y = 0;
      Vector3 flatRootPosition = animator.transform.position;
      flatRootPosition.y = 0;
      Vector3 distanceToRoot = flatCamPosition - flatRootPosition;

      if (!hasRun && elapsedTime > 1.5f && distanceToRoot.magnitude > .05f) {
        //Vector3 placeAnimatorUnderCam = new Vector3(Camera.main.transform.position.x, animator.transform.position.y, Camera.main.transform.position.z);
        //animator.transform.position = Vector3.Lerp(startPos, placeAnimatorUnderCam, percentComplete);
        //percentComplete += .1f;
        locoMotionAvatar.IsCentering = true;
      }
      else if (distanceToRoot.magnitude < .10f) {
        locoMotionAvatar.IsCentering = false;
        hasRun = true;
      }
    }
  }

  // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
  override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
    elapsedTime = 0f;
    startTime = 0;
    hasRun = false;
    startPos = animator.transform.position;
    percentComplete = 0;
  }

  // OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
  override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

  }

  // OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
  override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

  }
}
