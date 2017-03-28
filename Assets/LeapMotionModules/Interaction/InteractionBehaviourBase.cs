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
    public new Rigidbody rigidbody { get { return _body; } }

    [Header("Interaction Settings")]
    public bool allowHover = true;
    public bool allowGrasping = true;
    /// <summary> Can this object be grasped with two or more hands? </summary>
    public bool allowMultiGrasp = false;

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

      if (interactionManager != null) {
        interactionManager.RegisterInteractionBehaviour(this);
      }
    }

    protected virtual void OnEnable() {
      if (interactionManager == null) {
        interactionManager = InteractionManager.singleton;

        if (interactionManager == null) {
          Debug.LogError("Interaction Behaviours require an Interaction Manager. Please ensure you have an InteractionManager in your scene.");
          this.enabled = false;
        }
        else {
          interactionManager.RegisterInteractionBehaviour(this);
        }
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

    /// <summary> Called per-hand when that hand is no longer near this object.
    /// The hand object may be null. This will occur if the hand stopped hovering due to a loss of tracking. </summary>
    public abstract void HoverEnd(Hand hand);

    /// <summary> As HoverBegin, but only for the hand that is closest to this object.
    /// If a new hand becomes the closest hand, the old hand will get PrimaryHoverEnd before the new hand
    /// gets PrimaryHoverBegin. </summary>
    public abstract void PrimaryHoverBegin(Hand hand);

    /// <summary> As HoverStay, but only for the hand that is closest to this object. </summary>
    public abstract void PrimaryHoverStay(Hand hand);

    /// <summary> As HoverEnd, but only for the hand that was closest to this object.
    /// The hand object may be null. This will occur if the hand stopped hovering due to a loss of tracking. </summary>
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

    public abstract void GraspEnd(Hand hand);

    /// <summary> Called when the hand grasping an object stops tracking and is going to disappear. </summary>
    public abstract void GraspSuspendObject(Hand hand);

    /// <summary> Called when the hand grasping an object resumes tracking and is going to re-appear. </summary>
    public abstract void GraspResumeObject(Hand hand);

    #endregion

  }

}