using UnityEngine;
using System.Collections.Generic;

public class Pierce : MonoBehaviour {
  public float linearLimit = 0.15f;
  public float tipDistance = 0.1f;
  public float slidingResistance = 600f;

  List<ConfigurableJoint>     joints          = new List<ConfigurableJoint>();
  Dictionary<Joint, Collider> jointToCollider = new Dictionary<Joint, Collider>();

  // Check for Stabs with each collision
  void OnCollisionEnter(Collision collision) { TryToStabIntoObject(collision); }
  void OnCollisionStay (Collision collision) { TryToStabIntoObject(collision); }

  // Every FixedUpdate, check if we should end the "Pierced State"
  void FixedUpdate() {
    for(int i = 0; i < joints.Count; i++) {
      Vector3 worldJointAnchor     = transform.TransformPoint(joints[i].anchor);
      Vector3 worldConnectedAnchor;
      if (joints[i].connectedBody != null) {
        worldConnectedAnchor = joints[i].connectedBody.transform.TransformPoint(joints[i].connectedAnchor);
      } else {
        worldConnectedAnchor = joints[i].connectedArticulationBody.transform.TransformPoint(joints[i].connectedAnchor);
      }

      if (Vector3.Dot(transform.up, worldConnectedAnchor - worldJointAnchor) < -0.001f) {
        foreach (Collider collider in GetComponentsInChildren<Collider>()) {
          Physics.IgnoreCollision(collider, jointToCollider[joints[i]], false);
        }
        jointToCollider.Remove(joints[i]);
        Destroy(joints[i]);
        joints.RemoveAt(i);
        i--;
      }
    }
  }

  void TryToStabIntoObject(Collision collision) {
    // Filter by whether the other object has a "rigidbody"
    if (collision.rigidbody == null) { return; }

    // Filter Collisions by whether they occured at the tip of the blade
    int collidedWithTipIndex = -1;
    for(int i = 0; i < collision.contactCount; i++) {
      if (transform.InverseTransformPoint(collision.contacts[i].point).y <= -tipDistance) {
        collidedWithTipIndex = i;
      }
    }
    if (collidedWithTipIndex < 0) return;

    // Filter collisions by the force magnitude applied
    if (Mathf.Abs(Vector3.Dot(collision.impulse, transform.up)) <= 2f) { return; }

    // If a PiercingEvent truly has occurred, create a linear joint to represent it
    ConfigurableJoint linearJoint = gameObject.AddComponent<ConfigurableJoint>();

    // ArticulationBody Special Case
    ArticulationBody aBody = collision.rigidbody.GetComponent<ArticulationBody>();
    if (aBody != null) {
      linearJoint.connectedArticulationBody = aBody;
    } else {
      linearJoint.connectedBody = collision.rigidbody;
    }

    // Configure the properties of the sliding joint
    linearJoint.autoConfigureConnectedAnchor = true;
    linearJoint.enableCollision   = false;
    linearJoint.linearLimit       = new SoftJointLimit       { bounciness = 0f, contactDistance = 0.2f, limit = linearLimit };
    linearJoint.linearLimitSpring = new SoftJointLimitSpring { damper = 1000f, spring = 100000f };
    linearJoint.anchor            = transform.InverseTransformPoint(collision.contacts[collidedWithTipIndex].point);
    linearJoint.xMotion           = ConfigurableJointMotion.Locked;
    linearJoint.yMotion           = ConfigurableJointMotion.Limited;
    linearJoint.zMotion           = ConfigurableJointMotion.Locked;
    linearJoint.angularXMotion    = ConfigurableJointMotion.Locked;
    linearJoint.angularYMotion    = ConfigurableJointMotion.Locked;
    linearJoint.angularZMotion    = ConfigurableJointMotion.Locked;
    linearJoint.yDrive            = new JointDrive { positionDamper = slidingResistance, positionSpring = 0f, maximumForce = 10000000f };

    // Disable collisions between the piercer and the colliding object
    foreach (Collider collider in GetComponentsInChildren<Collider>()) {
      Physics.IgnoreCollision(collider, collision.collider, true);
    }
    jointToCollider.Add(linearJoint, collision.collider);

    // Add the joint to the list of joints
    joints.Add(linearJoint);
  }

}
