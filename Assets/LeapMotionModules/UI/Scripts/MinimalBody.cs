using UnityEngine;

public class MinimalBody : MonoBehaviour {
  Vector3 prevPosition; Quaternion prevRotation; float prevDeltaTime;

  void Start() {
    prevDeltaTime = 0.001f;
  }

  void Update() {
    //Grab State from Previous Frame
    Vector3 tempPos = transform.position;
    Quaternion tempRot = transform.rotation;
    float tempDeltaTime = Time.deltaTime;

    //Integrate Position
    transform.position += (transform.position - prevPosition) * (Time.deltaTime / prevDeltaTime);
    //Integrate Rotation
    float angle; Vector3 axis;
    (transform.rotation * Quaternion.Inverse(prevRotation)).ToAngleAxis(out angle, out axis);
    transform.rotation = Quaternion.AngleAxis(angle * (Time.deltaTime / prevDeltaTime), axis) * transform.rotation;

    //Store State from Previous Frame
    prevPosition = tempPos;
    prevRotation = tempRot;
    prevDeltaTime = tempDeltaTime;
  }
}