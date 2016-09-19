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

    // Use this for initialization
    void Start() {
      animator = GetComponent<Animator>();
      locomotion = new Locomotion(animator);
      rootDirection = transform.forward;
    }
    void Update() {
      rootDirection = transform.forward + transform.position;
      Vector3 flatCamPosition = transform.InverseTransformPoint(Camera.main.transform.position);
      flatCamPosition.y = 0;
      flatCamPosition.x = 0;
      Vector3 flatRootPosition = transform.InverseTransformPoint(transform.position);
      flatRootPosition.y = 0;
      flatRootPosition.x = 0;
      distanceToRoot = flatCamPosition - flatRootPosition;
      speed = distanceToRoot.magnitude;
      float ZForwardSpeed = flatCamPosition.z - flatRootPosition.z;
      if (ZForwardSpeed < 0) {
        speed = 0;
      }
      //Debug.Log("speed: " + speed);
      //Debug.Log("ZForwardSpeed: " + ZForwardSpeed);

      
      // Get camera rotation.    
      Vector3 CameraDirection = transform.InverseTransformDirection( Camera.main.transform.forward);
      CameraDirection.y = 0.0f;
      Quaternion referentialShift = Quaternion.FromToRotation(transform.forward, CameraDirection);
      
      // Convert joystick input in Worldspace coordinates
      Vector3 moveDirection = referentialShift * CameraDirection;

      Vector3 axis = Vector3.Cross(rootDirection, moveDirection);
      direction = Vector3.Angle(rootDirection, moveDirection) / 180f * (axis.y < 0 ? -1 : 1);
      if (speed > .01f) {
        //Debug.Log("Speed > .01: " + speed);
      }
      else {
        speed = 0.0f;
      }
      speed = 0;// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
      //Debug.Log(direction * 180);
      Debug.Log(" = = = = = = = = = = = = = = = = = = = = = = = = =");
      Debug.Log("transform.forward: " + transform.forward + "| CamDirection: " + CameraDirection);
      Debug.Log("referentialShift: " + referentialShift);
      Debug.Log("rootDirection: " + rootDirection + " | moveDirection: " + moveDirection);

      float joySpeed = 0;
      float joyDirection = 0;
      if (animator && Camera.main) {
        JoystickToEvents.Do(transform, Camera.main.transform, ref joySpeed, ref joyDirection);
        if (joyDirection != 0 || joyDirection != 0) {
          //direction = joyDirection;
          speed = joySpeed;
        }

        locomotion.Do(speed * 10f, (direction * 180));
      }
    }

    //void LateUpdate() {
    //  LMRig.position = new Vector3(transform.position.x, LMRig.position.y, transform.position.z);
    //}
  }
}
