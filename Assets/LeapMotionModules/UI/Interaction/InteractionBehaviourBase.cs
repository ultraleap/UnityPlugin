using Leap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public abstract class InteractionBehaviourBase : MonoBehaviour {

    public InteractionManager manager;

    [Header("Interaction Types")]
    public bool enableHovering = true;
    public bool enableContact = true;
    public bool enableGrasping = true;
    public bool allowsTwoHandedGrab = false;

    #region hovering

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


    #region Contact

    public abstract void OnContactBegin(Hand hand);

    public abstract void OnContactStay(Hand hand);

    public abstract void OnContactEnd(Hand hand);

    #endregion


    #region Grasping

    public abstract void OnGraspBegin(Hand hand);

    public abstract void OnGraspHold(Hand hand);

    public abstract void OnGraspRelease(Hand hand);

    public abstract void OnSuspend(Hand hand);

    public abstract void OnResume(Hand hand);

    #endregion

  }

}