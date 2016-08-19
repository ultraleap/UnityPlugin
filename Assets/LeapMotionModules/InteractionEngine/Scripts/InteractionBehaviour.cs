
using System;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// InteractionBehaviour is the default implementation of IInteractionBehaviour.
  /// </summary>
  ///
  /// <remarks>
  /// It has the following features:
  ///    - Extends from InteractionBehaviourBase to take advantage of it's bookkeeping and callbacks.
  ///    - Supports kinematic movement as well as physical, non-kinematic movement.
  ///    - When non-kinematic, supports pushing.
  ///    - Has the concept of a graphical anchor to reduce apparent latency between a hand moving and the object responding.
  ///      This can result in the graphical representation diverging slightly from the physical representation.
  ///    - Utilizes the Kabsch algorithm to determine how the object should rest in the hand when grabbed.
  ///      This allows more fidelity than simple rigid atatchment to the hand, as well as more intuitive multi-hand.
  ///      interaction.
  ///
  /// This default implementation has the following requirements:
  ///    - A Rigidbody is required.
  ///    - Kinematic movement must still be simulated via Rigidbody kinematic movement, as opposed to rigid movement of the Transform.
  ///    - Any non-continuous movement must be noted using the NotifyTeleported() method.
  ///    - Any forces or torques must be applied using the AddLinearAcceleration and AddAngularAcceleration methods instead of
  ///      the Rigidbody AddForce or AddTorque methods.
  ///    - Any runtime update of the kinematic or gravity status of the object must be done through setting the IsKinematic or UseGravity
  ///      properties of this behaviour instead of the properties on the Rigidbody component.
  /// </remarks>
  [SelectionBase]
  [RequireComponent(typeof(Rigidbody))]
  public partial class InteractionBehaviour : InteractionBehaviourBase {
    protected enum ContactMode {
      NORMAL = 0,  // Influenced by brushes and not by soft contact.
      SOFT = 1,    // Influenced by soft contact and not by brushes.  Will not return to NORMAL until no brush or soft contact remains.
      GRASPED = 2, // Not infuenced by either brushes or soft contact.  Returns to SOFT not NORMAL.
    };

    [SerializeField]
    protected InteractionMaterial _material;

    protected Transform[] _childrenArray;
    protected Rigidbody _rigidbody;

    // Rigidbody shadow state
    protected bool _isKinematic;
    protected bool _useGravity;
    protected float _drag;
    protected float _angularDrag;

    // Try to allow brushes to exit gracefully when passing fingers between objects.
    private const int DISLOCATED_BRUSH_COOLDOWN = 120;
    protected uint _dislocatedBrushCounter = DISLOCATED_BRUSH_COOLDOWN;
    protected ContactMode _contactMode = ContactMode.NORMAL;

    protected bool _recievedVelocityUpdate = false;
    protected float _minHandDistance = float.MaxValue;
    protected bool _notifiedOfTeleport = false;
    protected Vector3 _accumulatedLinearAcceleration = Vector3.zero;
    protected Vector3 _accumulatedAngularAcceleration = Vector3.zero;

    protected Vector3 _solvedPosition;
    protected Quaternion _solvedRotation;

    protected ControllerContainer _controllers;
    protected PhysicMaterialReplacer _materialReplacer;
    protected RigidbodyWarper _warper;

    #region PUBLIC METHODS

    public override bool IsBeingGrasped {
      get {
        Assert.IsTrue((_contactMode == ContactMode.GRASPED) == base.IsBeingGrasped);
        return _contactMode == ContactMode.GRASPED;
      }
    }

    /// <summary>
    /// Whether or not this InteractionBehaviour is Kinematic.  Always use this property instead
    /// of Rigidbody.IsKinematic because InteractionBehaviour overrides the kinematic status of the Rigidbody.
    /// </summary>
    public bool isKinematic {
      get {
        return _isKinematic;
      }
      set {
        _isKinematic = value;
        if (HasShapeInstance) {
          if (_contactMode != ContactMode.GRASPED) {
            _rigidbody.isKinematic = value;
          }
        } else {
          _rigidbody.isKinematic = value;
        }
      }
    }

    public ControllerContainer controllers {
      get {
        return _controllers;
      }
    }

    public
#if UNITY_EDITOR
      new
#endif
      Rigidbody rigidbody {
      get {
        return _rigidbody;
      }
    }

    public RigidbodyWarper warper {
      get {
        return _warper;
      }
    }

    public InteractionMaterial material {
      get {
        return _material;
      }
    }

    /// <summary>
    /// Whether or not this InteractionBehaviour uses Gravity.  Always use this property instead
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
    /// Whether or not this InteractionBehaviour was teleported in the current Unity frame. 
    /// The InteractionBehaviour sets this property to true when
    /// NotifyTeleported() is called, and resets the property to false once the interaction
    /// simulation has finished updating its internal representation.
    /// </summary>
    public bool WasTeleported {
      get {
        return _notifiedOfTeleport;
      }
    }

    /// <summary>
    /// Adds a linear acceleration to the center of mass of this object. 
    /// Use this instead of Rigidbody.AddForce() to accelerate an interactable object.
    /// </summary>
    public void AddLinearAcceleration(Vector3 acceleration) {
      _accumulatedLinearAcceleration += acceleration;
    }

    /// <summary>
    /// Adds an angular acceleration to the center of mass of this object. 
    /// Use this instead of Rigidbody.AddTorque() to add angular acceleration 
    /// to an interactable object
    /// </summary>
    public void AddAngularAcceleration(Vector3 acceleration) {
      _accumulatedAngularAcceleration += acceleration;
    }

    /// <summary>
    /// This method should always be called if the object is teleported to a new 
    /// location instead of moving there through applied forces or collisions. If
    /// this method is not called, teleporting objects can cause the simulation to 
    /// become unstable.
    /// </summary>
    public void NotifyTeleported() {
      _notifiedOfTeleport = true;
    }
    #endregion

    public override bool IsAbleToBeDeactivated() {
      return _contactMode == ContactMode.NORMAL && UntrackedHandCount == 0;
    }

    #region INTERACTION CALLBACKS

    protected override void OnRegistered() {
      base.OnRegistered();

      _controllers = new ControllerContainer(this, _material);

      _materialReplacer = new PhysicMaterialReplacer(transform, _material);
      _warper = new RigidbodyWarper(_manager, transform, _rigidbody, _material.GraphicalReturnTime);

      _childrenArray = GetComponentsInChildren<Transform>(true);

      _contactMode = ContactMode.NORMAL;
      updateLayer();
    }

    protected override void OnUnregistered() {
      base.OnUnregistered();

      Assert.IsTrue(UntrackedHandCount == 0);

      // Ditch this object in the layer that doesn't collide with brushes in case they are still embedded.
      _contactMode = ContactMode.SOFT;
      updateLayer();

      _warper.Dispose();
      _warper = null;

      revertRigidbodyState();
    }

    protected override void OnPreSolve() {
      base.OnPreSolve();
      _recievedVelocityUpdate = false;
      
      ++_dislocatedBrushCounter;
      _minHandDistance = float.MaxValue;

#if UNITY_EDITOR
      if (_contactMode == ContactMode.GRASPED && UntrackedHandCount == 0 &&
          Vector3.Distance(_solvedPosition, _warper.RigidbodyPosition) > _material.ReleaseDistance * _manager.SimulationScale ||
          Quaternion.Angle(_solvedRotation, _warper.RigidbodyRotation) > _material.ReleaseAngle) {
        _manager.ReleaseObject(this);
      }
#endif
    }

    protected override void OnPostSolve() {
      base.OnPostSolve();

      // Material already replaced in OnGraspBegin
      if (_contactMode != ContactMode.GRASPED) {
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

        if (_recievedVelocityUpdate || _minHandDistance <= 0.0f) {
          // Shapes in the contact graph of a hand do not bounce.
          _materialReplacer.ReplaceMaterials();
        } else {
          // Shapes in the contact graph of a hand do not bounce.
          _materialReplacer.RevertMaterials();
        }
      }

      //Reset so we can accumulate for the next frame
      _accumulatedLinearAcceleration = Vector3.zero;
      _accumulatedAngularAcceleration = Vector3.zero;
      _minHandDistance = float.MaxValue;
      _notifiedOfTeleport = false;
    }

    public override void GetInteractionShapeCreationInfo(out INTERACTION_CREATE_SHAPE_INFO createInfo, out INTERACTION_TRANSFORM createTransform) {
      createInfo = new INTERACTION_CREATE_SHAPE_INFO();
      createInfo.shapeFlags = ShapeInfoFlags.None;

      createTransform = getRigidbodyTransform();
    }

    protected override void OnInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle) {
      base.OnInteractionShapeCreated(instanceHandle);

      _solvedPosition = _rigidbody.position;
      _solvedRotation = _rigidbody.rotation;

      updateLayer();
    }

    protected override void OnInteractionShapeDestroyed() {
      base.OnInteractionShapeDestroyed();
      updateContactMode();
      revertRigidbodyState();
    }

    public override void GetInteractionShapeUpdateInfo(out INTERACTION_UPDATE_SHAPE_INFO updateInfo, out INTERACTION_TRANSFORM interactionTransform) {
      updateInfo = new INTERACTION_UPDATE_SHAPE_INFO();

      updateInfo.updateFlags = UpdateInfoFlags.VelocityEnabled;
      updateInfo.linearVelocity = _rigidbody.velocity.ToCVector();
      updateInfo.angularVelocity = _rigidbody.angularVelocity.ToCVector();

      if (_isKinematic) {
        updateInfo.updateFlags |= UpdateInfoFlags.Kinematic;
      } else {
        // Generates notifications even when hands are no longer influencing
        if (_contactMode == ContactMode.SOFT) {
          updateInfo.updateFlags |= UpdateInfoFlags.SoftContact;
        }

        // All forms of acceleration.
        if (_contactMode != ContactMode.GRASPED) {
          updateInfo.updateFlags |= UpdateInfoFlags.AccelerationEnabled;
          updateInfo.linearAcceleration = _accumulatedLinearAcceleration.ToCVector();
          updateInfo.angularAcceleration = _accumulatedAngularAcceleration.ToCVector();

          if (_useGravity) {
            updateInfo.updateFlags |= UpdateInfoFlags.GravityEnabled;
          }
        }
      }

      interactionTransform = getRigidbodyTransform();
    }

    protected override void OnRecievedSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results) {
      base.OnRecievedSimulationResults(results);

      // Velocities can propagate even when not able to contact the hand.
      if (_contactMode != ContactMode.GRASPED && (results.resultFlags & ShapeInstanceResultFlags.Velocities) != 0) {
        Assert.IsFalse(_isKinematic);
        _rigidbody.velocity = results.linearVelocity.ToVector3();
        _rigidbody.angularVelocity = results.angularVelocity.ToVector3();
        _recievedVelocityUpdate = true;
      }

      if ((results.resultFlags & ShapeInstanceResultFlags.MaxHand) != 0) {
        _minHandDistance = results.minHandDistance;
      }

      updateContactMode();
    }

    protected override void OnHandGrasped(Hand hand) {
      base.OnHandGrasped(hand);

      _controllers.HoldingPoseController.AddHand(hand);
    }

    protected override void OnHandsHoldPhysics(ReadonlyList<Hand> hands) {
      base.OnHandsHoldPhysics(hands);

      PhysicsMoveInfo info = new PhysicsMoveInfo();
      info.remainingDistanceLastFrame = Vector3.Distance(_warper.RigidbodyPosition, _solvedPosition);
      info.shouldTeleport = _notifiedOfTeleport;

      _controllers.HoldingPoseController.GetHoldingPose(hands, out _solvedPosition, out _solvedRotation);

      _controllers.MoveToController.MoveTo(hands, info, _solvedPosition, _solvedRotation);

      if (_controllers.ThrowingController != null) {
        _controllers.ThrowingController.OnHold(hands);
      }
    }

    protected override void OnHandsHoldGraphics(ReadonlyList<Hand> hands) {
      base.OnHandsHoldGraphics(hands);

      if (_material.WarpingEnabled) {
        Vector3 deltaPosition = Quaternion.Inverse(_solvedRotation) * (_warper.RigidbodyPosition - _solvedPosition);
        Quaternion deltaRotation = Quaternion.Inverse(_solvedRotation) * _warper.RigidbodyRotation;

        Vector3 newPosition;
        Quaternion newRotation;
        _controllers.HoldingPoseController.GetHoldingPose(hands, out newPosition, out newRotation);

        Vector3 graphicalPosition = newPosition + newRotation * deltaPosition;
        Quaternion graphicalRotation = newRotation * deltaRotation;

        _warper.WarpPercent = _material.WarpCurve.Evaluate(deltaPosition.magnitude / _manager.SimulationScale);
        _warper.SetGraphicalPosition(graphicalPosition, graphicalRotation);
      }
    }

    protected override void OnHandReleased(Hand hand) {
      base.OnHandReleased(hand);

      _controllers.HoldingPoseController.RemoveHand(hand);
    }

    protected override void OnHandLostTracking(Hand oldHand, out float maxSuspensionTime) {
      base.OnHandLostTracking(oldHand, out maxSuspensionTime);

      if (_controllers.SuspensionController == null) {
        maxSuspensionTime = 0;
      } else {
        maxSuspensionTime = _controllers.SuspensionController.MaxSuspensionTime;
        _controllers.SuspensionController.Suspend();
      }

    }

    protected override void OnHandRegainedTracking(Hand newHand, int oldId) {
      base.OnHandRegainedTracking(newHand, oldId);

      if (_controllers.SuspensionController != null) {
        _controllers.SuspensionController.Resume();
      }

      _controllers.HoldingPoseController.TransferHandId(oldId, newHand.Id);

      _controllers.MoveToController.SetGraspedState();

      NotifyTeleported();
    }

    protected override void OnHandTimeout(Hand oldHand) {
      base.OnHandTimeout(oldHand);

      if (_controllers.SuspensionController != null) {
        _controllers.SuspensionController.Timeout();
      }

      _controllers.HoldingPoseController.RemoveHand(oldHand);
    }

    protected override void OnGraspBegin() {
      base.OnGraspBegin();

      _controllers.MoveToController.OnGraspBegin();
      _controllers.MoveToController.SetGraspedState();

      _materialReplacer.ReplaceMaterials();
      updateContactMode();
    }

    protected override void OnGraspEnd(Hand lastHand) {
      base.OnGraspEnd(lastHand);

      _controllers.MoveToController.OnGraspEnd();

      if (_controllers.ThrowingController != null && lastHand != null) {
        _controllers.ThrowingController.OnThrow(lastHand);
      }

      revertRigidbodyState();

      // Transition to soft contact when exiting grasp.  This is because the fingers
      // are probably embedded.
      _dislocatedBrushCounter = 0;
      updateContactMode();
    }
    #endregion

    #region BRUSH CALLBACKS

    public override void NotifyBrushDislocated() {
      _dislocatedBrushCounter = 0;
      updateContactMode();
    }

    #endregion

    #region UNITY CALLBACKS

    protected override void Awake() {
      base.Awake();

      _rigidbody = GetComponent<Rigidbody>();
      if (_rigidbody == null) {
        //Should only happen if the user has done some trickery since there is a RequireComponent attribute
        enabled = false;
        throw new InvalidOperationException("InteractionBehaviour must have a Rigidbody component attached to it.");
      }
      _rigidbody.maxAngularVelocity = float.PositiveInfinity;

      //Copy over existing settings for defaults
      _isKinematic = _rigidbody.isKinematic;
      _useGravity = _rigidbody.useGravity;
      _drag = _rigidbody.drag;
      _angularDrag = _rigidbody.angularDrag;

      CheckMaterial();
    }

    protected override void Reset() {
      base.Reset();
      CheckMaterial();
    }

    private void CheckMaterial() {
      if (_material == null) {
        if (_manager == null) {
          return;
        } else {
          Debug.LogWarning("No InteractionMaterial specified; will use the default InteractionMaterial as specified by the InteractionManager.");
          _material = _manager.DefaultInteractionMaterial;
        }
      }
    }

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
    #endregion

    #region INTERNAL

    protected void updateContactMode() {
      ContactMode desiredContactMode = ContactMode.NORMAL;
      if (base.IsBeingGrasped) {
        desiredContactMode = ContactMode.GRASPED;
      } else if (_dislocatedBrushCounter < DISLOCATED_BRUSH_COOLDOWN || (_contactMode != ContactMode.NORMAL && _minHandDistance <= 0.0f)) {
        desiredContactMode = ContactMode.SOFT;
      }

      if (_contactMode != desiredContactMode) {
        _contactMode = desiredContactMode;
        updateLayer();
      }

      Assert.IsTrue((_contactMode == ContactMode.GRASPED) == base.IsBeingGrasped);
    }

    protected void updateLayer() {
      int layer;
      if (_controllers.LayerController != null) {
        if (_contactMode != ContactMode.NORMAL) {
          layer = _controllers.LayerController.InteractionNoClipLayer;
        } else {
          layer = _controllers.LayerController.InteractionLayer;
        }
      } else {
        if (_contactMode != ContactMode.NORMAL) {
          layer = _manager.InteractionNoClipLayer;
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

    protected virtual void revertRigidbodyState() {
      if (_rigidbody.useGravity != _useGravity) {
        _rigidbody.useGravity = _useGravity;
      }
      if (_rigidbody.isKinematic != _isKinematic) {
        _rigidbody.isKinematic = _isKinematic;
      }
      if (_rigidbody.drag != _drag) {
        _rigidbody.drag = _drag;
      }
      if (_rigidbody.angularDrag != _angularDrag) {
        _rigidbody.angularDrag = _angularDrag;
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
    #endregion
  }
}
