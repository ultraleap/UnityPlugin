using Leap.Unity.Space;
using Leap.Unity.UI.Interaction.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  /// <summary>
  /// ActivityManager is a wrapper around PhysX sphere queries for InteractionBehaviours.
  /// "Active" objects are objects found in the latest query. It's also possible to get the
  /// sets of objects that just began or stopped being active since the last query.
  /// </summary>
  public class ActivityManager<T> {

    /// <summary> The radius of the query in world-space. </summary>
    public float activationRadius;

    /// <summary>
    /// The layer mask against which to query for active objects. The ActivityManager will only
    /// find objects in these layers. By default, the ActivityManager will query all layers, but
    /// this is highly inefficient.
    /// 
    /// See SingleLayer and use bitwise operations on their layerMasks for a convenient way to
    /// express layer masks.
    /// <see cref="SingleLayer"/>
    /// </summary>
    public int activationLayerMask = ~0;

    /// <summary> If non-null, only objects with at least one Collider for which this function
    /// returns a non-null T will be added to the active behaviours list. </summary>
    public Func<Collider, T> filter = null;

    private HashSet<T> _activeObjects = new HashSet<T>();
    /// <summary>
    /// Returns the currently "active" objects -- objects that were within the latest sphere query.
    /// </summary>
    public HashSet<T> ActiveObjects { get { return _activeObjects; } }

    /// <summary> If set to true, BeganActive and EndedActive will be calculated and populated. </summary>
    public bool trackStateChanges = true;
    private HashSet<T> _activeObjectsLastFrame = new HashSet<T>();
    private HashSet<T> _beganActiveObjects = new HashSet<T>();
    /// <summary>
    /// If trackStateChanges is enabled (on by default), contains the objects that just started being active since
    /// the last query.
    /// </summary>
    public HashSet<T> BeganActive {
      get { return _beganActiveObjects; }
    }
    private HashSet<T> _endedActiveObjects = new HashSet<T>();
    /// <summary>
    /// If trackStateChanges is enabled (on by default), contains the objects that just stopped being active since
    /// the last query.
    /// </summary>
    public HashSet<T> EndedActive {
      get { return _endedActiveObjects; }
    }

    public ActivityManager(float activationRadius) {
      this.activationRadius = activationRadius;
    }

    [ThreadStatic]
    private static Collider[] s_colliderResultsBuffer = new Collider[32];
    public void FixedUpdateQueryPosition(Vector3 palmPosition, List<LeapSpace> spaces = null) {
      using (new ProfilerSample("Update Activity Manager")) {
        _activeObjects.Clear();
        
        // Make sure collider results buffer exists (for other threads; see ThreadStatic)
        if (s_colliderResultsBuffer == null || s_colliderResultsBuffer.Length < 32) {
          s_colliderResultsBuffer = new Collider[32];
        }

        if (palmPosition != Vector3.zero) {
          int count = GetSphereColliderResults(palmPosition, ref s_colliderResultsBuffer);
          UpdateActiveList(count, s_colliderResultsBuffer);

          if (spaces != null) {
            //Check once in each of the GUI's subspaces
            foreach (LeapSpace space in spaces) {
              count = GetSphereColliderResults(transformPoint(palmPosition, space), ref s_colliderResultsBuffer);
              UpdateActiveList(count, s_colliderResultsBuffer);
            }
          }
        }

        if (trackStateChanges) {
          _endedActiveObjects.Clear();
          _beganActiveObjects.Clear();

          foreach (var behaviour in _activeObjects) {
            if (!_activeObjectsLastFrame.Contains(behaviour)) {
              _beganActiveObjects.Add(behaviour);
            }
          }

          foreach (var behaviour in _activeObjectsLastFrame) {
            if (!_activeObjects.Contains(behaviour)) {
              _endedActiveObjects.Add(behaviour);
            }
          }

          _activeObjectsLastFrame.Clear();
          foreach (var behaviour in _activeObjects) {
            _activeObjectsLastFrame.Add(behaviour);
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
                                                       activationLayerMask,
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
        T obj = filter(results[i]);
        if (obj != null) {
          _activeObjects.Add(obj);
        }
      }
    }

    private Vector3 transformPoint(Vector3 worldPoint, LeapSpace space) {
      Vector3 localPalmPos = space.transform.InverseTransformPoint(worldPoint);
      return space.transform.TransformPoint(space.transformer.InverseTransformPoint(localPalmPos));
    }
  }
}