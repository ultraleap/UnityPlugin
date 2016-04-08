using UnityEngine;
using System.Collections.Generic;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {
  
  public interface IInteractionBehaviour {
    /// <summary>
    /// Gets or sets the manager this object belongs to.
    /// </summary>
    InteractionManager Manager { get; set; }

    /// <summary>
    /// Gets or Sets whether or not interaction is enabled for this object.  Setting is
    /// equivilent to calling EnableInteraction() or DisableInteraction()
    /// </summary>
    bool IsInteractionEnabled { get; set; }

    /// <summary>
    /// Gets the handle to the internal shape description.
    /// </summary>
    INTERACTION_SHAPE_DESCRIPTION_HANDLE ShapeDescriptionHandle { get; }

    /// <summary>
    /// Gets the handle to the internal shape instance.
    /// </summary>
    INTERACTION_SHAPE_INSTANCE_HANDLE ShapeInstanceHandle { get; }

    /// <summary>
    /// Returns true if there is at least one hand grasping this object.
    /// </summary>
    bool IsBeingGrasped { get; }

    /// <summary>
    /// Returns the number of hands that are currently grasping this object.
    /// </summary>
    int GraspingHandCount { get; }

    /// <summary>
    /// Returns the number of hands that are currently grasping this object but
    /// are not currently being tracked.
    /// </summary>
    int UntrackedHandCount { get; }

    /// <summary>
    /// Returns the ids of the hands currently grasping this object.
    /// </summary>
    IEnumerable<int> GraspingHands { get; }

    /// <summary>
    /// Returns the ids of the hands currently grasping this object, but only
    /// returns ids of hands that are also currently being tracked.
    /// </summary>
    IEnumerable<int> TrackedGraspingHands { get; }

    /// <summary>
    /// Returns the ids of the hands that are considered grasping but are untracked.
    /// </summary>
    IEnumerable<int> UntrackedGraspingHands { get; }

    /// <summary>
    /// Calling this method registers this object with the manager.  A shape definition must be registered
    /// with the manager before interaction can be enabled.
    /// </summary>
    void EnableInteraction();

    /// <summary>
    /// Calling this method will unregister this object from the manager.
    /// </summary>
    void DisableInteraction();

    /// <summary>
    /// Returns whether or not a hand with the given ID is currently grasping this object.
    /// </summary>
    bool IsBeingGraspedByHand(int handId);



    /// <summary>
    /// Called by InteractionManager when the behaviour is successfully registered with the manager.
    /// </summary>
    void OnRegister();

    /// <summary>
    /// Called by InteractionManager when the behaviour is unregistered with the manager.
    /// </summary>
    void OnUnregister();

    /// <summary>
    /// Called by InteractionManager before any solving is performed.
    /// </summary>
    void OnPreSolve();

    /// <summary>
    /// Called by InteractionManager after all solving is performed.
    /// </summary>
    void OnPostSolve();

    /// <summary>
    /// Called by InteractionManager to get the creation info used to create the shape associated with this InteractionBehaviour.
    /// </summary>
    /// <param name="createInfo"></param>
    /// <param name="createTransform"></param>
    void OnInteractionShapeCreationInfo(out INTERACTION_CREATE_SHAPE_INFO createInfo, out INTERACTION_TRANSFORM createTransform);

    /// <summary>
    /// Called by InteractionManager when the interaction shape associated with this InteractionBehaviour
    /// is created and added to the interaction scene.
    /// </summary>
    void OnInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle);

    /// <summary>
    /// Called by InteractionManager when the interaction shape associated with this InteractionBehaviour
    /// is about to be updated.
    /// </summary>
    void OnInteractionShapeUpdate(out INTERACTION_UPDATE_SHAPE_INFO updateInfo, out INTERACTION_TRANSFORM interactionTransform);

    /// <summary>
    /// Called by InteractionManager when the interaction shape associated with this InteractionBehaviour
    /// is destroyed and removed from the interaction scene.
    /// </summary>
    void OnInteractionShapeDestroyed();

    /// <summary>
    /// Called by InteractionManager when the velocity of an object is changed.
    /// </summary>
    void OnRecieveSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results);

    /// <summary>
    /// Called by InteractionManager when a Hand begins grasping this object.
    /// </summary>
    void OnHandGrasp(Hand hand);

    /// <summary>
    /// Called by InteractionManager every frame that a Hand continues to grasp this object.  This callback
    /// is invoked both in FixedUpdate, and also in LateUpdate.  This gives all objects a chance to both update
    /// their physical representation as well as their graphical, for decreased latency and increase fidelity.
    /// </summary>
    void OnHandsHold(List<Hand> hands);

    /// <summary>
    /// Called by InteractionManager when a Hand stops grasping this object.
    /// </summary>
    void OnHandRelease(Hand hand);

    /// <summary>
    /// Called by InteractionManager when a Hand that was grasping becomes untracked.  The Hand
    /// is not yet considered ungrasped, and OnHandRegainedTracking might be called in the future
    /// if the Hand becomes tracked again.
    /// </summary>
    void OnHandLostTracking(Hand oldHand);

    /// <summary>
    /// Called by InteractionManager when a grasping Hand that had previously been untracked has
    /// regained tracking.  The new hand is provided, as well as the id of the previously tracked
    /// hand.
    /// </summary>
    void OnHandRegainedTracking(Hand newHand, int oldId);

    /// <summary>
    /// Called by InteractionManager when an untracked grasping Hand has remained ungrasped for
    /// too long.  The hand is no longer considered to be grasping the object.
    /// </summary>
    void OnHandTimeout(Hand oldHand);
    
  }
}
