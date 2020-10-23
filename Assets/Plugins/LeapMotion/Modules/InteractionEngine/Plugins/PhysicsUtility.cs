/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/


using UnityEngine;
using System.Collections.Generic;

namespace Leap.Interaction.Internal.InteractionEngineUtility {
  public static class PhysicsUtility {
    public struct SoftContact {
      public Rigidbody body;
      public Vector3   position;
      public Vector3   normal;
      public Vector3   velocity;
      public Matrix4x4 invWorldInertiaTensor;
    }

    public struct Velocities {
      public Vector3   velocity;
      public Vector3   angularVelocity;
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

    public static bool generateSphereContacts(Vector3 spherePosition, float sphereRadius, Vector3 sphereVelocity, int layerMask, ref List<SoftContact> softContacts, ref Dictionary<Rigidbody, Velocities> originalVelocities, ref Collider[] temporaryColliderSwapSpace, bool interpretAsEllipsoid = true) {
      int numberOfColliders = Physics.OverlapSphereNonAlloc(spherePosition, sphereRadius, temporaryColliderSwapSpace, layerMask, QueryTriggerInteraction.Ignore);
      for (int i = 0; i < numberOfColliders; i++) {
        generateSphereContact(spherePosition, sphereRadius, sphereVelocity, layerMask, ref softContacts, ref originalVelocities, temporaryColliderSwapSpace[i], interpretAsEllipsoid);
      }
      return numberOfColliders > 0;
    }

    public static void generateBoxContact(BoxCollider box, int layerMask, Collider otherCollider, ref List<SoftContact> softContacts, ref Dictionary<Rigidbody, Velocities> originalVelocities, bool interpretAsEllipsoid = true) {
      Vector3 boxExtent = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
      Vector3 boxCenter = Vector3.Scale(box.center, box.transform.lossyScale);
      Vector3 unrotatedOtherColliderPosition = Quaternion.Inverse(box.attachedRigidbody.rotation) * (otherCollider.attachedRigidbody.position - box.attachedRigidbody.position);
      Vector3 otherColliderPositionOnBox = new Vector3(
        Mathf.Clamp(unrotatedOtherColliderPosition.x, -boxExtent.x + boxCenter.x, boxExtent.x + boxCenter.x),
        Mathf.Clamp(unrotatedOtherColliderPosition.y, -boxExtent.y + boxCenter.y, boxExtent.y + boxCenter.y),
        Mathf.Clamp(unrotatedOtherColliderPosition.z, -boxExtent.z + boxCenter.z, boxExtent.z + boxCenter.z));

      float radius = otherColliderPositionOnBox.magnitude;
      otherColliderPositionOnBox = (box.attachedRigidbody.rotation * otherColliderPositionOnBox) + box.attachedRigidbody.position;

      generateSphereContact(otherColliderPositionOnBox, 0f, box.attachedRigidbody.GetPointVelocity(otherColliderPositionOnBox), layerMask, ref softContacts, ref originalVelocities, otherCollider, interpretAsEllipsoid);
    }

    public static void generateSphereContact(SphereCollider sphere, int layerMask, Collider otherCollider, ref List<SoftContact> softContacts, ref Dictionary<Rigidbody, Velocities> originalVelocities, bool interpretAsEllipsoid = true) {
      Vector3 unrotatedOtherColliderPosition = sphere.transform.InverseTransformPoint(otherCollider.attachedRigidbody.position) - sphere.center;
      float dist = unrotatedOtherColliderPosition.magnitude;
      if (dist > sphere.radius) {
        unrotatedOtherColliderPosition *= sphere.radius / dist;
      }
      Vector3 otherColliderPositionOnSphere = sphere.transform.TransformPoint(unrotatedOtherColliderPosition + sphere.center);

      generateSphereContact(sphere.transform.TransformPoint(sphere.center), (sphere.transform.TransformPoint(sphere.center) - otherColliderPositionOnSphere).magnitude, sphere.attachedRigidbody.GetPointVelocity(otherColliderPositionOnSphere), layerMask, ref softContacts, ref originalVelocities, otherCollider, interpretAsEllipsoid);
    }

    public static void generateCapsuleContact(CapsuleCollider capsule, int layerMask, Collider otherCollider, ref List<SoftContact> softContacts, ref Dictionary<Rigidbody, Velocities> originalVelocities, bool interpretAsEllipsoid = true) {
      Vector3 unrotatedOtherColliderPosition = capsule.transform.InverseTransformPoint(otherCollider.attachedRigidbody.position) - capsule.center;
      Vector3 a, b;
      switch (capsule.direction) {
        case 0:
          a = Vector3.right * ((capsule.height * 0.5f) - capsule.radius);
          break;
        case 1:
          a = Vector3.up * ((capsule.height * 0.5f) - capsule.radius);
          break;
        case 2:
          a = Vector3.forward * ((capsule.height * 0.5f) - capsule.radius);
          break;
        default:
          a = Vector3.up * ((capsule.height * 0.5f) - capsule.radius);
          break;
      }
      b = -a; Vector3 ba = b - a;
      Vector3 otherColliderPosOnSegment = Vector3.Lerp(a, b, Vector3.Dot(unrotatedOtherColliderPosition - a, ba) / ba.sqrMagnitude);
      Vector3 displacement = unrotatedOtherColliderPosition - otherColliderPosOnSegment;
      float dist = displacement.magnitude;
      if (dist > capsule.radius) {
        displacement *= capsule.radius / dist;
      }
      Vector3 otherColliderPositionOnCapsule =  capsule.transform.TransformPoint(otherColliderPosOnSegment + displacement + capsule.center);

      generateSphereContact(capsule.transform.TransformPoint(capsule.center), (capsule.transform.TransformPoint(capsule.center) - otherColliderPositionOnCapsule).magnitude, capsule.attachedRigidbody.GetPointVelocity(otherColliderPositionOnCapsule), layerMask, ref softContacts, ref originalVelocities, otherCollider, interpretAsEllipsoid);
    }

