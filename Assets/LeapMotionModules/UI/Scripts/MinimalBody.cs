using Leap.Unity;
using Leap.Unity.Attributes;
using UnityEngine;

[ExecuteAfter(typeof(LeapServiceProvider))]
[ExecuteAfter(typeof(SpringBase))]
public class MinimalBody : MonoBehaviour {

  public bool lockPosition = false;
  public bool lockRotation = false;

  [MinValue(0F)]
  [MaxValue(100F)]
  public float linearDampingPercent = 0.0f;

  [MinValue(0F)]
  [MaxValue(100F)]
  public float angularDampingPercent = 0.0f;

  Vector3 prevPosition;
  Quaternion prevRotation;
  float prevDeltaTime;

  void Start() {
    prevDeltaTime = 0.001f;
    prevPosition = transform.localPosition;
    prevRotation = transform.localRotation;
  }

  void Update() {
    //Grab State from Previous Frame
    float tempDeltaTime = Time.deltaTime;

    //Integrate Position
    if (lockPosition) {
      transform.localPosition = prevPosition;
    } else {
      Vector3 tempPos = transform.localPosition;
      transform.localPosition += (transform.localPosition - prevPosition) * (Time.deltaTime / prevDeltaTime) * (1f - (linearDampingPercent / 100F));
      prevPosition = tempPos;
    }
    //Integrate Rotation
    if (lockRotation) {
      transform.localRotation = prevRotation;
    } else {
      Quaternion tempRot = transform.localRotation;
      float angle; Vector3 axis;
      (transform.localRotation * Quaternion.Inverse(prevRotation)).ToAngleAxis(out angle, out axis);
      transform.localRotation = Quaternion.AngleAxis(angle * (Time.deltaTime / prevDeltaTime) * (1f - (angularDampingPercent/100F)), axis) * transform.localRotation;
      prevRotation = tempRot;
    }

    //Store State from Previous Frame
    prevDeltaTime = tempDeltaTime;
  }
}