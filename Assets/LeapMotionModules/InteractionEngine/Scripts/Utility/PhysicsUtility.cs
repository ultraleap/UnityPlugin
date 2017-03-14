using UnityEngine;
using System;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  public static class PhysicsUtility {

    public struct SoftContact {
      public Rigidbody body;
      public Vector3 position;
      public Vector3 normal;
      public Vector3 velocity;
      public Matrix4x4 invWorldInertiaTensor;
    }

    public struct Velocities {
      public Vector3 velocity;
      public Vector3 angularVelocity;
      public Matrix4x4 invWorldInertiaTensor;
    }

    public static Vector3 ToLinearVelocity(Vector3 deltaPosition, float deltaTime) {
      return deltaPosition / deltaTime;
    }

    public static Vector3 ToLinearVelocity(Vector3 startPosition, Vector3 destinationPosition, float deltaTime) {
      return ToLinearVelocity(destinationPosition - startPosition, deltaTime);
    }

    public static Vector3 ToAngularVelocity(Quaternion deltaRotation, float deltaTime) {
      Vector3 deltaAxis;
      float deltaAngle;
      deltaRotation.ToAngleAxis(out deltaAngle, out deltaAxis);

      if (float.IsInfinity(deltaAxis.x)) {
        deltaAxis = Vector3.zero;
        deltaAngle = 0;
      }

      if (deltaAngle > 180) {
        deltaAngle -= 360.0f;
      }

      return deltaAxis * deltaAngle * Mathf.Deg2Rad / deltaTime;
    }

    public static Vector3 ToAngularVelocity(Quaternion startRotation, Quaternion destinationRotation, float deltaTime) {
      return ToAngularVelocity(destinationRotation * Quaternion.Inverse(startRotation), deltaTime);
    }

    public static bool IsValid(this Vector3 v) {
      return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)) && !(float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
    }

    public static bool generateSphereContacts(Vector3 spherePosition, float sphereRadius, Vector3 sphereVelocity, int layerMask, ref List<SoftContact> softContacts, ref Dictionary<Rigidbody, Velocities> originalVelocities, ref Collider[] temporaryColliderSwapSpace) {
      Array.Clear(temporaryColliderSwapSpace, 0, temporaryColliderSwapSpace.Length);
      Physics.OverlapSphereNonAlloc(spherePosition, sphereRadius, temporaryColliderSwapSpace, layerMask);

      bool contacting = false;
      foreach (Collider col in temporaryColliderSwapSpace) {
        if (col != null && col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic) {
          SoftContact contact = new SoftContact();
          contact.body = col.attachedRigidbody;

          //Store this rigidbody's pre-contact velocities and inverse world inertia tensor
          Velocities originalBodyVelocities;
          if (!originalVelocities.TryGetValue(contact.body, out originalBodyVelocities)) {
            originalBodyVelocities = new Velocities();
            originalBodyVelocities.velocity = contact.body.velocity;
            originalBodyVelocities.angularVelocity = contact.body.angularVelocity;

            Matrix4x4 tensorRotation = Matrix4x4.TRS(Vector3.zero, contact.body.rotation * contact.body.inertiaTensorRotation, Vector3.one);
            originalBodyVelocities.invWorldInertiaTensor = (tensorRotation * Matrix4x4.Scale(contact.body.inertiaTensor) * tensorRotation.inverse).inverse;

            originalVelocities.Add(contact.body, originalBodyVelocities);
          }

          if (col is MeshCollider) {
            contact.normal = (contact.body.worldCenterOfMass - spherePosition).normalized;
          } else {
            Vector3 objectLocalBoneCenter = col.transform.InverseTransformPoint(spherePosition);
            Vector3 objectPoint = col.transform.TransformPoint(col.ClosestPointOnSurface(objectLocalBoneCenter));
            contact.normal = (objectPoint - spherePosition).normalized * (col.IsPointInside(objectLocalBoneCenter) ? -1f : 1f);
          }
          contact.position = spherePosition + (contact.normal * sphereRadius);
          contact.velocity = sphereVelocity;
          contact.invWorldInertiaTensor = originalBodyVelocities.invWorldInertiaTensor;

          softContacts.Add(contact);
          contacting = true;
        }
      }
      return contacting;
    }

    static void applySoftContact(Rigidbody thisObject, Vector3 handContactPoint, Vector3 handVelocityAtContactPoint, Vector3 normal, Matrix4x4 invWorldInertiaTensor) {
      //Determine the relative velocity between the soft contactor and the object at the contact point
      Vector3 objectVelocityAtContactPoint = thisObject.GetPointVelocity(handContactPoint);
      Vector3 velocityDifference = objectVelocityAtContactPoint - handVelocityAtContactPoint;
      float relativeVelocity = Vector3.Dot(normal, velocityDifference);
      if (relativeVelocity > float.Epsilon)
        return;

      // Define a cone of friction where the direction of velocity relative to the surface
      // controls the degree to which perpendicular movement (relative to the surface) is
      // resisted.  Smoothly blending in friction prevents vibration while the impulse
      // solver is iterating.
      // Interpolation parameter 0..1: -handContactNormal.dot(vel_dir).

      float invVelocityDifferenceMagnitude = 1f / velocityDifference.magnitude;
      Vector3 vel_dir = velocityDifference * invVelocityDifferenceMagnitude;
      float t = -relativeVelocity * invVelocityDifferenceMagnitude;

      normal = t * vel_dir + (1.0f - t) * normal;

      if (normal.sqrMagnitude > float.Epsilon) { normal = normal.normalized; }
      relativeVelocity = Vector3.Dot(normal, velocityDifference);

      // Apply impulse along calculated normal.  Note this is slightly unusual.  Instead of
      // handling perpendicular movement as a separate constraint this calculates a "bent normal"
      // to blend it in.

      float jacDiagABInv = 1.0f / computeImpulseDenominator(thisObject, handContactPoint, normal, invWorldInertiaTensor);
      float velocityImpulse = -relativeVelocity * jacDiagABInv;

      applyImpulseNow(thisObject, normal * velocityImpulse, handContactPoint, invWorldInertiaTensor);
    }

    public static void applySoftContacts(List<SoftContact> softContacts, Dictionary<Rigidbody, Velocities> originalVelocities) {
      if (softContacts.Count > 0) {
        //Switch between iterating forwards and reverse through the list of contacts
        bool m_iterateForwards = true;
        for (int i = 0; i < 10; i++) {
          // Pick a random partition.
          int partition = UnityEngine.Random.Range(0, softContacts.Count);
          if (m_iterateForwards = !m_iterateForwards) {
            for (int it = partition; it < softContacts.Count; it++) {
              applySoftContact(softContacts[it].body, softContacts[it].position, softContacts[it].velocity, softContacts[it].normal, softContacts[it].invWorldInertiaTensor);
            }
            for (int it = 0; it < partition; it++) {
              applySoftContact(softContacts[it].body, softContacts[it].position, softContacts[it].velocity, softContacts[it].normal, softContacts[it].invWorldInertiaTensor);
            }
          } else {
            for (int it = partition; 0 <= it; it--) {
              applySoftContact(softContacts[it].body, softContacts[it].position, softContacts[it].velocity, softContacts[it].normal, softContacts[it].invWorldInertiaTensor);
            }
            for (int it = softContacts.Count - 1; partition <= it; it--) {
              applySoftContact(softContacts[it].body, softContacts[it].position, softContacts[it].velocity, softContacts[it].normal, softContacts[it].invWorldInertiaTensor);
            }
          }
        }

        //Now iterate through the list of Rigidbodies
        foreach (KeyValuePair<Rigidbody, Velocities> RigidbodyVelocity in originalVelocities) {
          //Apply these changes in velocity as forces, so the PhysX solver can resolve them (optional)
          RigidbodyVelocity.Key.AddForce(RigidbodyVelocity.Key.velocity - RigidbodyVelocity.Value.velocity, ForceMode.VelocityChange);
          RigidbodyVelocity.Key.AddTorque(RigidbodyVelocity.Key.angularVelocity - RigidbodyVelocity.Value.angularVelocity, ForceMode.VelocityChange);
          RigidbodyVelocity.Key.velocity = RigidbodyVelocity.Value.velocity;
          RigidbodyVelocity.Key.angularVelocity = RigidbodyVelocity.Value.angularVelocity;

          //Physics translates objects downward every physics update; preemptively translate them back
          if (RigidbodyVelocity.Key.useGravity) {
            RigidbodyVelocity.Key.MovePosition(RigidbodyVelocity.Key.position - (Physics.gravity * Time.fixedDeltaTime * Time.fixedDeltaTime * 0.95f));
          }
        }

        softContacts.Clear();
        originalVelocities.Clear();
      }
    }

    static float computeImpulseDenominator(Rigidbody thisObject, Vector3 pos, Vector3 normal, Matrix4x4 invWorldInertiaTensor) {
      Vector3 r0 = pos - thisObject.worldCenterOfMass;
      Vector3 c0 = Vector3.Cross(r0, normal);
      Vector3 vec = Vector3.Cross(invWorldInertiaTensor * c0, r0);
      return 1f / thisObject.mass + Vector3.Dot(normal, vec);
    }

    public static void applyImpulseNow(Rigidbody thisObject, Vector3 impulse, Vector3 worldPos, Matrix4x4 invWorldInertiaTensor) {
      if (thisObject.mass != 0f && 1f / thisObject.mass != 0f && impulse.IsValid() && impulse != Vector3.zero) {
        thisObject.velocity += impulse / thisObject.mass;
        thisObject.angularVelocity += Vector3.Cross(worldPos - thisObject.worldCenterOfMass, invWorldInertiaTensor * impulse);
      }
    }

    static void setPointVelocityNow(Rigidbody thisObject, Vector3 impulse, Vector3 worldPos, float LinearAngularRatio) {
      if (thisObject.mass != 0f && 1f / thisObject.mass != 0f) {
        LinearAngularRatio = Mathf.Clamp01(LinearAngularRatio);
        thisObject.velocity = impulse * LinearAngularRatio;
        thisObject.angularVelocity = Vector3.Cross(worldPos - thisObject.worldCenterOfMass, impulse / (worldPos - thisObject.worldCenterOfMass).sqrMagnitude) * (1f - LinearAngularRatio);
      }
    }
  }
}
