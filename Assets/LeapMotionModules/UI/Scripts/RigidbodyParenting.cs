using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyParenting : MonoBehaviour, IRuntimeGizmoComponent {

  [SerializeField]
  private Rigidbody _parentBody;
  [SerializeField]
  private Rigidbody _body;

  void Start() {
    _body = GetComponent<Rigidbody>();
    _parentBody = _body.transform.parent.GetComponent<Rigidbody>();
    if (_parentBody == null) {
      Debug.LogError("[RigidbodyParenting] Must be attached to a Rigidbody that is the child of another Rigidbody.");
    }
    _body.sleepThreshold = -1F;
  }

  private Vector3 _parentPositionPrePhysics;
  private Quaternion _parentRotationPrePhysics;
  private Vector3 _bodyPositionInParentSpacePrePhysics;
  private bool _didPhysicsMetrics;

  void FixedUpdate() {
    if (!_didPhysicsMetrics) {
      _parentPositionPrePhysics = _parentBody.position;
      _parentRotationPrePhysics = _parentBody.rotation;
      _bodyPositionInParentSpacePrePhysics = _parentBody.transform.InverseTransformPoint(_body.position);
      _didPhysicsMetrics = true;
    }
    _body.velocity = Vector3.zero;
    _body.angularVelocity = Vector3.zero;
  }

  void Update() {
    Vector3 parentPositionDelta = _parentBody.position - _parentPositionPrePhysics;

    Quaternion parentRotationDelta = _parentBody.rotation * Quaternion.Inverse(_parentRotationPrePhysics);
    float rotationDeltaAngle;
    Vector3 rotationDeltaAxis;
    parentRotationDelta.ToAngleAxis(out rotationDeltaAngle, out rotationDeltaAxis);

    Vector3 positionDeltaDueToParentRotation = (parentRotationDelta * _parentBody.transform.TransformVector(_bodyPositionInParentSpacePrePhysics)) - _parentBody.transform.TransformVector(_bodyPositionInParentSpacePrePhysics);

    _body.position = _body.position + Vector3.zero /*parentPositionDelta*/ + positionDeltaDueToParentRotation;

    _body.rotation = parentRotationDelta * _body.rotation;

    _didPhysicsMetrics = false;
  }

  #region Gizmos

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {

    drawer.PushMatrix();
    drawer.color = Color.red;
    drawer.matrix = _body.transform.localToWorldMatrix;
    drawer.DrawWireCube(Vector3.zero, Vector3.one * 0.04F);
    drawer.PopMatrix();

    drawer.color = new Color(0.9F, 0.45F, 0.1F);
    drawer.DrawLine(_body.position, _body.position + _body.rotation * Vector3.up * 0.1F);
  }

  #endregion

}
