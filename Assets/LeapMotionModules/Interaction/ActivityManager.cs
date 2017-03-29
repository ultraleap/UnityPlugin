using System.Collections.Generic;
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

    public ActivityManager(InteractionManager manager, float activationRadius) {
      this.manager = manager;
      this.activationRadius = activationRadius;
    }

    public void FixedUpdateHand(Vector3 palmPosition, LeapGui[] guis = null) {
      _activeBehaviours.Clear();

      if (palmPosition != Vector3.zero) {
        int count = GetSphereColliderResults(palmPosition, _colliderResultsBuffer, out _colliderResultsBuffer);
        UpdateActiveList(count, _colliderResultsBuffer);

        if (guis != null) {
          //Check once in each of the GUI's subspaces
          foreach (LeapGui gui in guis) {
            if (!(gui.space.GetType() == typeof(LeapGuiRectSpace))) {
              count = GetSphereColliderResults(transformPoint(palmPosition, gui), _colliderResultsBuffer, out _colliderResultsBuffer);
              UpdateActiveList(count, _colliderResultsBuffer);
            }
          }
        }
      }
    }

    private int GetSphereColliderResults(Vector3 position, Collider[] resultsBuffer_in, out Collider[] resultsBuffer_out) {
      resultsBuffer_out = resultsBuffer_in;

      int overlapCount = 0;
      while (true) {
        overlapCount = Physics.OverlapSphereNonAlloc(position,
                                                     activationRadius * 100,
                                                     resultsBuffer_in,
                                                     ~0,
                                                     QueryTriggerInteraction.Collide);
        if (overlapCount < resultsBuffer_out.Length) {
          break;
        }
        else {
          // Non-allocating sphere-overlap fills the existing _resultsBuffer array.
          // If the output overlapCount is equal to the array's length, there might be more collision results
          // that couldn't be returned because the array wasn't large enough, so try again with increased length.
          // The _in, _out argument setup allows allocating a new array from within this function.
          resultsBuffer_out = new Collider[resultsBuffer_out.Length * 2];
          resultsBuffer_in = resultsBuffer_out;
        }
      }
      return overlapCount;
    }

    private void UpdateActiveList(int numResults, Collider[] results) {
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

    private Vector3 transformPoint(Vector3 worldPoint, LeapGui gui) {
      ITransformer space = gui.space.GetTransformer(gui.transform);
      Vector3 localPalmPos = gui.transform.InverseTransformPoint(worldPoint);
      return gui.transform.TransformPoint(space.InverseTransformPoint(localPalmPos));
    }

  }

}