using UnityEngine;
using System.Collections;

namespace Leap.Unity {
  public class LocomotionAvatar : MonoBehaviour {
    protected Animator animator;

    private float speed = 0;
    private float direction = 0;
    private Locomotion locomotion = null;
    
    private Vector3 distanceToRoot;
    private Vector3 rootDirection;
    public Transform LMRig;
    public bool LMRigToFollowAnimator;

    void Awake() {
      LMRig = GameObject.FindObjectOfType<LeapHandController>().transform.root;
    }
    
    void Start() {
      animator = GetComponent<Animator>();
      locomotion = new Locomotion(animator);
      rootDirection = transform.forward;
    }

    void Update() {
      if (LMRigToFollowAnimator == true) {
        LMRigLococmotion();
      }
      else AnimatorLocomotion();
    }
    void LMRigLococmotion() {
      //Requires positioning of LMHeadMountedRig OnAnimatorIK()
      Vector3 flatCamPosition = transform.InverseTransformPoint(Camera.main.transform.position);
      flatCamPosition.y = 0;
      Vector3 flatRootPosition = transform.InverseTransformPoint(transform.position);
      flatRootPosition.y = 0;
      distanceToRoot = flatCamPosition - flatRootPosition;
      speed = distanceToRoot.magnitude;

      // Convert joystick input in Worldspace coordinates
      Vector3 moveDirection = MoveDirectionCameraDirection();
      //Vector3 moveDirection = referentialShift * CameraDirection;
      Vector3 axis = Vector3.Cross(rootDirection, moveDirection);
      direction = Vector3.Angle(rootDirection, moveDirection) / 180f * (axis.y < 0 ? -1 : 1);
      if (speed > .01f) { //Dead "stick"
      }
      else {
        speed = 0.0f;
      }
      float joySpeed = 0;
      float joyDirection = 0;
      if (animator && Camera.main) {
        JoystickToEvents.Do(transform, Camera.main.transform, ref joySpeed, ref joyDirection);
        if (joyDirection != 0 || joyDirection != 0) {
          //direction += joyDirection;
          speed += joySpeed;
        }
        locomotion.Do(speed * 2 , (direction * 180 ), 1);
      }
    }
    Vector3 MoveDirectionCameraDirection() {
      // Get camera rotation.
      rootDirection = transform.forward;// +transform.position;
      Vector3 CameraDirection = Camera.main.transform.forward;
      CameraDirection.y = 0.0f;
      //Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, CameraDirection);
      return CameraDirection;
    }
    Vector3 MoveDirectionTowardCamera () {
        // Get camera rotation.
        rootDirection = transform.forward;// +transform.position;
        Vector3 DirectionToCamera = Camera.main.transform.position - transform.position;
        DirectionToCamera.y = 0.0f;
        Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, DirectionToCamera);
        // Convert joystick input in Worldspace coordinates
        return DirectionToCamera;
    }

    void AnimatorLocomotion() {
      float reverse = 1;
      Vector3 moveDirection;
      Vector3 flatCamPosition = Camera.main.transform.position;
      flatCamPosition.y = 0;
      Vector3 flatRootPosition = transform.position;
      flatRootPosition.y = 0;
      distanceToRoot = flatCamPosition - flatRootPosition;
      speed = distanceToRoot.magnitude;
      Debug.Log(transform.InverseTransformPoint( Camera.main.transform.position));
      if (speed < .1f) { //Dead "stick" and matching LMRigLococmotion method for turning in place
        speed = 0.0f;
        moveDirection = MoveDirectionCameraDirection();
      }

      else {
        Debug.Log("Moving");
        moveDirection = MoveDirectionTowardCamera();
        if (transform.InverseTransformPoint(Camera.main.transform.position).z < -.2f) {
          Debug.Log("Reversing");
          moveDirection = MoveDirectionCameraDirection();
          reverse = -1;
        }
      }

      //Vector3 moveDirection = referentialShift * CameraDirection;
      Vector3 axis = Vector3.Cross(rootDirection, moveDirection);
      direction = Vector3.Angle(rootDirection, moveDirection) / 180f * (axis.y < 0 ? -1 : 1);

      if (animator && Camera.main) {
        locomotion.Do(speed, (direction * 180), reverse);
        Debug.DrawLine(transform.position, moveDirection * 2, Color.red);
      }
    }

    void OnAnimatorIK() {
      if (LMRigToFollowAnimator) {
        LMRig.position = new Vector3(transform.position.x, LMRig.position.y, transform.position.z);
      }
    }
  }
}
