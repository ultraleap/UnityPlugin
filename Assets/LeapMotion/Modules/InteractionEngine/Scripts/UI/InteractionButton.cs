/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Attributes;
using Leap.Unity.Interaction.Internal;
using Leap.Unity.Query;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Leap.Unity.Interaction {

  ///<summary>
  /// A physics-enabled button. Activation is triggered by physically pushing the button
  /// back to its compressed position.
  ///</summary>
  public class InteractionButton : InteractionBehaviour {

    [Header("UI Control")]
    [Tooltip("When set to false, this UI control will not be functional. Use this instead "
           + "of disabling the component itself when you want to disable the user's "
           + "ability to affect this UI control while keeping the GameObject active and, "
           + "for example, rendering, and able to receive primaryHover state.")]
    [SerializeField, FormerlySerializedAs("controlEnabled")]
    private bool _controlEnabled = true;
    public bool controlEnabled {
      get { return _controlEnabled; }
      set { _controlEnabled = value; }
    }

    public enum StartingPositionMode {
      Depressed,
      Relaxed
    }

    [Header("Motion Configuration")]

    [EditTimeOnly]
    public StartingPositionMode startingPositionMode = StartingPositionMode.Depressed;

    ///<summary> The minimum and maximum heights the button can exist at. </summary>
    [Tooltip("The minimum and maximum heights the button can exist at.")]
    public Vector2 minMaxHeight = new Vector2(0f, 0.02f);

    ///<summary> The height that this button rests at; this value is a lerp in between the min and max height. </summary>
    [Tooltip("The height that this button rests at; this value is a lerp in between the min and max height.")]
    [Range(0f, 1f)]
    public float restingHeight = 0.5f;

    [Range(0, 1)]
    [SerializeField]
    private float _springForce = 0.1f;

    // State Events
    [SerializeField]
    [FormerlySerializedAs("OnPress")]
    private UnityEvent _OnPress = new UnityEvent();
    [SerializeField]
    [FormerlySerializedAs("OnUnpress")]
    private UnityEvent _OnUnpress = new UnityEvent();

    public Action OnPress = () => { };
    public Action OnUnpress = () => { };

    public float springForce {
      get {
        return _springForce;
      }
      set {
        _springForce = value;
      }
    }

    //Public State variables
    ///<summary> Gets whether the button is currently held down. </summary>
    public bool isDepressed { get; protected set; }
    ///<summary> Gets whether the button is currently held down. </summary>
    public bool isPressed { get { return isDepressed; } }

    ///<summary> Gets whether the button was pressed during this Update frame. </summary>
    public bool depressedThisFrame { get; protected set; }

    ///<summary> Gets whether the button was unpressed during this Update frame. </summary>
    public bool unDepressedThisFrame { get; protected set; }

    private float _depressedAmount = 0F;
    /// <summary>
    /// Gets a normalized value between 0 and 1 based on how depressed the button currently
    /// is relative to its maximum depression. 0 represents a button fully at rest or pulled
    /// out beyond its resting position; 1 represents a fully-depressed button.
    /// </summary>
    public float depressedAmount { get { return _depressedAmount; } }

    // Protected State Variables

    ///<summary> The initial position of this element in local space, stored upon Start() </summary>
    protected Vector3 initialLocalPosition;

    ///<summary> The physical position of this element in local space; may diverge from the graphical position. </summary>
    protected Vector3 localPhysicsPosition;

    ///<summary> The physical position of this element in world space; may diverge from the graphical position. </summary>
    protected Vector3 physicsPosition = Vector3.zero;

    /// <summary>
    /// Returns the local position of this button when it is able to relax into its target
    /// position.
    /// </summary>
    public virtual Vector3 RelaxedLocalPosition {
      get {
        return initialLocalPosition + Vector3.back * Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight);
      }
    }

    private Rigidbody _lastDepressor;
    private Vector3 _localDepressorPosition;
    private Vector3 _physicsVelocity = Vector3.zero;
    private bool _physicsOccurred;
    private bool _initialIgnoreGrasping = false;
    private Quaternion _initialLocalRotation;
    private InteractionController _lockedInteractingController = null;

    protected override void Start() {
      if(transform == transform.root) {
        Debug.LogError("This button has no parent!  Please ensure that it is parented to something!", this);
        enabled = false;
      }

      // Initialize Positions
      initialLocalPosition = transform.localPosition;
      if (startingPositionMode == StartingPositionMode.Relaxed) {
        initialLocalPosition = transform.localPosition + Vector3.forward * Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight);
      }

      transform.localPosition = initialLocalPosition + Vector3.back * Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight);
      localPhysicsPosition = transform.localPosition;
      physicsPosition = transform.position;
      rigidbody.position = physicsPosition;
      _initialIgnoreGrasping = ignoreGrasping;
      _initialLocalRotation = transform.localRotation;

      //Add a custom grasp controller
      OnGraspBegin += onGraspBegin;
      OnGraspEnd += onGraspEnd;

      OnPress += _OnPress.Invoke;
      OnUnpress += _OnUnpress.Invoke;

      base.Start();
    }

    protected virtual void FixedUpdate() {
      if (!_physicsOccurred) {
        _physicsOccurred = true;

        if (!isGrasped && !rigidbody.IsSleeping()) {
          //Sleep the rigidbody if it's not really moving...

          float localPhysicsDisplacementPercentage = Mathf.InverseLerp(minMaxHeight.x, minMaxHeight.y, initialLocalPosition.z - localPhysicsPosition.z);
          if (rigidbody.position == physicsPosition && _physicsVelocity == Vector3.zero && Mathf.Abs(localPhysicsDisplacementPercentage - restingHeight) < 0.01F) {
            rigidbody.Sleep();
            //Else, reset the body's position to where it was last time PhysX looked at it...
          } else {
            if (_physicsVelocity.ContainsNaN()) {
              _physicsVelocity = Vector3.zero;
            }

            rigidbody.position = physicsPosition;
            rigidbody.velocity = _physicsVelocity;
          }
        }
      }
    }

    private const float FRICTION_COEFFICIENT = 30F;
    private const float DRAG_COEFFICIENT = 50F;

    protected virtual void Update() {
      //Reset our convenience state variables...
      depressedThisFrame = false;
      unDepressedThisFrame = false;

      //Disable collision on this button if it is not the primary hover
      ignoreGrasping = _initialIgnoreGrasping ? true : !isPrimaryHovered && !isGrasped;
      ignoreContact = (!isPrimaryHovered || isGrasped) || !controlEnabled;

      //Enforce local rotation (if button is child of non-kinematic rigidbody, this is necessary)
      transform.localRotation = _initialLocalRotation;

      //Apply physical corrections only if PhysX has modified our positions
      if (_physicsOccurred) {
        _physicsOccurred = false;

        //Record and enforce the sliding state from the previous frame
        if (isPrimaryHovered || isGrasped) {
          localPhysicsPosition = getDepressedConstrainedLocalPosition(transform.parent.InverseTransformPoint(rigidbody.position) - localPhysicsPosition);
        } else {
          Vector2 localSlidePosition = new Vector2(localPhysicsPosition.x, localPhysicsPosition.y);
          localPhysicsPosition = transform.parent.InverseTransformPoint(rigidbody.position);
          localPhysicsPosition = new Vector3(localSlidePosition.x, localSlidePosition.y, localPhysicsPosition.z);
        }

        // Calculate the physical kinematics of the button in local space
        Vector3 localPhysicsVelocity = transform.parent.InverseTransformVector(rigidbody.velocity);
        if (isDepressed && isPrimaryHovered && _lastDepressor != null) {
          Vector3 curLocalDepressorPos = transform.parent.InverseTransformPoint(_lastDepressor.position);
          Vector3 origLocalDepressorPos = transform.parent.InverseTransformPoint(transform.TransformPoint(_localDepressorPosition));
          localPhysicsVelocity = Vector3.back * 0.05f;
          localPhysicsPosition = getDepressedConstrainedLocalPosition(curLocalDepressorPos - origLocalDepressorPos);
        }
        else if (isGrasped) {
          // Do nothing!
        }
        else {
          Vector3 originalLocalVelocity = localPhysicsVelocity;

          // Spring force
          localPhysicsVelocity += Mathf.Clamp(_springForce * 10000F * (initialLocalPosition.z - Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight) - localPhysicsPosition.z), -100f / transform.parent.lossyScale.x, 100f / transform.parent.lossyScale.x)
                                              * Time.fixedDeltaTime
                                              * Vector3.forward;

          // Friction & Drag
          float velMag = originalLocalVelocity.magnitude;
          if (velMag > 0F) {
            Vector3 resistanceDir = -originalLocalVelocity / velMag;

            // Friction force
            Vector3 frictionForce = resistanceDir * velMag * FRICTION_COEFFICIENT;
            localPhysicsVelocity += (frictionForce /* assume unit mass */ * Time.fixedDeltaTime * transform.parent.lossyScale.x);

            // Drag force
            float velSqrMag = velMag * velMag;
            Vector3 dragForce = resistanceDir * velSqrMag * DRAG_COEFFICIENT;
            localPhysicsVelocity += (dragForce /* assume unit mass */ * Time.fixedDeltaTime * transform.parent.lossyScale.x);
          }
        }

        // Transform the local physics back into world space
        physicsPosition = transform.parent.TransformPoint(localPhysicsPosition);
        _physicsVelocity = transform.parent.TransformVector(localPhysicsVelocity);

        // Calculate the Depression State of the Button from its Physical Position
        // Set its Graphical Position to be Constrained Physically
        bool oldDepressed = isDepressed;

        // Normalized depression amount.
        _depressedAmount = localPhysicsPosition.z.Map(initialLocalPosition.z - minMaxHeight.x,
          initialLocalPosition.z - Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight),
          1F, 0F);

        // If the button is depressed past its limit...
        if (localPhysicsPosition.z > initialLocalPosition.z - minMaxHeight.x) {
          transform.localPosition = new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, initialLocalPosition.z - minMaxHeight.x);
          if ((isPrimaryHovered && _lastDepressor != null) || isGrasped) {
            isDepressed = true;
          }
          else {
            physicsPosition = transform.parent.TransformPoint(new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, initialLocalPosition.z - minMaxHeight.x));
            _physicsVelocity = _physicsVelocity * 0.1f;
            isDepressed = false;
            _lastDepressor = null;
          }
          // Else if the button is extended past its limit...
        }
        else if (localPhysicsPosition.z < initialLocalPosition.z - minMaxHeight.y) {
          transform.localPosition = new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, initialLocalPosition.z - minMaxHeight.y);
          physicsPosition = transform.position;
          isDepressed = false;
          _lastDepressor = null;
        }
        else {
          // Else, just make the physical and graphical motion of the button match
          transform.localPosition = localPhysicsPosition;

          // Allow some hysteresis before setting isDepressed to false.
          if (!isDepressed
              || !(localPhysicsPosition.z > initialLocalPosition.z - (minMaxHeight.y - minMaxHeight.x) * 0.1F)) {
            isDepressed = false;
            _lastDepressor = null;
          }
        }

        // If our depression state has changed since last time...
        if (isDepressed && !oldDepressed) {
          primaryHoveringController.primaryHoverLocked = true;
          _lockedInteractingController = primaryHoveringController;

          OnPress();
          depressedThisFrame = true;

        } else if (!isDepressed && oldDepressed) {
          unDepressedThisFrame = true;
          OnUnpress();

          if (!(isGrasped && graspingController == _lockedInteractingController)) {
            _lockedInteractingController.primaryHoverLocked = false;
          }

          _lastDepressor = null;
        }
      }
    }

    // How the button should behave when it is depressed
    protected virtual Vector3 getDepressedConstrainedLocalPosition(Vector3 desiredOffset) {
      return new Vector3(initialLocalPosition.x, initialLocalPosition.y, localPhysicsPosition.z + desiredOffset.z);
    }

    protected virtual void onGraspBegin() {
      primaryHoveringController.LockPrimaryHover(this);
      _lockedInteractingController = primaryHoveringController;
    }

    protected virtual void onGraspEnd() {
      if (localPhysicsPosition.z > initialLocalPosition.z - minMaxHeight.x) {
        transform.localPosition = new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, initialLocalPosition.z - minMaxHeight.x);
        _physicsVelocity = _physicsVelocity * 0.1f;
      }

      if (_lockedInteractingController != null && !isDepressed) {
        _lockedInteractingController.primaryHoverLocked = false;
        _lockedInteractingController = null;
      }
    }

    protected virtual void OnCollisionEnter(Collision collision) { trySetDepressor(collision.collider); }
    protected virtual void OnCollisionStay(Collision collision) { trySetDepressor(collision.collider); }

    // during Soft Contact, controller colliders are triggers
    protected virtual void OnTriggerEnter(Collider collider) { trySetDepressor(collider); }
    protected virtual void OnTriggerStay(Collider collider) { trySetDepressor(collider); }

    // Try grabbing the offset between the fingertip and this object...
    private void trySetDepressor(Collider collider) {
      if (collider.attachedRigidbody != null && _lastDepressor == null && (localPhysicsPosition.z > initialLocalPosition.z - minMaxHeight.x)
        && (manager.contactBoneBodies.ContainsKey(collider.attachedRigidbody)
            && !this.ShouldIgnoreHover(manager.contactBoneBodies[collider.attachedRigidbody].interactionController))) {
        _lastDepressor = collider.attachedRigidbody;
        _localDepressorPosition = transform.InverseTransformPoint(collider.attachedRigidbody.position);
      }
    }

    public void setMinHeight(float minHeight) {
      minMaxHeight = new Vector2(Mathf.Min(minMaxHeight.y, minHeight), minMaxHeight.y);
    }
    public void setMaxHeight(float maxHeight) {
      minMaxHeight = new Vector2(minMaxHeight.x, Mathf.Max(minMaxHeight.x, maxHeight));
    }

    protected override void OnDisable() {
      if (isDepressed) {
        unDepressedThisFrame = true;
        OnUnpress();

        if (_lockedInteractingController != null) {
          _lockedInteractingController.primaryHoverLocked = false;
        }
      }

      base.OnDisable();
    }

    protected virtual void OnDrawGizmosSelected() {
      if (transform.parent != null) {
        Gizmos.matrix = transform.parent.localToWorldMatrix;
        Vector2 heights = minMaxHeight;
        Vector3 originPosition = Application.isPlaying ? initialLocalPosition : transform.localPosition;
        if (!Application.isPlaying && startingPositionMode == StartingPositionMode.Relaxed) {
          originPosition = transform.localPosition + Vector3.forward * Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(originPosition + (Vector3.back * heights.x), originPosition + (Vector3.back * heights.y));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(originPosition + (Vector3.back * heights.x), originPosition + (Vector3.back * Mathf.Lerp(heights.x, heights.y, restingHeight)));
      }
    }

    void Reset() {
      contactForceMode = ContactForceMode.UI;
      graspedMovementType = GraspedMovementType.Nonkinematic;

      startingPositionMode = StartingPositionMode.Relaxed;

      rigidbody = GetComponent<Rigidbody>();
      if (rigidbody != null) {
        rigidbody.useGravity = false;
      }
    }
  }
}
