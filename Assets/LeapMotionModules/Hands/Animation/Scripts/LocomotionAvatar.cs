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

    // Use this for initialization
    void Start() {
      animator = GetComponent<Animator>();
      locomotion = new Locomotion(animator);
      rootDirection = transform.forward;
    }
    void Update() {
      Vector3 flatCamPosition = Camera.main.transform.position;
      flatCamPosition.y = 0;
      Vector3 flatRootPosition = transform.position;
      flatRootPosition.y = 0;
      distanceToRoot = flatCamPosition - flatRootPosition;
      speed = distanceToRoot.magnitude;
      //Debug.Log(speed);

      Vector3 CameraDirection = Camera.main.transform.forward;
      CameraDirection.y = 0.0f;
      Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, transform.forward);
      Vector3 moveDirection = referentialShift *  CameraDirection;

      if (speed > .01f) {
        Vector3 axis = Vector3.Cross(rootDirection, moveDirection);
        direction = Vector3.Angle(rootDirection, moveDirection) / 180f * (axis.y < 0 ? -1 : 1);
      }
      else {
        direction = 0.0f;
      }
      Debug.Log(direction * 180);
     
      if (animator && Camera.main) {
        //JoystickToEvents.Do(transform, Camera.main.transform, ref speed, ref direction);
        //locomotion.Do(speed * 1, direction * 180);

        locomotion.Do(speed * 10f, direction * 180);
      }
    }
  }
}
