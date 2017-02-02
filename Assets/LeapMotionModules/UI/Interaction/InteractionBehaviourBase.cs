using Leap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public abstract class InteractionBehaviourBase : MonoBehaviour {

    public InteractionManager interactionManager;

    [Header("Interaction Settings -- currently ignored!")]
    public bool allowsTwoHandedGrasp = false;

    public abstract void FixedUpdateObject();

    #region Hovering

    // TODO: Hover scores may not be the right idea here.
    /// <summary> Values >= zero are "hovered." Of hovered objects, the one with the highest score is the "primary" hovered object. </summary>
    public abstract float GetHoverScore(Hand hand);


    /// <summary> Called per-hand when that hand produces a non-zero hover score for this object. </summary>
    public abstract void HoverBegin(Hand hand);

    /// <summary> Called per-hand when that hand maintains a non-zero hover score for each frame beyond the first. </summary>
    public abstract void HoverStay(Hand hand);

    /// <summary> Called per-hand when that hand's hover score has transitioned from above zero to at-or-below zero.
    /// The hand object may be null. This will occur if the hand stopped hovering due to a loss of tracking. </summary>
    public abstract void HoverEnd(Hand hand);

    // TODO: Primary hover does not incorporate grasp potentiality, and it really needs to.

    /// <summary> Called per-hand when this object returns the highest hover score for a given hand. </summary>
    public abstract void PrimaryHoverBegin(Hand hand);

    /// <summary> Called per-hand when this object has the highest hover score for a given hand each frame beyond the first. </summary>
    public abstract void PrimaryHoverStay(Hand hand);

    /// <summary> Called per-hand when this object no longer has the highest hover score for that hand.
    /// The hand object may be null. This will occur if the hand stopped hovering due to a loss of tracking. </summary>
    public abstract void PrimaryHoverEnd(Hand hand);

    #endregion


    #region Contact

    public abstract void ContactBegin(Hand hand);

    public abstract void ContactStay(Hand hand);

    public abstract void ContactEnd(Hand hand);

    #endregion


    #region Grasping

    public abstract bool IsGrasped { get; }

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