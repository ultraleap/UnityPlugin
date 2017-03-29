using Leap;
using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public abstract class InteractionBehaviourBase : MonoBehaviour {

    public const float MAX_ANGULAR_VELOCITY = 100F;

    public InteractionManager interactionManager;

    protected Rigidbody _body;
    /// <summary> The rigidbody associated with this interaction object. </summary>
    public new Rigidbody rigidbody { get { return _body; } }

    [Header("Interaction Overrides")]
    [Tooltip("This object will no longer receive hover callbacks if this property is checked.")]
    public bool ignoreHover = false;
    [Tooltip("Hands will not be able to touch this object if this property is checked.")]
    public bool ignoreContact = false;
    [Tooltip("Hands will not be able to grasp this object if this property is checked.")]
    public bool ignoreGrasping = false;

    [Header("Grasp Settings")]
    [Tooltip("Can this object be grasped with two or more hands?")]
    public bool allowMultiGrasp = false;

    //[Tooltip("Trigger colliders on InteractionBehaviours can only be poked or pushed by hands "
    //       + "if this property is enabled.")]
    //public bool allowContactOnTriggers = true;

    /// <summary>
    /// Called by the InteractionManager every FixedUpdate, after
    /// InteractionHands have FixedUpdated.
    /// 
    /// InteractionBehaviour uses this to fire per-object interaction events.
    /// </summary>
    public abstract void FixedUpdateObject();

    protected virtual void Awake() {
      _body = GetComponent<Rigidbody>();
      _body.maxAngularVelocity = MAX_ANGULAR_VELOCITY;
    }

    protected virtual void OnEnable() {
      if (interactionManager == null) {
        interactionManager = InteractionManager.singleton;

        if (interactionManager == null) {
          Debug.LogError("Interaction Behaviours require an Interaction Manager. Please ensure you have an InteractionManager in your scene.");
          this.enabled = false;
        }
      }

      if (interactionManager != null && !interactionManager.IsBehaviourRegistered(this)) {
        interactionManager.RegisterInteractionBehaviour(this);
      }
    }

    protected virtual void OnValidate() {
      _body = GetComponent<Rigidbody>();
    }

    /// <summary> Return the distance the interaction object is from the given world position. </summary>
    public abstract float GetDistance(Vector3 worldPosition);

    #region Hovering

    /// <summary> Called per-hand when that hand is nearby this object. </summary>
    public abstract void HoverBegin(Hand hand);

    /// <summary> Called per-hand every frame after the first when that hand is nearby this object. </summary>
    public abstract void HoverStay(Hand hand);

    /// <summary> Called per-hand when that hand is no longer near this object. </summary>
    public abstract void HoverEnd(Hand hand);

    /// <summary> As HoverBegin, but only for the hand that is closest to this object.
    /// If a new hand becomes the closest hand, the old hand will get PrimaryHoverEnd before the new hand
    /// gets PrimaryHoverBegin. </summary>
    public abstract void PrimaryHoverBegin(Hand hand);

    /// <summary> As HoverStay, but only for the hand that is closest to this object. </summary>
    public abstract void PrimaryHoverStay(Hand hand);

    /// <summary> As HoverEnd, but only for the hand that was closest to this object.  </summary>
    public abstract void PrimaryHoverEnd(Hand hand);

    #endregion


    #region Contact

    public abstract void ContactBegin(Hand hand);

    public abstract void ContactStay(Hand hand);

    public abstract void ContactEnd(Hand hand);

    #endregion


    #region Grasping

    public abstract bool isGrasped { get; }

    public abstract void GraspBegin(Hand hand);

    public abstract void GraspHold(Hand hand);

    public abstract void GraspEnd(InteractionHand intHand);

    protected bool _isSuspended = false;
    /// <summary> Gets whether the hand grasping this object is currently untracked. </summary>
    public bool isSuspended { get { return _isSuspended; } }

    /// <summary> Called when the hand grasping an object stops tracking. </summary>
    public abstract void GraspSuspendObject();

    /// <summary> Called when the hand grasping an object resumes tracking. </summary>
    public abstract void GraspResumeObject();

    #endregion

  }

}