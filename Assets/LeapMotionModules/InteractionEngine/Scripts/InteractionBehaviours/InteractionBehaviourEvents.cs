using UnityEngine;
using UnityEngine.Events;
using System;

namespace Leap.Unity.Interaction {

  [RequireComponent(typeof(InteractionBehaviourBase))]
  public class InteractionBehaviourEvents : MonoBehaviour {

    [Serializable]
    public class HandEvent : UnityEvent<Hand> { }

    public UnityEvent onGraspBegin;
    public UnityEvent onGraspEnd;
    public HandEvent onHandGrasp;
    public HandEvent onHandRelease;

    private InteractionBehaviourBase _interactionBehaviour;

    void Awake() {
      _interactionBehaviour = GetComponent<InteractionBehaviourBase>();

      if (_interactionBehaviour != null) {
        _interactionBehaviour.OnGraspBeginEvent += onGraspBegin.Invoke;
        _interactionBehaviour.OnGraspEndEvent += onGraspEnd.Invoke;
        _interactionBehaviour.OnHandGraspedEvent += onHandGrasp.Invoke;
        _interactionBehaviour.OnHandReleasedEvent += onHandRelease.Invoke;
      }
    }

    void OnDestroy() {
      if (_interactionBehaviour != null) {
        _interactionBehaviour.OnGraspBeginEvent -= onGraspBegin.Invoke;
        _interactionBehaviour.OnGraspEndEvent -= onGraspEnd.Invoke;
        _interactionBehaviour.OnHandGraspedEvent -= onHandGrasp.Invoke;
        _interactionBehaviour.OnHandReleasedEvent -= onHandRelease.Invoke;
      }
    }
  }
}
