using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformTool : MonoBehaviour {

  public GameObject targetObject;

  private struct PositionRotationOffset {
    public Vector3 pos; public Quaternion rot;
  }

  public void MoveTargetPosition(Vector3 deltaPosition) {
    this.transform.position += deltaPosition;
    targetObject.transform.position += deltaPosition;
  }

  public void MoveTargetRotation(Quaternion deltaRotation) {
    // tool maintains absolute orientation
    targetObject.transform.rotation = deltaRotation * targetObject.transform.rotation;
  }

}
