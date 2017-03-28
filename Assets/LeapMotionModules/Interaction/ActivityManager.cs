using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Query;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  /// <summary> The Activity Manager encapsulates the logic that determines which
  /// InteractionBehaviours are within an interactible distance and which interactions
  /// they should be considered for (hovering/contact/grasping).
  /// 
  /// Essentially, the ActivityManager is a wrapper around PhysX sphere queries around
  /// Hands for InteractionBehaviours.</summary>
  public class ActivityManager {

    public float activationRadius;
    public InteractionManager manager;

    private Collider[] _colliderResultsBuffer = new Collider[32];
    private HashSet<InteractionBehaviourBase> _activeBehaviours = new HashSet<InteractionBehaviourBase>();

    public HashSet<InteractionBehaviourBase> ActiveBehaviours {
      get { return _activeBehaviours; }
    }

    public ActivityManager(InteractionManager manager) {
      this.manager = manager;
      this.activationRadius = 1F;
    }

    public ActivityManager(InteractionManager manager, float activationRadius) {
      this.manager = manager;
      this.activationRadius = activationRadius;
    }

    public void FixedUpdateHand(Hand hand) {
      int count = GetSphereColliderResults(hand, ref _colliderResultsBuffer);
      UpdateActiveList(count, _colliderResultsBuffer);
    }

    private int GetSphereColliderResults(Hand hand, ref Collider[] resultsBuffer) {
      if (hand == null) return 0;

      int overlapCount = 0;
      while (true) {
        overlapCount = Physics.OverlapSphereNonAlloc(hand.PalmPosition.ToVector3(),
                                                         activationRadius,
                                                         resultsBuffer,
                                                         manager.InteractionLayer.layerMask,
                                                         QueryTriggerInteraction.Collide);
        if (overlapCount < resultsBuffer.Length) {
          break;
        }
        else {
          // Non-allocating sphere-overlap fills the existing _resultsBuffer array.
          // If the output overlapCount is equal to the array's length, there might be more collision results
          // that couldn't be returned because the array wasn't large enough, so try again with increased length.
          // The _in, _out argument setup allows allocating a new array from within this function.
          resultsBuffer = new Collider[resultsBuffer.Length * 2];
        }
      }
      return overlapCount;
    }

    private void UpdateActiveList(int numResults, Collider[] results) {
      _activeBehaviours.Clear();

      for (int i = 0; i < numResults; i++) {
        if (results[i].attachedRigidbody != null) {
          Rigidbody body = results[i].attachedRigidbody;
          InteractionBehaviourBase interactionObj;
          if (body != null && manager.RigidbodyRegistry.TryGetValue(body, out interactionObj)) {
            _activeBehaviours.Add(interactionObj);
          }
        }
      }
    }

  }

}