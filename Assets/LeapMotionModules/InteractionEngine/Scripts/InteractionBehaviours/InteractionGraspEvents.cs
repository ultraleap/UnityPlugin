using UnityEngine;
using UnityEngine.Events;
using System;

namespace Leap.Unity.Interaction {

  /**
  * Add an InteractionGraspEvents component to an interactable object to expose
  * standard Unity event dispatchers for the OnGraspBegin and OnGraspEnd events.
  * The events become accessible in the Unity inspector panel where you can hook
  * up the events to call the functions of other scripts.
  *
  * OnGraspBegin is dispatched when an interactable object is first grasped by 
  * any hands (but not when the object is already in the grasp of another hand).
  *
  * OnGraspEnd is dispatched when the last hand releases the object.
  *
  * Contrast these events with those defined by the InteractionHandEvents component, 
  * which are dispatched whenever an individual hand grasps or releases the object and
  * which also provide the Leap.Hand object involved in the event.
  * @since 4.1.4
  */
  [RequireComponent(typeof(InteractionBehaviourBase))]
  public class InteractionGraspEvents : MonoBehaviour {

    /**
    * Dispatched when an interactable object is first grasped by 
    * any hands (but not when the object is already in the grasp of another hand).
    * @since 4.1.4
    */
    public UnityEvent onGraspBegin;
    /**
    * Dispatched when the last hand releases the object.
    * @since 4.1.4
    */
    public UnityEvent onGraspEnd;

    private InteractionBehaviourBase _interactionBehaviour;

    void Awake() {
      _interactionBehaviour = GetComponent<InteractionBehaviourBase>();

      if (_interactionBehaviour != null) {
        _interactionBehaviour.OnGraspBeginEvent += onGraspBegin.Invoke;
        _interactionBehaviour.OnGraspEndEvent += onGraspEnd.Invoke;
      }
    }

    void OnDestroy() {
      if (_interactionBehaviour != null) {
        _interactionBehaviour.OnGraspBeginEvent -= onGraspBegin.Invoke;
        _interactionBehaviour.OnGraspEndEvent -= onGraspEnd.Invoke;
      }
    }
  }
}
