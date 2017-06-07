/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

﻿using Leap.Unity.Interaction.Internal;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Leap.Unity.Query;

namespace Leap.Unity.Interaction {

  ///<summary>
  /// A physics-enabled button. Activation is triggered by physically pushing the button
  /// back to its compressed position.
  ///</summary>
  public class InteractionButton : InteractionBehaviour {

    [Header("Motion Configuration")]

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
    public UnityEvent OnPress = new UnityEvent();
    public UnityEvent OnUnpress = new UnityEvent();

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

    ///<summary> Gets whether the button was pressed during this Update frame. </summary>
    public bool depressedThisFrame { get; protected set; }

    ///<summary> Gets whether the button was unpressed during this Update frame. </summary>
    public bool unDepressedThisFrame { get; protected set; }

    // Protected State Variables

    ///<summary> The initial position of this element in local space, stored upon Start() </summary>
    protected Vector3 initialLocalPosition;

    ///<summary> The physical position of this element in local space; may diverge from the graphical position. </summary>
    protected Vector3 localPhysicsPosition;

    ///<summary> The physical position of this element in world space; may diverge from the graphical position. </summary>
    protected Vector3 physicsPosition = Vector3.zero;

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
      transform.localPosition = initialLocalPosition + Vector3.back * Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight);
      localPhysicsPosition = transform.localPosition;
      physicsPosition = transform.position;
      rigidbody.position = physicsPosition;
      _initialIgnoreGrasping = ignoreGrasping;
      _initialLocalRotation = transform.localRotation;

      //Add a custom grasp controller
      OnGraspedMovement += onGraspedMovement;
      OnGraspEnd += onGraspEnd;

      base.Start();
    }

    protected virtual void FixedUpdate() {
      if (!_physicsOccurred) {
        _physicsOccurred = true;

        if (!rigidbody.IsSleeping()) {
          //Sleep the rigidbody if it's not really moving...

          float localPhysicsDisplacementPercentage = Mathf.InverseLerp(minMaxHeight.x, minMaxHeight.y, initialLocalPosition.z - localPhysicsPosition.z);
          if (rigidbody.position == physicsPosition && _physicsVelocity == Vector3.zero && Mathf.Abs(localPhysicsDisplacementPercentage - restingHeight) < 0.01f) {
            rigidbody.Sleep();
            //Else, reset the body's position to where it was last time PhysX looked at it...
          } else {
            rigidbody.position = physicsPosition;
            rigidbody.velocity = _physicsVelocity;
          }
        }
      }
    }

    protected virtual void Update() {
      //Reset our convenience state variables...
      depressedThisFrame = false;
      unDepressedThisFrame = false;

      //Disable collision on this button if it is not the primary hover
      ignoreGrasping = _initialIgnoreGrasping ? true : !isPrimaryHovered && !isGrasped;
      ignoreContact = !isPrimaryHovered || isGrasped;

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
        } else {
          localPhysicsVelocity += Mathf.Clamp(_springForce * (initialLocalPosition.z - Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight) - localPhysicsPosition.z) / Time.fixedDeltaTime, -1f, 1f) * Vector3.forward;
          localPhysicsVelocity *= Mathf.Pow(0.0000000001f, Time.fixedDeltaTime);
        }

        // Transform the local physics back into world space
        physicsPosition = transform.parent.TransformPoint(localPhysicsPosition);
        _physicsVelocity = transform.parent.TransformVector(localPhysicsVelocity);

        // Calculate the Depression State of the Button from its Physical Position
        // Set its Graphical Position to be Constrained Physically
        bool oldDepressed = isDepressed;

        // If the button is depressed past its limit...
        if (localPhysicsPosition.z > initialLocalPosition.z - minMaxHeight.x) {
          transform.localPosition = new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, initialLocalPosition.z - minMaxHeight.x);
          if ((isPrimaryHovered && _lastDepressor!=null) || isGrasped) {
            isDepressed = true;
          } else {
            physicsPosition = transform.parent.TransformPoint(new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, initialLocalPosition.z - minMaxHeight.x));
            _physicsVelocity = _physicsVelocity * 0.1f;
            isDepressed = false;
            _lastDepressor = null;
          }
          // Else if the button is extended past its limit...
        } else if (localPhysicsPosition.z < initialLocalPosition.z - minMaxHeight.y) {
          transform.localPosition = new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, initialLocalPosition.z - minMaxHeight.y);
          physicsPosition = transform.position;
          isDepressed = false;
          _lastDepressor = null;
        } else {
          // Else, just make the physical and graphical motion of the button match
          transform.localPosition = localPhysicsPosition;
          isDepressed = false;
          _lastDepressor = null;
        }

        // If our depression state has changed since last time...
        if (isDepressed && !oldDepressed) {
          OnPress.Invoke();
          depressedThisFrame = true;

          primaryHoveringController.primaryHoverLocked = true;
          _lockedInteractingController = primaryHoveringController;

        } else if (!isDepressed && oldDepressed) {
          unDepressedThisFrame = true;
          OnUnpress.Invoke();
          _lockedInteractingController.primaryHoverLocked = false;
          _lastDepressor = null;
        }
      }
    }

    // How the button should behave when it is depressed
    protected virtual Vector3 getDepressedConstrainedLocalPosition(Vector3 desiredOffset) {
      return new Vector3(initialLocalPosition.x, initialLocalPosition.y, localPhysicsPosition.z + desiredOffset.z);
    }

    protected virtual void onGraspedMovement(Vector3 preSolvedPosition, Quaternion preSolvedRotation,
                                             Vector3 postSolvedPosition, Quaternion postSolvedRotation,
                                             List<InteractionController> graspingControllers) {
      Vector3 newLocalPosition = getDepressedConstrainedLocalPosition(transform.parent.InverseTransformVector(postSolvedPosition - preSolvedPosition));
      newLocalPosition.z = Mathf.Clamp(newLocalPosition.z, initialLocalPosition.z - minMaxHeight.y, initialLocalPosition.z - minMaxHeight.x);
      _physicsVelocity = 0.5f * (transform.parent.TransformPoint(newLocalPosition) - physicsPosition) / Time.fixedDeltaTime;
    }

    protected virtual void onGraspEnd() {
      if (localPhysicsPosition.z > initialLocalPosition.z - minMaxHeight.x) {
        transform.localPosition = new Vector3(localPhysicsPosition.x, localPhysicsPosition.y, initialLocalPosition.z - minMaxHeight.x);
        _physicsVelocity = _physicsVelocity * 0.1f;
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
        OnUnpress.Invoke();

        _lockedInteractingController.primaryHoverLocked = false;
      }

      base.OnDisable();
    }

    protected virtual void OnDrawGizmosSelected() {
      if (transform.parent != null) {
        Gizmos.matrix = transform.parent.localToWorldMatrix;
        Vector2 heights = minMaxHeight;
        Vector3 originPosition = Application.isPlaying ? initialLocalPosition : transform.localPosition;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(originPosition + (Vector3.back * heights.x), originPosition + (Vector3.back * heights.y));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(originPosition + (Vector3.back * heights.x), originPosition + (Vector3.back * Mathf.Lerp(heights.x, heights.y, restingHeight)));
      }
    }

    void Reset() {
      contactForceMode = ContactForceMode.UI;
      graspedMovementType = GraspedMovementType.Nonkinematic;

      if (rigidbody != null) {
        rigidbody.useGravity = false;
      }
    }
  }
}
