using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyParenting : MonoBehaviour {

  private Rigidbody _parentBody;
  private Rigidbody _body;

  void Start() {
    _body = GetComponent<Rigidbody>();
    _parentBody = _body.transform.parent.GetComponent<Rigidbody>();
    if (_parentBody == null) {
      Debug.LogError("[RigidbodyParenting] Must be attached to a Rigidbody that is the child of another Rigidbody.");
    }
  }

  private Vector3 _parentPositionPrePhysics;
  private Quaternion _parentRotationPrePhysics;
  private bool _didPhysicsMetrics;

  void FixedUpdate() {
    if (!_didPhysicsMetrics) {
      _parentPositionPrePhysics = _parentBody.transform.position;
      _parentRotationPrePhysics = _parentBody.transform.rotation;
      _didPhysicsMetrics = true;
    }
  }

  void Update() {
    Vector3 parentPositionDelta = _parentBody.transform.position - _parentPositionPrePhysics;
    _body.position = parentPositionDelta + _body.position;

    Quaternion parentRotationDelta = _parentBody.transform.rotation * Quaternion.Inverse(_parentRotationPrePhysics);
    _body.rotation = parentRotationDelta * _body.rotation;

    _didPhysicsMetrics = false;
  }

}
