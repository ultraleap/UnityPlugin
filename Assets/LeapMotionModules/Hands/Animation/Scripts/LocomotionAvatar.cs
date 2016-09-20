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

    void Awake() {
      LMRig = GameObject.FindObjectOfType<LeapHandController>().transform.root;
    }
    
    void Start() {
      animator = GetComponent<Animator>();
      locomotion = new Locomotion(animator);
      rootDirection = transform.forward;
    }
    void Update() {
      Vector3 flatCamPosition = transform.InverseTransformPoint(Camera.main.transform.position);
      flatCamPosition.y = 0;
      Vector3 flatRootPosition = transform.InverseTransformPoint(transform.position);
      flatRootPosition.y = 0;
      distanceToRoot = flatCamPosition - flatRootPosition;
      speed = distanceToRoot.magnitude;

      // Get camera rotation.
      rootDirection = transform.forward;// +transform.position;
      Vector3 CameraDirection = Camera.main.transform.forward;
      CameraDirection.y = 0.0f;
      Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, CameraDirection);
      
      // Convert joystick input in Worldspace coordinates
      Vector3 moveDirection = CameraDirection;
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
        locomotion.Do(speed * 2 , (direction * 180 ));
      }
    }

    void OnAnimatorIK() {
      LMRig.position = new Vector3(transform.position.x, LMRig.position.y, transform.position.z);
    }
  }
}
