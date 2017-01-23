using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyParenting : MonoBehaviour, IRuntimeGizmoComponent {

  [Header("Debug")]
  public Text outputText;

  private Rigidbody _parentBody;
  private Rigidbody _childBody;

  void Start() {
    _childBody = GetComponent<Rigidbody>();
    _parentBody = _childBody.transform.parent.GetComponent<Rigidbody>();
    if (_parentBody == null) { Debug.LogError("[RigidbodyParenting] Must be attached to a Rigidbody that is the child of another Rigidbody."); }
    InitializeBodies();
  }

  private bool _physicsUpdated = false;

  void FixedUpdate() {
    if (!_physicsUpdated) {
      OnPrePhysics();
      _physicsUpdated = true;
    }
  }

  void Update() {
    OnPostPhysics();
    _physicsUpdated = false;

    if (outputText != null) {
      outputText.text = "Child Local Position: \n" + _childBody.transform.localPosition.ToString("G4") + "      "
                      + "Child Body Velocity: \n" + _childBody.velocity;
    }
  }

  #region World Space Calculations (really really bad rotational drift)

  //private Vector3 _prePhysicsParentPos;
  //private Quaternion _prePhysicsParentRot;
  //private Vector3 _prePhysicsChildPos;

  //private Vector3 _childPosUpdate = Vector3.zero;
  //private Vector3 _childPosFromRotUpdate = Vector3.zero;

  //private void OnPrePhysics() {
  //  _childBody.WakeUp();

  //  _prePhysicsParentPos = _parentBody.position;
  //  _prePhysicsParentRot = _parentBody.rotation;
  //  _prePhysicsChildPos = _childBody.position;

  //  _childBody.position = _childBody.position + _childPosUpdate + _childPosFromRotUpdate;
  //}

  //private void OnPostPhysics() {
  //  Vector3 parentPosDelta = _parentBody.position - _prePhysicsParentPos;
  //  _childPosUpdate = parentPosDelta;

  //  Quaternion parentRotDelta = _parentBody.rotation * Quaternion.Inverse(_prePhysicsParentRot);
  //  _childPosFromRotUpdate = parentRotDelta * (_prePhysicsChildPos - _prePhysicsParentPos) - (_prePhysicsChildPos - _prePhysicsParentPos);
  //}

  #endregion

  #region Transform Calculations

  private Transform _parentT;
  private Transform _childT;

  private void InitializeBodies() {
    _parentT = new GameObject("Transform Sim: " + _parentBody.gameObject.name).transform;
    _childT  = new GameObject("Transform Sim: " + _childBody.gameObject.name).transform;

    _parentT.transform.position = _parentBody.transform.position;
    _parentT.transform.rotation = _parentBody.transform.rotation;
    _parentT.transform.localScale = _parentBody.transform.localScale;

    _childT.transform.parent = _parentT;
    _childT.transform.position = _childBody.transform.position;
    _childT.transform.rotation = _childBody.transform.rotation;
    _childT.transform.localScale = _childBody.transform.localScale;
  }

  private Vector3    _prePhysicsParentPosition;
  private Quaternion _prePhysicsParentRotation;
  private Vector3    _prePhysicsChildTransformPosition;
  private Quaternion _prePhysicsChildTransformRotation;

  private Vector3    _childPosUpdate = Vector3.zero;
  private Quaternion _childRotUpdate = Quaternion.identity;

  // called once per frame, on the first FixedUpdate
  private void OnPrePhysics() {
    _prePhysicsParentPosition = _parentBody.position;
    _prePhysicsParentRotation = _parentBody.rotation;

    _childBody.position = _childBody.position + _childPosUpdate;
    _childBody.rotation = _childBody.rotation * _childRotUpdate;

    _childT.position = _childBody.position;
    _childT.rotation = _childBody.rotation;
    _prePhysicsChildTransformPosition = _childT.position;
    _prePhysicsChildTransformRotation = _childT.rotation;
  }

  // called on Update
  private void OnPostPhysics() {
    _parentT.position = _parentBody.position;
    _parentT.rotation = _parentBody.rotation;

    _childPosUpdate = _childT.position - _prePhysicsChildTransformPosition;
    _childRotUpdate = Quaternion.Inverse(_prePhysicsChildTransformRotation) * _childT.rotation;
  }

  #endregion

  #region Gizmos

  public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    if (_childBody == null || _parentBody == null) return;

    drawer.PushMatrix();
    drawer.color = Color.red;
    drawer.matrix = _childBody.transform.localToWorldMatrix;
    drawer.DrawWireCube(Vector3.zero, Vector3.one * 0.04F);
    drawer.PopMatrix();

    drawer.color = new Color(0.9F, 0.45F, 0.1F);
    drawer.DrawLine(_childBody.position, _childBody.position + _childBody.rotation * Vector3.up * 0.1F);
  }

  #endregion

}
