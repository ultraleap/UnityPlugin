using Leap.Unity;
using Leap.Unity.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spaceship : MonoBehaviour {

  public
  #if UNITY_EDITOR
  new
  #endif
  Rigidbody rigidbody { get { return _body; } }

  private Rigidbody _body;
  private float _mass = 10F;

  private Vector3 _velocity;
  public Vector3 velocity {
    get { return _velocity; }
    set { _velocity = value; }
  }

  public Vector3 shipAlignedVelocity {
    get { return Quaternion.Inverse(this.transform.rotation) * _velocity; }
  }

  private Vector3 _angularVelocity;
  public Vector3 angularVelocity {
    get { return _angularVelocity; }
    set { _angularVelocity = value; }
  }

  public Vector3 shipAlignedAngularVelocity {
    get { return Quaternion.Inverse(this.transform.rotation) * _angularVelocity; }
  }

  private Vector3 _accumulatedForce;
  private Vector3 _accumulatedTorque;

  public Action OnMovement = () => { };

  void Awake() {
    _body = GetComponent<Rigidbody>();
    _body.mass = _mass;
  }

  void Start() {
    InteractionManager.instance.OnPrePhysicalUpdate += updateShipPhysics;
  }

  void Update() {
   // updateShipPhysics();
  }

  private void updateShipPhysics() {
    // Update velocity.
    Vector3 acceleration = _accumulatedForce / _mass;
    _velocity += acceleration * Time.deltaTime;

    this.rigidbody.velocity = _velocity;
    _accumulatedForce = Vector3.zero;

    // Update position.
    Vector3 newPosition = this.transform.position + _velocity * Time.deltaTime;
    //this.rigidbody.position = newPosition;
    this.transform.position = newPosition;


    // Update angular velocity.
    Vector3 eulerAcceleration = _accumulatedTorque;
    _angularVelocity += eulerAcceleration * Time.deltaTime;

    this.rigidbody.angularVelocity = _angularVelocity;
    _accumulatedTorque = Vector3.zero;

    // Update rotation.
    Quaternion newRotation = Quaternion.Euler(_angularVelocity * Time.deltaTime) * this.transform.rotation;
    //this.rigidbody.rotation = newRotation;
    this.transform.rotation = newRotation;
    
    OnMovement();
  }

  #region Ship Forces API

  public void AddForce(Vector3 force) {
    _accumulatedForce += force;
  }

  public void AddForceAtPosition(Vector3 force, Vector3 position) {
    Vector3 toCenterOfMass = this.transform.TransformPoint(_body.centerOfMass) - position;

    float forceCMAngle = Vector3.Angle(force, toCenterOfMass);

    // Linear force
    Vector3 linForce = force * Mathf.Cos(forceCMAngle);
    _accumulatedForce += linForce;

    // Torque
    Vector3 torqueVector = force * Mathf.Sin(forceCMAngle) * toCenterOfMass.magnitude;

    _accumulatedTorque += Vector3.Cross(torqueVector, toCenterOfMass);
  }

  public void AddShipAlignedTorque(Vector3 shipAlignedTorque) {
    _accumulatedTorque += this.transform.rotation * shipAlignedTorque;
  }

  public void AddShipAlignedForce(Vector3 shipAlignedForce) {
    _accumulatedForce += this.transform.rotation * shipAlignedForce;
  }

  #endregion

}