    public static void generateSphereContact(Vector3 spherePosition, float sphereRadius, Vector3 sphereVelocity, int layerMask, ref List<SoftContact> softContacts, ref Dictionary<Rigidbody, Velocities> originalVelocities, Collider temporaryCollider, bool interpretAsEllipsoid = true) {
      if (temporaryCollider.attachedRigidbody != null && !temporaryCollider.attachedRigidbody.isKinematic) {
        SoftContact contact = new SoftContact();
        contact.body = temporaryCollider.attachedRigidbody;

        //Store this rigidbody's pre-contact velocities and inverse world inertia tensor
        Velocities originalBodyVelocities;
        if (!originalVelocities.TryGetValue(contact.body, out originalBodyVelocities)) {
          originalBodyVelocities = new Velocities();
          originalBodyVelocities.velocity = contact.body.velocity;
          originalBodyVelocities.angularVelocity = contact.body.angularVelocity;

          Matrix4x4 tensorRotation = Matrix4x4.TRS(Vector3.zero, contact.body.rotation * contact.body.inertiaTensorRotation, Vector3.one);
          Matrix4x4 worldInertiaTensor = (tensorRotation * Matrix4x4.Scale(contact.body.inertiaTensor) * tensorRotation.inverse);
          originalBodyVelocities.invWorldInertiaTensor = worldInertiaTensor.inverse;

          originalVelocities.Add(contact.body, originalBodyVelocities);

          //Premptively apply the force due to gravity BEFORE soft contact
          //so soft contact can factor it in during the collision solve
          if (contact.body.useGravity) {
            contact.body.AddForce(-Physics.gravity, ForceMode.Acceleration);
            contact.body.velocity += Physics.gravity * Time.fixedDeltaTime;
          }
        }

        if (interpretAsEllipsoid || temporaryCollider is MeshCollider) {
          //First get the world spherical normal
          contact.normal = (temporaryCollider.bounds.center - spherePosition).normalized;
          //Then divide by the world extends squared to get the world ellipsoidal normal
          Vector3 colliderExtentsSquared = Vector3.Scale(temporaryCollider.bounds.extents, (temporaryCollider.bounds.extents));
          contact.normal = new Vector3(contact.normal.x / colliderExtentsSquared.x, contact.normal.y / colliderExtentsSquared.y, contact.normal.z / colliderExtentsSquared.z).normalized;
        } else {
          //Else, use the analytic support functions that we have for boxes and capsules to generate the normal
          Vector3 objectLocalBoneCenter = temporaryCollider.transform.InverseTransformPoint(spherePosition);
          Vector3 objectPoint = temporaryCollider.transform.TransformPoint(temporaryCollider.ClosestPointOnSurface(objectLocalBoneCenter));
          contact.normal = (objectPoint - spherePosition).normalized * (temporaryCollider.IsPointInside(objectLocalBoneCenter) ? -1f : 1f);
        }

        contact.position = spherePosition + (contact.normal * sphereRadius);
        contact.velocity = sphereVelocity;
        contact.invWorldInertiaTensor = originalBodyVelocities.invWorldInertiaTensor;

        softContacts.Add(contact);
      }
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
          if (m_iterateForwards) {
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
          m_iterateForwards = !m_iterateForwards;
        }

        //Apply these changes in velocity as forces, so the PhysX solver can resolve them (optional)
        foreach (KeyValuePair<Rigidbody, Velocities> RigidbodyVelocity in originalVelocities) {
          RigidbodyVelocity.Key.AddForce(RigidbodyVelocity.Key.velocity - RigidbodyVelocity.Value.velocity, ForceMode.VelocityChange);
          RigidbodyVelocity.Key.AddTorque(RigidbodyVelocity.Key.angularVelocity - RigidbodyVelocity.Value.angularVelocity, ForceMode.VelocityChange);
          RigidbodyVelocity.Key.velocity = RigidbodyVelocity.Value.velocity;
          RigidbodyVelocity.Key.angularVelocity = RigidbodyVelocity.Value.angularVelocity;
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
        impulse = impulse.normalized * Mathf.Clamp(impulse.magnitude, 0f, 6f);
        thisObject.velocity += impulse / thisObject.mass;
        Vector3 angularImpulse = Vector3.Cross(worldPos - thisObject.worldCenterOfMass, invWorldInertiaTensor * impulse);
        if (angularImpulse.magnitude < 4f) {
          thisObject.angularVelocity += angularImpulse;
        }
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
