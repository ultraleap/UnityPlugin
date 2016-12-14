using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  public partial class ActivityManager {
    private float _overlapRadius = 0;
    private int _maxDepth = 0;
    private int _brushLayer = 0;
    private int _brushLayerMask = 0;

    private List<IInteractionBehaviour> _markedBehaviours = new List<IInteractionBehaviour>();
    private Collider[] _colliderResults = new Collider[32];

    //Maps registered objects to their active component, which is always null for inactive but still registered objects
    private Dictionary<IInteractionBehaviour, IActivityMonitor> _registeredBehaviours = new Dictionary<IInteractionBehaviour, IActivityMonitor>();
    //Technically provided by _registerBehaviours, but we want fast iteration over active objects, so pay a little more for a list
    private List<IInteractionBehaviour> _activeBehaviours = new List<IInteractionBehaviour>();

    private List<IInteractionBehaviour> _misbehavingBehaviours = new List<IInteractionBehaviour>();

    public event Action<IInteractionBehaviour> OnActivate;
    public event Action<IInteractionBehaviour> OnDeactivate;

    public float OverlapRadius {
      get {
        return _overlapRadius;
      }
      set {
        _overlapRadius = value;
      }
    }

    public int MaxDepth {
      get {
        return _maxDepth;
      }
      set {
        _maxDepth = value;
      }
    }

    public int BrushLayer {
      get {
        return _brushLayer;
      }
      set {
        _brushLayer = value;
      }
    }

    public IEnumerable<IInteractionBehaviour> RegisteredObjects {
      get {
        return _registeredBehaviours.Keys;
      }
    }

    public ReadonlyList<IInteractionBehaviour> ActiveBehaviours {
      get {
        return _activeBehaviours;
      }
    }

    public void Register(IInteractionBehaviour behaviour) {
      //Do nothing if already registered
      if (_registeredBehaviours.ContainsKey(behaviour)) {
        return;
      }

      _registeredBehaviours.Add(behaviour, null);

      behaviour.NotifyRegistered();
    }

    public void Unregister(IInteractionBehaviour behaviour) {
      //Do nothing if not registered
      if (!_registeredBehaviours.ContainsKey(behaviour)) {
        return;
      }

      if (IsActive(behaviour)) {
        Deactivate(behaviour);
      }

      _registeredBehaviours.Remove(behaviour);

      behaviour.NotifyUnregistered();
    }

    public void NotifyMisbehaving(IInteractionBehaviour behaviour) {
      _misbehavingBehaviours.Add(behaviour);
    }

    public IActivityMonitor Activate(IInteractionBehaviour interactionBehaviour) {
      IActivityMonitor monitor;
      if (_registeredBehaviours.TryGetValue(interactionBehaviour, out monitor)) {
        if (monitor == null) {
          if (_maxDepth == 1) {
            monitor = interactionBehaviour.gameObject.AddComponent<ActivityMonitorLite>();
          } else {
            monitor = interactionBehaviour.gameObject.AddComponent<ActivityMonitor>();
          }

          monitor.Init(interactionBehaviour, this);

          _registeredBehaviours[interactionBehaviour] = monitor;

          monitor.arrayIndex = _activeBehaviours.Count;
          _activeBehaviours.Add(interactionBehaviour);

          if (OnActivate != null) {
            OnActivate(interactionBehaviour);
          }
        }
      }
      return monitor;
    }

    public void Deactivate(IInteractionBehaviour interactionBehaviour) {
      IActivityMonitor monitor;
      if (_registeredBehaviours.TryGetValue(interactionBehaviour, out monitor)) {
        if (monitor != null) {
          //The monitor that is last in the array of monitors
          IInteractionBehaviour lastBehaviour = _activeBehaviours[_activeBehaviours.Count - 1];
          IActivityMonitor lastMonitor = _registeredBehaviours[lastBehaviour];

          //Replace the monitor we are going to destroy with the last monitor
          _activeBehaviours[monitor.arrayIndex] = lastBehaviour;
          //Make sure to update the index of the moved monitor!
          lastMonitor.arrayIndex = monitor.arrayIndex;
          //Remove the empty space at the end
          _activeBehaviours.RemoveAt(_activeBehaviours.Count - 1);

          _registeredBehaviours[interactionBehaviour] = null;
          UnityEngine.Object.Destroy(monitor);

          if (OnDeactivate != null) {
            OnDeactivate(interactionBehaviour);
          }
        }
      }
    }

    public void DeactivateAll() {
      while (_activeBehaviours.Count > 0) {
        Deactivate(_activeBehaviours[0]);
      }
    }

    public bool IsActive(IInteractionBehaviour interactionBehaviour) {
      IActivityMonitor monitor;
      if (_registeredBehaviours.TryGetValue(interactionBehaviour, out monitor)) {
        return monitor != null;
      }
      return false; // Not even registered.
    }

    public bool IsRegistered(IInteractionBehaviour interactionBehaviour) {
      return _registeredBehaviours.ContainsKey(interactionBehaviour);
    }

    public void UpdateState(Frame frame) {
      for (int i = _activeBehaviours.Count; i-- != 0;) {
        _registeredBehaviours[_activeBehaviours[i]].UpdateState();
      }

      markOverlappingObjects(frame.Hands);

      activateAndKeepMarkedObjectsAlive();
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
      // This should really be a single call into Unity.
      // Find the set of layers that are currently colliding with the brush layer.
      _brushLayerMask = 0;
      for (int i = 0; i < 32; i++) {
        _brushLayerMask |= Physics.GetIgnoreLayerCollision(_brushLayer, i) ? 0 : (1 << i);
      }

      // Update _markedBehaviours.
      _markedBehaviours.Clear();

      switch (hands.Count) {
        case 0:
          break;
        case 1:
          getSphereResults(hands[0], _markedBehaviours);
          break;
        //Currently broken on android for some unknown reason
#if UNITY_5_4 && false
        case 2:
          if (hands[0].PalmPosition.DistanceTo(hands[1].PalmPosition) > (_overlapRadius * 2.0f)) {
            getSphereResults(hands[0], _markedBehaviours);
            getSphereResults(hands[1], _markedBehaviours);
            break;
          }
          //Use capsule collider for efficiency.  Only need one overlap and no duplicates!
          getCapsuleResults(hands[0], hands[1], _markedBehaviours);
          break;
#endif
        default:
          for (int i = 0; i < hands.Count; i++) {
            getSphereResults(hands[i], _markedBehaviours);
          }
          break;
      }
    }

    private void activateAndKeepMarkedObjectsAlive() {
      //This loop doesn't care about duplicates
      for (int i = 0; i < _markedBehaviours.Count; i++) {
        IInteractionBehaviour behaviour = _markedBehaviours[i];
        IActivityMonitor monitor;
        if (_registeredBehaviours.TryGetValue(behaviour, out monitor)) {
          if (monitor == null) {
            monitor = Activate(behaviour);
          }
          monitor.Revive();
        } else {
          Debug.LogError("Should always be registered, since we checked in handleColliderResults");
        }
      }
    }

    private void handleColliderResults(int count, List<IInteractionBehaviour> list) {
      for (int i = 0; i < count; i++) {
        Collider collider = _colliderResults[i];

        IInteractionBehaviour behaviour = collider.attachedRigidbody.GetComponent<IInteractionBehaviour>();
        if (behaviour == null) {
          Assert.IsNotNull(behaviour, "Only interaction behaviours are allowed to collide with the brush layer.");
          continue;
        }

        // Nothing stopping our overlaps from finding object of other managers, or unregistered objects
        if (!IsRegistered(behaviour)) {
          continue;
        }

        // This will totally add duplicates, we don't care
        list.Add(behaviour);
      }
    }

    private void getSphereResults(Hand hand, List<IInteractionBehaviour> list) {
      int count;
      while (true) {
        count = Physics.OverlapSphereNonAlloc(hand.PalmPosition.ToVector3(),
                                              _overlapRadius,
                                              _colliderResults,
                                              _brushLayerMask,
                                              QueryTriggerInteraction.Ignore);
        if (count < _colliderResults.Length) {
          break;
        }

        //If the count was equal to the array length, we don't know if there are colliders that we missed.
        //Double the length of the array and try again!
        _colliderResults = new Collider[_colliderResults.Length * 2];
      }

      handleColliderResults(count, list);
    }

#if UNITY_5_4
    private void getCapsuleResults(Hand handA, Hand handB, List<IInteractionBehaviour> list) {
      int count;
      while (true) {
        count = Physics.OverlapCapsuleNonAlloc(handA.PalmPosition.ToVector3(),
                                               handB.PalmPosition.ToVector3(),
                                               _overlapRadius,
                                               _colliderResults,
                                               _brushLayerMask,
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
