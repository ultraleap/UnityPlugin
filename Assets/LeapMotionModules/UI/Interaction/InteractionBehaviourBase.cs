using Leap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public enum HoverType {
    Proximity
  }

  public enum TouchType {
    SoftContact,
    CallbacksOnly
  }

  public enum GrabType {
    GrabOrPinch,
    GrabOnly,
    PinchOnly
  }

  public abstract class InteractionBehaviourBase : MonoBehaviour {

    public InteractionManager manager;

    [Header("Interaction Types")]
    public bool enableHover = true;
    public HoverType hoverType;
    public bool enableTouch = true;
    public TouchType touchType;
    public bool enableGrab = true;
    public GrabType grabType;
    public bool allowsTwoHandedGrab = false;

    #region Hover

    /// <summary> Values >= zero are "hovered." Of hovered objects, the one with the highest score is the "primary" hovered object. </summary>
    public abstract float GetHoverScore(Hand hand);


    /// <summary> Called per-hand when that hand produces a non-zero hover score for this object. </summary>
    public abstract void OnHoverBegin(Hand hand);

    /// <summary> Called per-hand when that hand maintains a non-zero hover score for each frame beyond the first. </summary>
    public abstract void OnHoverStay(Hand hand);

    /// <summary> Called per-hand when that hand's hover score has transitioned from above zero to at-or-below zero. </summary>
    public abstract void OnHoverEnd(Hand hand);


    /// <summary> Called per-hand when this object returns the highest hover score for a given hand. </summary>
    public abstract void OnPrimaryHoverBegin(Hand hand);

    /// <summary> Called per-hand when this object has the highest hover score for a given hand each frame beyond the first. </summary>
    public abstract void OnPrimaryHoverStay(Hand hand);

    /// <summary> Called per-hand when this object no longer has the highest hover score for that hand. </summary>
    public abstract void OnPrimaryHoverEnd(Hand hand);

    #endregion


    #region Touch

    public abstract void OnTouchBegin(Hand hand);

    public abstract void OnTouchStay(Hand hand);

    public abstract void OnTouchEnd(Hand hand);

    #endregion


    #region Grab

    /// <summary> Returns whether the argument hand is currently grabbing this object.
    /// No grab classification is performed here; this method simply asks the Interaction Manager about grab states. </summary>
    public abstract bool IsBeingGrabbedBy(Hand hand);

    public abstract void OnGraspBegin(Hand hand);

    public abstract void OnGraspHold(Hand hand);

    public abstract void OnGraspRelease(Hand hand);

    public abstract void OnSuspend(Hand hand);

    public abstract void OnResume(Hand hand);

    #endregion

  }

}