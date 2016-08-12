using UnityEngine;
using UnityEngine.Events;
using System;

namespace Leap.Unity.Interaction {

  [RequireComponent(typeof(InteractionBehaviourBase))]
  public class InteractionHandEvents : MonoBehaviour {

    [Serializable]
    public class HandEvent : UnityEvent<Hand> { }

    public HandEvent onHandGrasp;
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
