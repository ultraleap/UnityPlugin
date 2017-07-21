using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR;
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity {
  public class LocomotionAvatar : MonoBehaviour {
    protected Animator animator;

    private float speed = 0;
    private float averageSpeed = 0;
    private Queue<float> speedList = new Queue<float>(10);
    private float direction = 0;
    private Locomotion locomotion = null;
    private Vector3 moveDirection;
    private Vector3 distanceToRoot;
    private Vector3 rootDirection;
    [HideInInspector]
    public Transform LMRig;
    [HideInInspector]
    public Transform Target;
    [HideInInspector]
    public bool IsCentering = false;

    public bool WalkingEnabled = true;
    public bool crouchEnabled = false;
    public Vector3 BodyCameraOffset;
    private float userHeight = 1.63241f;
    private bool standing = true;

    [Header("For Debugging")] 
    public Transform AnimatorRoot;
    public Transform CameraOnGround;


    void Awake() {
      LMRig = GameObject.FindObjectOfType<LeapHandController>().transform.root;
    }

    void Start() {
      InputTracking.Recenter();
      animator = GetComponent<Animator>();
      locomotion = new Locomotion(animator);
      rootDirection = transform.forward;

      //Creating runtime gizmo target similar to ShoulderTurnBehavior.cs
      GameObject markerPrefab = Resources.Load("RuntimeGizmoMarker") as GameObject;
      Target = GameObject.Instantiate(markerPrefab).transform;
      Target.name = transform.name + "_ChestReferenceMarker";
      Target.parent = GameObject.FindObjectOfType<LeapVRCameraControl>().transform;
      Target.localPosition = new Vector3(0, BodyCameraOffset.y, 2);
      transform.Translate(0f, BodyCameraOffset.y, 0f, transform);

    }

    void Update() {
      if (WalkingEnabled) {
        AnimatorLocomotion();
      }
    }
   
    void OnAnimatorIK() {
      //Floating crouch
      if (crouchEnabled && !WalkingEnabled) {
        float heightOffset = 0;
        if (Camera.main.transform.position.y < userHeight) {
          heightOffset = Camera.main.transform.position.y - userHeight;
          animator.transform.position = new Vector3(animator.transform.position.x, heightOffset + BodyCameraOffset.y, animator.transform.position.z);
        }
      }
      Vector3 placeAnimatorUnderCam = new Vector3(Camera.main.transform.position.x, transform.position.y, Camera.main.transform.position.z);

      if (IsCentering || !WalkingEnabled) {
        animator.transform.position = Vector3.Lerp(animator.rootPosition, placeAnimatorUnderCam, .03f);
        animator.transform.Translate(BodyCameraOffset.x, 0f, BodyCameraOffset.z, animator.transform);
        var lookPos = Target.position - transform.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        animator.transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 1.5f);
      }
    }

    void AnimatorLocomotion() {
      float reverse = 1;
      Vector3 flatCamPosition = Camera.main.transform.position;
      flatCamPosition.y = 0;
      Vector3 flatRootPosition = transform.position;
      flatRootPosition.y = 0;
      distanceToRoot = flatCamPosition - flatRootPosition;
      speed = distanceToRoot.magnitude;

      if (speedList.Count >= 10) {
        speedList.Dequeue();
      }
      if (speedList.Count < 10) {
        speedList.Enqueue(speed);
      }
      averageSpeed = 0;
      foreach (float s in speedList) {
        averageSpeed += s;
      }
      averageSpeed = (averageSpeed / 10);
      
      if (!standing && averageSpeed < .15f) {
        standing = true;
      }
      if (standing && averageSpeed > .30) {
        standing = false;
      }
      if (standing) { //Dead "stick" and matching LMRigLococmotion method for turning in place
        averageSpeed = 0.0f;
        moveDirection = MoveDirectionCameraDirection();
      }
      else {
        moveDirection = MoveDirectionTowardCamera();
      }

      //Vector3 moveDirection = referentialShift * CameraDirection;
      Vector3 axis = Vector3.Cross(rootDirection, moveDirection);
      direction = Vector3.Angle(rootDirection, moveDirection) / 180f * (axis.y < 0 ? -1 : 1);
      if (animator && Camera.main && WalkingEnabled) {
        locomotion.Do(averageSpeed * 1.25f, (direction * 180), reverse);
        Debug.DrawLine(transform.position, moveDirection * 2, Color.red);
      }
    }

    Vector3 MoveDirectionCameraDirection() {
      // Get camera rotation.
      rootDirection = transform.forward;// +transform.position;
      Vector3 CameraDirection = Camera.main.transform.forward;
      CameraDirection.y = 0.0f;
      return CameraDirection;
    }

    Vector3 MoveDirectionTowardCamera() {
      // Get camera rotation.
      rootDirection = transform.forward;// +transform.position;
      Vector3 DirectionToCamera = Camera.main.transform.position - transform.position;
      DirectionToCamera.y = 0.0f;
      Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, DirectionToCamera);
      // Convert joystick input in Worldspace coordinates
      return DirectionToCamera;
    }

    //Called by Mecanim StateMachineBehavior
    public void CenterUnderCamera() {
      animator.transform.position = new Vector3(Camera.main.transform.position.x, transform.position.y, Camera.main.transform.position.z);
    }
  }
}
