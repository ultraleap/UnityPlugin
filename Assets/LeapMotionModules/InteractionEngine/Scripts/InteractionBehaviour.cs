
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction.CApi;
using LeapInternal;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// InteractionBehaviour is the default implementation of IInteractionBehaviour.
  /// </summary>
  ///
  /// <remarks>
  /// It has the following features:
  ///    - Extends from InteractionBehaviourBase to take advantage of it's bookkeeping and callbacks.
  ///    - Supports kinematic movement as well as physical movement.
  ///    - When non-kinematic, supports pushing.
  ///    - Has the concept of a graphical anchor to reduce apparent latency between a hand moving and the object responding.
  ///      This can result in the graphical representation diverging slightly from the physical representation.
  ///    - Utilizes the Kabsch algorithm to determine how the object should rest in the hand when grabbed.
  ///      This allows more fidelity than simple rigid atatchment to the hand, as well as more intuitive multi-hand
  ///      interaction.
  ///
  /// This default implementation has the following requirements:
  ///    - A Rigidbody is required
  ///    - Kinematic movement must still be simulated via Rigidbody kinematic movement, as opposed to rigid movement of the Transform.
  ///    - Any non-continuous movement must be noted using the NotifyTeleported() method.
  ///    - Any forces or torques must be applied using the AddLinearAcceleration and AddAngularAcceleration methods instead of
  ///      the Rigidbody AddForce or AddTorque methods.
  ///    - Any update of the kinematic or gravity status of the object must be done through setting the IsKinematic or UseGravity
  ///      properties of this behaviour instead of the properties on the Rigidbody component.
  /// </remarks>
  [SelectionBase]
  [RequireComponent(typeof(Rigidbody))]
  public class InteractionBehaviour : InteractionBehaviourBase {
    public const int NUM_FINGERS = 5;
    public const int NUM_BONES = 4;

    [SerializeField]
    protected InteractionMaterial _material;

    protected Transform[] _childrenArray;
    protected Rigidbody _rigidbody;

    protected bool _isKinematic;
    protected bool _useGravity;
    protected float _drag;
    protected float _angularDrag;
    protected bool _recievedVelocityUpdate = false;
    protected bool _recievedSimulationResults = false;
    protected bool _notifiedOfTeleport = false;
    protected bool _ignoringBrushes = false;

    protected Vector3 _solvedPosition;
    protected Quaternion _solvedRotation;

    protected PhysicMaterialReplacer _materialReplacer;
    protected RigidbodyWarper _warper;

    protected Vector3 _accumulatedLinearAcceleration = Vector3.zero;
    protected Vector3 _accumulatedAngularAcceleration = Vector3.zero;

    private Bounds _debugBounds;
    private bool _showDebugRecievedVelocity = false;

    #region PUBLIC METHODS

    /// <summary>
    /// Sets or Gets whether or not this InteractionBehaviour is Kinematic or not.  Always use this instead
    /// of Rigidbody.IsKinematic because InteractionBehaviour overrides the kinematic status of the Rigidbody.
    /// </summary>
    public bool isKinematic {
      get {
        return _isKinematic;
      }
      set {
        _isKinematic = value;
        if (HasShapeInstance) {
          if (!IsBeingGrasped) {
            _rigidbody.isKinematic = value;
          }
        } else {
          _rigidbody.isKinematic = value;
        }
      }
    }

    public new Rigidbody rigidbody {
      get {
        return _rigidbody;
      }
    }

    public InteractionMaterial material {
      get {
        return _material;
      }
    }

    /// <summary>
    /// Sets or Gets whether or not this InteractionBehaviour uses Gravity or not.  Always use this instead
    /// of Rigidbody.UseGravity because InteractionBehaviour overrides the gravity status of the Rigidbody.
    /// </summary>
    public bool useGravity {
      get {
        return _useGravity;
      }
      set {
        _useGravity = value;
        if (!HasShapeInstance) {
          _rigidbody.useGravity = _useGravity;
        }
      }
    }

    /// <summary>
    /// Adds a linear acceleration to the center of mass of this object.  Use this instead of Rigidbody.AddForce()
    /// </summary>
    public void AddLinearAcceleration(Vector3 acceleration) {
      _accumulatedLinearAcceleration += acceleration;
    }

    /// <summary>
    /// Adds an angular acceleration to the center of mass of this object.  Use this instead of Rigidbody.AddTorque()
    /// </summary>
    public void AddAngularAcceleration(Vector3 acceleration) {
      _accumulatedAngularAcceleration += acceleration;
    }

    /// <summary>
    /// This method should always be called if the object is teleported to a new location instead of moving there.  If
    /// this method is not called, it can cause the simulation to become unstable.
    /// </summary>
    public void NotifyTeleported() {
      _notifiedOfTeleport = true;
    }
    #endregion

    #region INTERACTION CALLBACKS

    protected override void OnRegistered() {
      base.OnRegistered();

      _rigidbody = GetComponent<Rigidbody>();
      if (_rigidbody == null) {
        //Should only happen if the user has done some trickery since there is a RequireComponent attribute
        throw new InvalidOperationException("InteractionBehaviour must have a Rigidbody component attached to it.");
      }
      _rigidbody.maxAngularVelocity = float.PositiveInfinity;

      _materialReplacer = new PhysicMaterialReplacer(transform, _material);
      _warper = new RigidbodyWarper(_manager, transform, _rigidbody, _material.GraphicalReturnTime);

      _childrenArray = GetComponentsInChildren<Transform>(true);
      updateLayer();
    }

    protected override void OnUnregistered() {
      base.OnUnregistered();

      _warper.Dispose();
      _warper = null;

      resetState();
    }

#if UNITY_EDITOR
    protected override void OnPreSolve() {
      base.OnPreSolve();

      _showDebugRecievedVelocity = false;
    }
#endif

    protected override void OnPostSolve() {
      base.OnPostSolve();

      if (IsBeingGrasped) {
        if (Vector3.Distance(_solvedPosition, _warper.RigidbodyPosition) > _material.ReleaseDistance * _manager.SimulationScale) {
          _manager.ReleaseObject(this);
        }
      } else {
        if (_recievedSimulationResults) {
          _materialReplacer.ReplaceMaterials();
        } else {
          _materialReplacer.RevertMaterials();
        }

        if (_recievedVelocityUpdate) {
          //If we recieved a velocity update, gravity must always be disabled because the
          //velocity update accounts for gravity.
          if (_rigidbody.useGravity) {
            _rigidbody.useGravity = false;
          }


        } else {
          //If we did not recieve a velocity update, we set the rigidbody's gravity status
          //to match whatever the user has set.
          if (_rigidbody.useGravity != _useGravity) {
            _rigidbody.useGravity = _useGravity;
          }

          //Only apply if non-zero to prevent waking up the body
          if (_accumulatedLinearAcceleration != Vector3.zero) {
            _rigidbody.AddForce(_accumulatedLinearAcceleration, ForceMode.Acceleration);
          }

          if (_accumulatedAngularAcceleration != Vector3.zero) {
            _rigidbody.AddTorque(_accumulatedAngularAcceleration, ForceMode.Acceleration);
          }
        }
      }

      //Reset so we can accumulate for the next frame
      _accumulatedLinearAcceleration = Vector3.zero;
      _accumulatedAngularAcceleration = Vector3.zero;
      _recievedVelocityUpdate = false;
      _recievedSimulationResults = false;
    }

    public override void GetInteractionShapeCreationInfo(out INTERACTION_CREATE_SHAPE_INFO createInfo, out INTERACTION_TRANSFORM createTransform) {
      createInfo = new INTERACTION_CREATE_SHAPE_INFO();
      createInfo.shapeFlags = ShapeInfoFlags.None;

      if (!_isKinematic) {
        //Kinematic objects do not need velocity simulation
        createInfo.shapeFlags |= ShapeInfoFlags.HasRigidBody;
      }

      createTransform = getRigidbodyTransform();
    }

    protected override void OnInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle) {
      base.OnInteractionShapeCreated(instanceHandle);

      //Copy over existing settings for defaults
      _isKinematic = _rigidbody.isKinematic;
      _useGravity = _rigidbody.useGravity;
      _drag = _rigidbody.drag;
      _angularDrag = _rigidbody.angularDrag;

      _solvedPosition = _rigidbody.position;
      _solvedRotation = _rigidbody.rotation;

      updateLayer();

#if UNITY_EDITOR
      Collider[] colliders = GetComponentsInChildren<Collider>();
      if (colliders.Length > 0) {
        _debugBounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++) {
          _debugBounds.Encapsulate(colliders[i].bounds);
        }
        _debugBounds.center = transform.InverseTransformPoint(_debugBounds.center);
      }
