using Leap.Unity;
using Leap.Unity.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spaceship : MonoBehaviour {

  private Vector3 velocity;
  private Vector3 angularVelocity;
  private float mass = 10F;

  private Rigidbody _body;
  private Vector3 accumulatedForce;
  private Vector3 accumulatedTorque;
  //private struct ForceAtPosition {  public Vector3 force; public Vector3 position; }
  //private List<ForceAtPosition> _accumulatedForceAtPositions = new List<ForceAtPosition>();

  void Start() {
    PhysicsCallbacks.OnPrePhysics += OnPrePhysics;
    _body = GetComponent<Rigidbody>();
    _body.mass = mass;
  }

  public void AddForce(Vector3 force) {
    accumulatedForce += force;
  }

  public void AddForceAtPosition(Vector3 force, Vector3 position) {
    Vector3 toCenterOfMass = this.transform.TransformPoint(_body.centerOfMass) - position;

    float forceCMAngle = Vector3.Angle(force, toCenterOfMass);

    // Linear force
    Vector3 linForce = force * Mathf.Cos(forceCMAngle);
    accumulatedForce += linForce;

    // Torque
    Vector3 torqueVector = force * Mathf.Sin(forceCMAngle) * toCenterOfMass.magnitude;

    accumulatedTorque += Vector3.Cross(torqueVector, toCenterOfMass);
  }

  void OnPrePhysics() {
    Vector3 acceleration = accumulatedForce / mass;
    velocity += acceleration * Time.fixedDeltaTime;
    this.transform.position += velocity * Time.fixedDeltaTime;
    accumulatedForce = Vector3.zero;

    // torque
    //Quaternion worldInertiaTensorRot = this.transform.rotation * _body.inertiaTensorRotation;
    //Vector3 eulerAcceleration = worldInertiaTensorRot * ((Quaternion.Inverse(worldInertiaTensorRot) * accumulatedTorque).CompDiv(Vector3.one/*_body.inertiaTensor*/));

    Vector3 eulerAcceleration = accumulatedTorque;
    Debug.DrawLine(Vector3.zero, eulerAcceleration);
    angularVelocity += eulerAcceleration * Time.fixedDeltaTime;
    this.transform.rotation = Quaternion.Euler(angularVelocity * Time.fixedDeltaTime) * this.transform.rotation;
    accumulatedTorque = Vector3.zero;
  }

}
