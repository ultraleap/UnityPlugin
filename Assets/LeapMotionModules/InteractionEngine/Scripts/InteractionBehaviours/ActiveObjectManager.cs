using UnityEngine;
using System;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  public class ActiveObjectManager {
    private Dictionary<Rigidbody, ActiveObject> _activeObjects = new Dictionary<Rigidbody, ActiveObject>();

    private int _updateIndex = 0;

    private float _overlapRadius = 0;
    private int _layerMask;

    private List<Rigidbody> _rigidbodyList = new List<Rigidbody>();
    private Collider[] _colliderResults = new Collider[32];

    private HashSet<IInteractionBehaviour> _registeredBehaviours = new HashSet<IInteractionBehaviour>();
    private List<IInteractionBehaviour> _activeBehaviours = new List<IInteractionBehaviour>();
    private List<IInteractionBehaviour> _misbehavingBehaviours = new List<IInteractionBehaviour>();

    private List<IInteractionBehaviour> _activatedBehaviours = new List<IInteractionBehaviour>();
    private List<IInteractionBehaviour> _deactivatedBehaviours = new List<IInteractionBehaviour>();

    public float OverlapRadius {
      get {
        return _overlapRadius;
      }
      set {
        _overlapRadius = value;
      }
    }

    public int LayerMask {
      get {
        return _layerMask;
      }
      set {
        _layerMask = value;
      }
    }

    public IEnumerable<IInteractionBehaviour> RegisteredObjects {
      get {
        return _registeredBehaviours;
      }
    }

    public ReadonlyList<IInteractionBehaviour> ActiveBehaviours {
      get {
        return _activeBehaviours;
      }
    }

    public void Register(IInteractionBehaviour behaviour) {
      behaviour.NotifyRegistered();
      _registeredBehaviours.Add(behaviour);
    }

    public void Unregister(IInteractionBehaviour behaviour) {
      if (_registeredBehaviours.Remove(behaviour)) {
        Deactivate(behaviour);
      }
    }

    public void NotifyMisbehaving(IInteractionBehaviour behaviour) {
      _misbehavingBehaviours.Add(behaviour);
    }

    public void Deactivate(IInteractionBehaviour interactionBehaviour) {
      Rigidbody rigidbody = interactionBehaviour.GetComponent<Rigidbody>();
      if (_activeObjects.ContainsKey(rigidbody)) {
        _activeObjects.Remove(rigidbody);
        _activeBehaviours.Remove(interactionBehaviour);
        _deactivatedBehaviours.Add(interactionBehaviour);
      }
    }

    public void DeactivateAll(out ReadonlyList<IInteractionBehaviour> deactivated) {
      _deactivatedBehaviours.AddRange(_activeBehaviours);
      deactivated = new ReadonlyList<IInteractionBehaviour>(_deactivatedBehaviours);

      _activeBehaviours.Clear();
      _activatedBehaviours.Clear();
      _activeObjects.Clear();
    }

    public bool IsActive(IInteractionBehaviour interactionBehaviour) {
      return _activeBehaviours.Contains(interactionBehaviour);
    }

    public bool IsRegistered(IInteractionBehaviour interactionBehaviour) {
      return _registeredBehaviours.Contains(interactionBehaviour);
    }

    public void Update(Frame frame, out ReadonlyList<IInteractionBehaviour> activated, out ReadonlyList<IInteractionBehaviour> deactivated) {
      _updateIndex++;

      List<Hand> hands = frame.Hands;

      markOverlappingObjects(hands);

      activateMarkedObjects();

      deactivateStaleObjects();

      buildResultsLists();

      activated = new ReadonlyList<IInteractionBehaviour>(_activatedBehaviours);
      deactivated = new ReadonlyList<IInteractionBehaviour>(_deactivatedBehaviours);
    }

    public void UnregisterMisbehavingObjects() {
      for (int i = 0; i < _misbehavingBehaviours.Count; i++) {
        var behaviour = _misbehavingBehaviours[i];
        if (behaviour != null) {
          try {
            Unregister(behaviour);
          } catch (Exception e) {
            Debug.LogException(e);
          }
        }
      }
      _misbehavingBehaviours.Clear();
    }

    private void markOverlappingObjects(List<Hand> hands) {
      _rigidbodyList.Clear();

      switch (hands.Count) {
        case 0:
          break;
        case 1:
          getSphereResults(hands[0], _rigidbodyList);
          break;
#if UNITY_5_4
        case 2:
          getCapsuleResults(hands[0], hands[1], _rigidbodyList);
          break;
#endif
        default:
          for (int i = 0; i < hands.Count; i++) {
            getSphereResults(hands[i], _rigidbodyList);
          }
          break;
      }
    }

    private void activateMarkedObjects() {
      _activatedBehaviours.Clear();
      for (int i = 0; i < _rigidbodyList.Count; i++) {
        Rigidbody body = _rigidbodyList[i];
        ActiveObject activeObj;
        if (!_activeObjects.TryGetValue(body, out activeObj)) {
          var behaviour = body.GetComponent<IInteractionBehaviour>();

          if (behaviour == null) {
            //Someone is using our layers for their own evil needs...
            continue;
          }

          if (!_registeredBehaviours.Contains(behaviour)) {
            continue;
          }

          activeObj = body.gameObject.AddComponent<ActiveObject>();
          activeObj.interactionBehaviour = behaviour;

          _activatedBehaviours.Add(activeObj.interactionBehaviour);

          _activeObjects[body] = activeObj;
        }

        activeObj.updateIndex = _updateIndex;
      }
    }

    private void deactivateStaleObjects() {
      //Find all active objects that have not had their index updated
      _rigidbodyList.Clear();
      _deactivatedBehaviours.Clear();
      foreach (var pair in _activeObjects) {
        if (pair.Value.updateIndex < _updateIndex) {
          _deactivatedBehaviours.Add(pair.Value.interactionBehaviour);

          //Destroy the component right away
          UnityEngine.Object.DestroyImmediate(pair.Value);

          //Add the key to the list for later removal
          _rigidbodyList.Add(pair.Key);
        }
      }

      //Remove all keys that were marked
      for (int i = 0; i < _rigidbodyList.Count; i++) {
        _activeObjects.Remove(_rigidbodyList[i]);
      }
    }

    private void buildResultsLists() {
      _activeBehaviours.Clear();
      foreach (var activeObj in _activeObjects.Values) {
        _activeBehaviours.Add(activeObj.interactionBehaviour);
      }
    }

    private void handleColliderResults(int count, List<Rigidbody> list) {
      for (int i = 0; i < count; i++) {
        Collider collider = _colliderResults[i];
        //This will totally add duplicates
        list.Add(collider.attachedRigidbody);
      }
    }

    private void getSphereResults(Hand hand, List<Rigidbody> list) {
      int count;
      while (true) {
        count = Physics.OverlapSphereNonAlloc(hand.PalmPosition.ToVector3(),
                                              _overlapRadius,
                                              _colliderResults,
                                              _layerMask,
                                              QueryTriggerInteraction.Ignore);
        if (count < _colliderResults.Length) {
          break;
        }
        _colliderResults = new Collider[_colliderResults.Length * 2];
      }

      handleColliderResults(count, list);
    }

#if UNITY_5_4
    private void getCapsuleResults(Hand handA, Hand handB, List<Rigidbody> list) {
      int count;
      while (true) {
        count = Physics.OverlapCapsuleNonAlloc(handA.PalmPosition.ToVector3(),
                                               handB.PalmPosition.ToVector3(),
                                               _primaryRadius,
                                               _colliderResults,
                                               _layerMask,
                                               QueryTriggerInteraction.Ignore);
        if (count < _colliderResults.Length) {
          break;
        }
        _colliderResults = new Collider[_colliderResults.Length * 2];
      }

      handleColliderResults(count, list);
    }
#endif
  }
}
