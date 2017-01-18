using UnityEngine;
using Leap.Unity;

[ExecuteBefore(typeof(MinimalBody))]
public class BoxDepenetrator : MonoBehaviour {

  public SphereCollider sphere;

  [Header("Automatic if null (this)")]
  public BoxCollider box;

  public struct DepenetrationRay {
    public Vector3 position;
    public Vector3 direction;
  }
  
  private DepenetrationRay _depenetrationRay;
  private bool _requiresDepenetrationThisFrame = false;
  private MinimalBody body;

  void Start() {
    if (box == null) box = GetComponent<BoxCollider>();
    if (box == null) {
      Debug.LogError("No box found. Please attach the desired box collider (should be in this or a child).");
    }
    body = GetComponent<MinimalBody>();
  }

  void Update() {
    Vector3 nearestBoxPoint = box.transform.TransformPoint(box.closestPointOnSurface(box.transform.InverseTransformPoint(sphere.transform.position)));
    Vector3 boxPointToSphereCenter = sphere.transform.position - nearestBoxPoint;
    _requiresDepenetrationThisFrame = boxPointToSphereCenter.sqrMagnitude < (sphere.radius * sphere.transform.localScale.x) * (sphere.radius * sphere.transform.localScale.x);

    if (_requiresDepenetrationThisFrame) {
      bool sphereCenterInsideBox = box.isPointInside(box.transform.InverseTransformPoint(sphere.transform.position));
      _depenetrationRay = new DepenetrationRay();
      _depenetrationRay.position    = nearestBoxPoint;
      _depenetrationRay.direction   = boxPointToSphereCenter + ((sphereCenterInsideBox ? 1F : -1F) * boxPointToSphereCenter.normalized) * (sphere.radius * sphere.transform.localScale.x);

      //How Hard the depenetration is
      float hardness = 0.1f;
      if (body) {
        if(!body.lockRotation) {
          ConstraintsUtil.ConstrainToPoint(transform, _depenetrationRay.position, _depenetrationRay.position + _depenetrationRay.direction, hardness);
        }else if (body.lockRotation && !body.lockPosition) {
          transform.position += _depenetrationRay.direction * hardness;
        }
      }
    }
  }

  //void OnDrawGizmos() {
  //  if (_requiresDepenetrationThisFrame) {
  //    Gizmos.color = Color.red;
  //    Gizmos.DrawSphere(_depenetrationRay.position + _depenetrationRay.direction, 0.002F);
  //    Gizmos.DrawRay(_depenetrationRay.position, _depenetrationRay.direction);
  //  }
  //}

}
