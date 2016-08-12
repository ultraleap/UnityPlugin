using UnityEngine;
using UnityEngine.Events;
using System;

namespace Leap.Unity.Interaction {

  [RequireComponent(typeof(InteractionBehaviourBase))]
  public class InteractionGraspEvents : MonoBehaviour {

    public UnityEvent onGraspBegin;
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
