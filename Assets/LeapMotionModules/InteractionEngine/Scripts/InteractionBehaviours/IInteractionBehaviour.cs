using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// IInteractionBehaviour is the interface used by InteractionManager to drive interaction objects.
  /// All interaction objects must extend from this base interface class.  This class is an abstract
  /// extended MonoBehaviour instead of an interface.
  /// </summary>
  public abstract class IInteractionBehaviour : MonoBehaviour {
    /// <summary>
    /// Gets or sets the manager this object belongs to.
    /// </summary>
    public abstract InteractionManager Manager { get; set; }

    /// <summary>
    /// Gets whether or not the behaviour is currently registered with the manager.
    /// </summary>
    public abstract bool IsRegisteredWithManager { get; }

    /// <summary>
    /// Gets the handle to the internal shape description.
    /// </summary>
    public abstract INTERACTION_SHAPE_DESCRIPTION_HANDLE ShapeDescriptionHandle { get; }

    /// <summary>
    /// Gets the handle to the internal shape instance.
    /// </summary>
    public abstract INTERACTION_SHAPE_INSTANCE_HANDLE ShapeInstanceHandle { get; }

    /// <summary>
    /// Returns true if there is at least one hand grasping this object.
    /// </summary>
    public abstract bool IsBeingGrasped { get; }

    /// <summary>
    /// Returns the number of hands that are currently grasping this object.
    /// </summary>
    public abstract int GraspingHandCount { get; }

    /// <summary>
    /// Returns the number of hands that are currently grasping this object but
    /// are not currently being tracked.
    /// </summary>
    public abstract int UntrackedHandCount { get; }

    /// <summary>
    /// Returns the ids of the hands currently grasping this object.
    /// </summary>
    public abstract IEnumerable<int> GraspingHands { get; }

    /// <summary>
    /// Returns the ids of the hands currently grasping this object, but only
    /// returns ids of hands that are also currently being tracked.
    /// </summary>
    public abstract IEnumerable<int> TrackedGraspingHands { get; }

    /// <summary>
    /// Returns the ids of the hands that are considered grasping but are untracked.
    /// </summary>
    public abstract IEnumerable<int> UntrackedGraspingHands { get; }

    /// <summary>
    /// Returns whether or not a hand with the given ID is currently grasping this object.
    /// </summary>
    public abstract bool IsBeingGraspedByHand(int handId);

    /// <summary>
    /// Returns whether or not this object can be deactivated.
    /// </summary>
    public abstract bool IsAbleToBeDeactivated();

    /// <summary>
    /// Called by InteractionManager when the behaviour is successfully registered with the manager.
    /// </summary>
    public abstract void NotifyRegistered();

    /// <summary>
    /// Called by InteractionManager when the behaviour is unregistered with the manager.
    /// </summary>
    public abstract void NotifyUnregistered();

    /// <summary>
    /// Called by InteractionManager before any solving is performed.
    /// </summary>
    public abstract void NotifyPreSolve();

    /// <summary>
    /// Called by InteractionManager after all solving is performed.
    /// </summary>
    public abstract void NotifyPostSolve();

    /// <summary>
    /// Called by InteractionManager to get the creation info used to create the shape associated with this InteractionBehaviour.
    /// </summary>
    /// <param name="createInfo"></param>
    /// <param name="createTransform"></param>
    public abstract void GetInteractionShapeCreationInfo(out INTERACTION_CREATE_SHAPE_INFO createInfo, out INTERACTION_TRANSFORM createTransform);

    /// <summary>
    /// Called by InteractionManager when the interaction shape associated with this InteractionBehaviour
    /// is created and added to the interaction scene.
    /// </summary>
    public abstract void NotifyInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle);

    /// <summary>
    /// Called by InteractionManager to get the update info needed to update the internal state of the object.
    /// </summary>
    public abstract void GetInteractionShapeUpdateInfo(out INTERACTION_UPDATE_SHAPE_INFO updateInfo, out INTERACTION_TRANSFORM interactionTransform);

    /// <summary>
    /// Called by InteractionManager when the interaction shape associated with this InteractionBehaviour
    /// is destroyed and removed from the interaction scene.
    /// </summary>
    public abstract void NotifyInteractionShapeDestroyed();

    /// <summary>
    /// Called by InteractionManager when simulation results are available for this object.
    /// </summary>
    public abstract void NotifyRecievedSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results);

    /// <summary>
    /// Called by InteractionManager when a Hand begins grasping this object.
    /// </summary>
    public abstract void NotifyHandGrasped(Hand hand);

    /// <summary>
    /// Called by InteractionManager every frame that a Hand continues to grasp this object.  This callback
    /// is invoked both in FixedUpdate.
    /// </summary>
    public abstract void NotifyHandsHoldPhysics(ReadonlyList<Hand> hands);

    /// <summary>
    /// Called by InteractionManager every frame that a Hand continues to grasp this object.  This callback
    /// is invoked both in LateUpdate.
    /// </summary>
    public abstract void NotifyHandsHoldGraphics(ReadonlyList<Hand> hands);

    /// <summary>
    /// Called by InteractionManager when a Hand stops grasping this object.
    /// </summary>
    public abstract void NotifyHandReleased(Hand hand);

    /// <summary>
    /// Called by InteractionManager when a Hand that was grasping becomes untracked.  The Hand
    /// is not yet considered ungrasped, and OnHandRegainedTracking might be called in the future
    /// if the Hand becomes tracked again.
    /// </summary>
    public abstract void NotifyHandLostTracking(Hand oldHand, out float maxSuspensionTime);

    /// <summary>
    /// Called by InteractionManager when a grasping Hand that had previously been untracked has
    /// regained tracking.  The new hand is provided, as well as the id of the previously tracked
    /// hand.
    /// </summary>
    public abstract void NotifyHandRegainedTracking(Hand newHand, int oldId);

    /// <summary>
    /// Called by InteractionManager when an untracked grasping Hand has remained ungrasped for
    /// too long.  The hand is no longer considered to be grasping the object.
    /// </summary>
    public abstract void NotifyHandTimeout(Hand oldHand);

    /// <summary>
    /// Called when a dislocated brush begins overlapping the InteractionBehaviour.
    /// </summary>
    public abstract void NotifyBrushDislocated();

    /// <summary>
    /// Called to validate the state of the object.
    /// </summary>
    [Conditional("UNITY_ASSERTIONS")]
    public abstract void Validate();
  }
}
