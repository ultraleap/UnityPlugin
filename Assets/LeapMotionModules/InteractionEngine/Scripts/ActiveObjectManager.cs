using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  public class ActiveObjectManager {
    private Dictionary<Rigidbody, ActiveObject> _activeObjects = new Dictionary<Rigidbody, ActiveObject>();

    private int _updateIndex = 0;

    private float _overlapRadius = 0;
    private int _layerMask;

    private List<Rigidbody> _rigidbodyList = new List<Rigidbody>();
    private Collider[] _colliderResults = new Collider[32];
    private List<IInteractionBehaviour> _activeBehaviours = new List<IInteractionBehaviour>();

    public ActiveObjectManager(float overlapRadius, int layerMask) {
      _overlapRadius = overlapRadius;
      _layerMask = layerMask;
    }

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

    public ReadonlyList<IInteractionBehaviour> ActiveBehaviours {
      get {
        return _activeBehaviours;
      }
    }

    public void FindActiveObjects(Frame frame) {
      List<Hand> hands = frame.Hands;

      markOverlappingObjects(hands);

      activateMarkedObjects();

      deactivateStaleObjects();

      buildResultsLists();
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
      for (int i = 0; i < _rigidbodyList.Count; i++) {
        Rigidbody body = _rigidbodyList[i];
        ActiveObject activeObj;
        if (!_activeObjects.TryGetValue(body, out activeObj)) {
          activeObj = body.gameObject.AddComponent<ActiveObject>();

          //TODO: validate that a behaviour actually exists (it should unless someone is being mean)
          activeObj.interactionBehaviour = body.GetComponent<IInteractionBehaviour>();

          _activeObjects[body] = activeObj;
        }

        activeObj.updateIndex = _updateIndex;
      }
    }

    private void deactivateStaleObjects() {
      //Find all active objects that have not had their index updated
      _rigidbodyList.Clear();
      foreach (var pair in _activeObjects) {
        if (pair.Value.updateIndex < _updateIndex) {
          //Destroy the component right away
          Object.DestroyImmediate(pair.Value);
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
      for (int i = 0; i < _colliderResults.Length; i++) {
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
