using Leap.Unity.Space;
using Leap.Unity.UI.Interaction.Internal;
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
    private HashSet<IInteractionBehaviour> _activeBehaviours = new HashSet<IInteractionBehaviour>();

    public HashSet<IInteractionBehaviour> ActiveBehaviours {
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

    public void FixedUpdatePosition(Vector3 palmPosition, List<LeapSpace> spaces = null) {
      using (new ProfilerSample("Update "+ (spaces==null?"Touch" : "Hover") + " Actvity Manager")) {
        _activeBehaviours.Clear();

        if (palmPosition != Vector3.zero) {
          int count = GetSphereColliderResults(palmPosition, ref _colliderResultsBuffer);
          UpdateActiveList(count, _colliderResultsBuffer);

          if (spaces != null) {
            //Check once in each of the GUI's subspaces
            foreach (LeapSpace space in spaces) {
              count = GetSphereColliderResults(transformPoint(palmPosition, space), ref _colliderResultsBuffer);
              UpdateActiveList(count, _colliderResultsBuffer);
            }
          }
        }
      }
    }

    private int GetSphereColliderResults(Vector3 position, ref Collider[] resultsBuffer) {
      using (new ProfilerSample("GetSphereColliderResults()")) {
        int overlapCount = 0;
        while (true) {
          overlapCount = Physics.OverlapSphereNonAlloc(position,
                                                       activationRadius,
                                                       resultsBuffer,
                                                       manager.interactionLayer.layerMask | manager.interactionNoContactLayer.layerMask,
                                                       QueryTriggerInteraction.Collide);
          if (overlapCount < resultsBuffer.Length) {
            break;
          } else {
            // Non-allocating sphere-overlap fills the existing _resultsBuffer array.
            // If the output overlapCount is equal to the array's length, there might be more collision results
            // that couldn't be returned because the array wasn't large enough, so try again with increased length.
            // The _in, _out argument setup allows allocating a new array from within this function.
            resultsBuffer = new Collider[resultsBuffer.Length * 2];
          }
        }
        return overlapCount;
      }
    }

    private void UpdateActiveList(int numResults, Collider[] results) {
      for (int i = 0; i < numResults; i++) {
        if (results[i].attachedRigidbody != null) {
          Rigidbody body = results[i].attachedRigidbody;
          IInteractionBehaviour interactionObj;
          if (body != null && manager.rigidbodyRegistry.TryGetValue(body, out interactionObj)) {
            _activeBehaviours.Add(interactionObj);
          }
        }
      }
    }

    private Vector3 transformPoint(Vector3 worldPoint, LeapSpace space) {
      Vector3 localPalmPos = space.transform.InverseTransformPoint(worldPoint);
      return space.transform.TransformPoint(space.transformer.InverseTransformPoint(localPalmPos));
    }
  }
}