using UnityEngine;
using UnityEngine.Events;
using System;

namespace Leap.Unity.Interaction {

  /**
  * Add an InteractionHandEvents component to an interactable object to expose
  * standard Unity event dispatchers for the onHandGrasp and onHandRelease events.
  * The events become accessible in the Unity inspector panel where you can hook
  * up the events to call the functions of other scripts.
  *
  * OnHandGrasp is dispatched when a hand grasps the interactable object.
  *
  * OnHandRelease is dispatched when a hand releases the object.
  *
  * Both events include the Leap.Hand object involved in the event.
  *
  * Contrast these events with those defined by the InteractionGraspEvents component, 
  * which are dispatched whenever the object changes from a grasped state to an 
  * ungrasped state or vice versa, taking multiple simultaneous grasps into account.
  * @since 4.1.4
  */
  [RequireComponent(typeof(InteractionBehaviourBase))]
  public class InteractionHandEvents : MonoBehaviour {

    /** Extends UnityEvent to provide hand related events containing a Leap.Hand parameter. */
    [Serializable]
    public class HandEvent : UnityEvent<Hand> { }

    /**
    * Dispatched when a hand grasps the interactable object.
    * @since 4.1.4
    */
    public HandEvent onHandGrasp;
    /**
    * Dispatched when a hand releases the object.
    * @since 4.1.4
    */
    public HandEvent onHandRelease;

    private InteractionBehaviourBase _interactionBehaviour;

    void Awake() {
      _interactionBehaviour = GetComponent<InteractionBehaviourBase>();

      if (_interactionBehaviour != null) {
        _interactionBehaviour.OnHandGraspedEvent += onHandGrasp.Invoke;
        _interactionBehaviour.OnHandReleasedEvent += onHandRelease.Invoke;
      }
    }

    void OnDestroy() {
      if (_interactionBehaviour != null) {
        _interactionBehaviour.OnHandGraspedEvent -= onHandGrasp.Invoke;
        _interactionBehaviour.OnHandReleasedEvent -= onHandRelease.Invoke;
      }
    }
  }
}
