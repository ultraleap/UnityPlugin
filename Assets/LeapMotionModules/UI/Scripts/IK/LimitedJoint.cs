using UnityEngine;

public class LimitedJoint : MonoBehaviour {
  public Transform target;
  public Transform handle;
  public float minDotProduct;
  public bool lockBase = false;

  void Update() {
    Debug.DrawLine(handle.parent.position, handle.parent.position + handle.parent.rotation * (new Vector3(0f, 1f, -1f)*0.4f));
    Debug.DrawLine(handle.parent.position, handle.parent.position + handle.parent.rotation * (new Vector3(0f, 1f, 1f)*0.4f));

    //Initialize Point to Constrain
    handle.position = target.position;

    //Constrain To Plane
    //handle.localPosition = Vector3.ProjectOnPlane(handle.localPosition, Vector3.right);
    //Constrain Distance
    handle.localPosition = handle.localPosition.ConstrainDistance(Vector3.zero, 0.5f);
    //Constrain Angle
    handle.localPosition = handle.localPosition.ConstrainToNormal(Vector3.up, minDotProduct);

    //Propagate the leftover displacement down the chain
    //if (!lockBase) { transform.ConstrainToPoint(handle.position, target.position); }
  }
}