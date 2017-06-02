/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Attachments {

  /**
  * The CameraFollower component controls the rotation of its parent game object so that
  * object always faces the main camera.
  * @since 4.1.1
  */
  public class CameraFollower : MonoBehaviour {

    /**
    * The vector representing the object's forward direction in its local coordinate system.
    * @since 4.1.1
    */
    [Tooltip("The object's forward direction")]
    public Vector3 objectForward = Vector3.forward;

    /**
    * An easing curve for changing the rotation.
    * @since 4.1.1
    */
    [Tooltip("Easing curve for animating the object rotation changes")]
    public AnimationCurve Ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    /**
    * The speed at which the follower tracks the camera.
    * @since 4.1.1
    */
    [Tooltip("Camera tracking rate")]
    [Range(1, 20)]
    public float Speed = 10;

    /**
    * Whether to prevent rotation around the x-axis.
    * @since 4.1.1
    */
    [Tooltip("Freeze rotation around the x-axis")]
    public bool FreezeX = false;

    /**
    * Whether to prevent rotation around the y-axis.
    * @since 4.1.1
    */
    [Tooltip("Freeze rotation around the y-axis")]
    public bool FreezeY = false;

    /**
    * Whether to prevent rotation around the z-axis.
    * @since 4.1.1
    */
    [Tooltip("Freeze rotation around the z-axis")]
    public bool FreezeZ = false;
  
    private Quaternion offset;
    private Quaternion startingLocalRotation;
  
    private void Awake(){
      offset = Quaternion.Inverse(Quaternion.LookRotation(objectForward));
      startingLocalRotation = transform.localRotation;
    }
  
    private void Update () {
      Vector3 cameraDirection = (Camera.main.transform.position - transform.position).normalized;
      Vector3 objectFacing = transform.TransformDirection(objectForward);
      float deltaAngle = Vector3.Angle(objectFacing, cameraDirection);
      float easing = Ease.Evaluate(Speed * deltaAngle / 360);
      Quaternion towardCamera = Quaternion.LookRotation(cameraDirection);
      towardCamera *= offset;
      transform.rotation = Quaternion.Slerp(transform.rotation, towardCamera, easing);
      Vector3 startingEulers = startingLocalRotation.eulerAngles;
      Vector3 targetEulers = transform.localEulerAngles;
      float angleX, angleY, angleZ;
      if(FreezeX){
        angleX = startingEulers.x;
      } else {
        angleX = targetEulers.x;
      }
      if(FreezeY){
        angleY = startingEulers.y;
      } else {
        angleY = targetEulers.y;
      }
      if(FreezeZ){
        angleZ = startingEulers.z;
      } else {
        angleZ = targetEulers.z;
      }
      transform.localEulerAngles = new Vector3(angleX, angleY, angleZ);
    }
  }
}