#endif
    }

    protected override void OnInteractionShapeDestroyed() {
      base.OnInteractionShapeDestroyed();

      resetState();
    }

    public override void GetInteractionShapeUpdateInfo(out INTERACTION_UPDATE_SHAPE_INFO updateInfo, out INTERACTION_TRANSFORM interactionTransform) {
      updateInfo = new INTERACTION_UPDATE_SHAPE_INFO();

      updateInfo.updateFlags = UpdateInfoFlags.VelocityEnabled;
      updateInfo.linearVelocity = _rigidbody.velocity.ToCVector();
      updateInfo.angularVelocity = _rigidbody.angularVelocity.ToCVector();

      // Request notification of when hands are no longer touching (or influencing.)
      if (_ignoringBrushes) {
        updateInfo.updateFlags |= UpdateInfoFlags.ReportNoResult;
      }

      if (_material.ContactEnabled && !_isKinematic && !IsBeingGrasped) {
        updateInfo.updateFlags |= UpdateInfoFlags.AccelerationEnabled;
        updateInfo.linearAcceleration = _accumulatedLinearAcceleration.ToCVector();
        updateInfo.angularAcceleration = _accumulatedAngularAcceleration.ToCVector();
      }

      if (_useGravity) {
        updateInfo.updateFlags |= UpdateInfoFlags.GravityEnabled;
      }

      interactionTransform = getRigidbodyTransform();
    }

    protected override void OnRecievedSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results) {
      base.OnRecievedSimulationResults(results);

      _recievedSimulationResults = true;

      if ((results.resultFlags & ShapeInstanceResultFlags.Velocities) != 0 &&
          !IsBeingGrasped &&
          _material.ContactEnabled) {
        //Use Sleep() to clear any forces that might have been applied by the user.
        _rigidbody.Sleep();
        _rigidbody.velocity = results.linearVelocity.ToVector3();
        _rigidbody.angularVelocity = results.angularVelocity.ToVector3();
        _recievedVelocityUpdate = true;
      }

#if UNITY_EDITOR
      _showDebugRecievedVelocity = _recievedVelocityUpdate;
#endif

      if ((results.resultFlags & ShapeInstanceResultFlags.MaxHand) != 0) {
        if (!_ignoringBrushes && results.maxHandDepth > _material.BrushDisableDistance * _manager.SimulationScale) {
          _ignoringBrushes = true;
        }
      } else if (_ignoringBrushes) {
        _ignoringBrushes = false;
      }

      updateLayer();
    }

    protected override void OnHandGrasped(Hand hand) {
      base.OnHandGrasped(hand);
    }

    protected override void OnHandsHoldPhysics(ReadonlyList<Hand> hands) {
      base.OnHandsHoldPhysics(hands);

      float distanceToSolved = Vector3.Distance(_warper.RigidbodyPosition, _solvedPosition);

      _notifiedOfTeleport = false;
    }

    protected override void OnHandsHoldGraphics(ReadonlyList<Hand> hands) {
      base.OnHandsHoldGraphics(hands);

      if (_material.WarpingEnabled) {
        Vector3 deltaPosition = Quaternion.Inverse(_solvedRotation) * (_warper.RigidbodyPosition - _solvedPosition);
        Quaternion deltaRotation = Quaternion.Inverse(_solvedRotation) * _warper.RigidbodyRotation;

        Vector3 newPosition = Vector3.zero;
        Quaternion newRotation = Quaternion.identity;
        //TODO: get solved position using holding controller

        Vector3 graphicalPosition = newPosition + newRotation * deltaPosition;
        Quaternion graphicalRotation = newRotation * deltaRotation;

        _warper.WarpPercent = _material.WarpCurve.Evaluate(deltaPosition.magnitude / _manager.SimulationScale);
        _warper.SetGraphicalPosition(graphicalPosition, graphicalRotation);
      }
    }

    protected override void OnHandReleased(Hand hand) {
      base.OnHandReleased(hand);
    }

    protected override void OnHandLostTracking(Hand oldHand, out float maxSuspensionTime) {
      base.OnHandLostTracking(oldHand, out maxSuspensionTime);
    }

    protected override void OnHandRegainedTracking(Hand newHand, int oldId) {
      base.OnHandRegainedTracking(newHand, oldId);

      NotifyTeleported();
    }

    protected override void OnHandTimeout(Hand oldHand) {
      base.OnHandTimeout(oldHand);
    }

    protected override void OnGraspBegin() {
      base.OnGraspBegin();

      _materialReplacer.ReplaceMaterials();

      _ignoringBrushes = true;
    }

    protected override void OnGraspEnd(Hand lastHand) {
      base.OnGraspEnd(lastHand);

      _materialReplacer.RevertMaterials();

      if (lastHand != null) {
        Vector3 palmVel = lastHand.PalmVelocity.ToVector3();
        float speed = palmVel.magnitude;
        float multiplier = _material.ThrowingVelocityCurve.Evaluate(speed / _manager.SimulationScale);
        _rigidbody.velocity = palmVel * multiplier;
      }
    }
    #endregion

    #region UNITY CALLBACKS
