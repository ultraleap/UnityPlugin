using UnityEngine;
using Leap.Unity;
using Leap.Unity.Query;
using System.Collections.Generic;

namespace Leap.Unity.UI {

  public struct DepenetrationRay {
    public Vector3 position;
    public Vector3 direction;
  }

  public class BoxDepenetrator : MonoBehaviour {

    public SphereCollider sphere;

    [Header("Automatic, searches this and children")]
    public List<BoxCollider> boxColliders;

    private MinimalBody body;

    void Start() {

      // TODO: the Query()/Concat here is test code. Replace with good compound collider logic.
      boxColliders = new List<BoxCollider>();
      GetComponents<BoxCollider>(boxColliders);

      List<BoxCollider> inChildren = new List<BoxCollider>();
      GetComponentsInChildren<BoxCollider>(inChildren);

      boxColliders = boxColliders.Query().Concat(inChildren.Query()).ToList();
      // end TODO

      body = GetComponent<MinimalBody>();
    }

    void Update() {
      DepenetrationRay shortestMagnitudeDepenetration = default(DepenetrationRay);
      bool requiresDepenetration = false;
      DepenetrationRay testRay;
      foreach (var box in boxColliders) {
        if (Depenetrate(sphere, box, out testRay)) {
          if (!requiresDepenetration || testRay.direction.magnitude < shortestMagnitudeDepenetration.direction.magnitude) {
            shortestMagnitudeDepenetration = testRay;
            requiresDepenetration = true;
          }
        }
      }

      if (requiresDepenetration) {
        DepenetrationRay depenetrationRay = shortestMagnitudeDepenetration;

        //How Hard the depenetration is
        float hardness = 0.5f;
        if (body) {
          if (!body.lockRotation) {
            ConstraintsUtil.ConstrainToPoint(transform, depenetrationRay.position, depenetrationRay.position + depenetrationRay.direction, hardness);
          }
          else if (body.lockRotation && !body.lockPosition) {
            transform.position += depenetrationRay.direction * hardness;
          }
        }
      }
    }

    public static bool Depenetrate(SphereCollider sphere, BoxCollider box, out DepenetrationRay depenetrationRay) {
      Vector3 spherePosition = sphere.transform.position;
      float sphereRadius = (sphere.radius * sphere.transform.lossyScale.x);
      return Depenetrate(spherePosition, sphereRadius, box, out depenetrationRay);
    }

    public static bool Depenetrate(Vector3 spherePosition, float sphereRadius, BoxCollider box, out DepenetrationRay depenetrationRay) {
      Vector3 nearestBoxPoint = box.transform.TransformPoint(box.closestPointOnSurface(box.transform.InverseTransformPoint(spherePosition)));
      Vector3 boxPointToSphereCenter = spherePosition - nearestBoxPoint;
      bool requiresDepenetration = boxPointToSphereCenter.sqrMagnitude < (sphereRadius * sphereRadius);

      if (requiresDepenetration) {
        bool sphereCenterInsideBox = box.isPointInside(box.transform.InverseTransformPoint(spherePosition));
        depenetrationRay = new DepenetrationRay {
          position = nearestBoxPoint,
          direction = boxPointToSphereCenter + ((sphereCenterInsideBox ? 1F : -1F) * boxPointToSphereCenter.normalized) * sphereRadius
        };
        return true;
      }
      else {
        depenetrationRay = default(DepenetrationRay);
        return false;
      }
    }

  }

}