#if UNITY_EDITOR
    private void OnCollisionEnter(Collision collision) {
      GameObject otherObj = collision.collider.gameObject;
      if (otherObj.GetComponentInParent<IHandModel>() != null
        && otherObj.GetComponentInParent<InteractionBrushHand>() == null) {
        string thisLabel = gameObject.name + " <layer " + LayerMask.LayerToName(gameObject.layer) + ">";
        string otherLabel = otherObj.name + " <layer " + LayerMask.LayerToName(otherObj.layer) + ">";
        Debug.LogError("For interaction to work properly please prevent collision between IHandModel and InteractionBehavior. " + thisLabel + ", " + otherLabel);
      }
    }
#endif

    protected virtual void OnDrawGizmos() {
      if (IsRegisteredWithManager) {
        Matrix4x4 gizmosMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(_warper.RigidbodyPosition, _warper.RigidbodyRotation, Vector3.one);

        if (_rigidbody.IsSleeping()) {
          Gizmos.color = Color.gray;
        } else if (IsBeingGrasped) {
          Gizmos.color = Color.green;
        } else if (_showDebugRecievedVelocity) {
          Gizmos.color = Color.yellow;
        } else if (_ignoringBrushes) {
          Gizmos.color = Color.red;
        } else {
          Gizmos.color = Color.blue;
        }

        Gizmos.DrawWireCube(_debugBounds.center, _debugBounds.size);

        Gizmos.matrix = gizmosMatrix;
      }
    }
    #endregion

    #region INTERNAL

    protected void updateLayer() {
      int layer;
      if (_ignoringBrushes || !_manager.ContactEnabled || !_material.ContactEnabled) {
        if (_material.UseCustomLayers) {
          layer = _material.InteractionNoClipLayer;
        } else {
          layer = _manager.InteractionNoClipLayer;
        }
      } else {
        if (_material.UseCustomLayers) {
          layer = _material.InteractionLayer;
        } else {
          layer = _manager.InteractionLayer;
        }
      }

      if (gameObject.layer != layer) {
        for (int i = 0; i < _childrenArray.Length; i++) {
          _childrenArray[i].gameObject.layer = layer;
        }
      }
    }

    protected INTERACTION_TRANSFORM getRigidbodyTransform() {
      INTERACTION_TRANSFORM interactionTransform = new INTERACTION_TRANSFORM();

      if (IsBeingGrasped) {
        interactionTransform.position = _solvedPosition.ToCVector();
        interactionTransform.rotation = _solvedRotation.ToCQuaternion();
      } else {
        interactionTransform.position = _warper.RigidbodyPosition.ToCVector();
        interactionTransform.rotation = _warper.RigidbodyRotation.ToCQuaternion();
      }

      interactionTransform.wallTime = Time.fixedTime;
      return interactionTransform;
    }

    protected void resetState() {
      _rigidbody.useGravity = _useGravity;
      _rigidbody.isKinematic = _isKinematic;
    }
    
    #endregion
  }
}
