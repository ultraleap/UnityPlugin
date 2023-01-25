/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction
{
    /// <summary>
    /// ActivityManager is a wrapper around PhysX sphere queries for arbitrary Unity objects.
    /// "Active" objects are objects found in the latest query. It's also possible to get the
    /// sets of objects that just began or stopped being active since the last query; this
    /// requires enabling the trackStateChanges setting.
    /// </summary>
    public class ActivityManager<T>
    {

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

        /// <summary>
        /// This function, if set to a non-null value, overrides the activationLayerMask
        /// setting with the result of calling the function. Use this if your application
        /// state will modify the layer mask to use for the activity manager.
        /// </summary>
        public Func<int> activationLayerFunction = null;

        /// <summary>
        /// This is the function by which the ActivityManager converts the Colliders it finds
        /// through PhysX queries into Ts to be placed in the ActiveObjects set.
        /// 
        /// Only objects with at least one Collider for which this function returns a non-null T
        /// will be added to the ActiveObjects set.
        /// </summary>
        public Func<Collider, T> filter = null;

        private HashSet<T> _activeObjects = new HashSet<T>();
        /// <summary>
        /// Returns the currently "active" objects -- objects that were within the latest sphere query.
        /// </summary>
        public HashSet<T> ActiveObjects { get { return _activeObjects; } }

        /// <summary>
        /// If set to true, BeganActive and EndedActive will be calculated and populated every time a
        /// new query occurs.
        /// </summary>
        public bool trackStateChanges = false;
        private HashSet<T> _activeObjectsLastFrame = new HashSet<T>();
        private HashSet<T> _beganActiveObjects = new HashSet<T>();
        /// <summary>
        /// If trackStateChanges is enabled (on by default), contains the objects that just started being active since
        /// the last query.
        /// </summary>
        public HashSet<T> BeganActive
        {
            get { return _beganActiveObjects; }
        }
        private HashSet<T> _endedActiveObjects = new HashSet<T>();
        /// <summary>
        /// If trackStateChanges is enabled (on by default), contains the objects that just stopped being active since
        /// the last query.
        /// </summary>
        public HashSet<T> EndedActive
        {
            get { return _endedActiveObjects; }
        }

        public ActivityManager(float activationRadius, Func<Collider, T> filter)
        {
            this.activationRadius = activationRadius;
            this.filter = filter;
        }

        [ThreadStatic]
        private static Collider[] s_colliderResultsBuffer = new Collider[32];
        public void UpdateActivityQuery(Vector3? queryPosition)
        {
            _activeObjects.Clear();

            // Make sure collider results buffer exists (for other threads; see ThreadStatic)
            if (s_colliderResultsBuffer == null || s_colliderResultsBuffer.Length < 32)
            {
                s_colliderResultsBuffer = new Collider[32];
            }

            // Update object activity by activation radius around the query position (PhysX colliders necessary).
            // queryPosition can be null, in which case no objects will be found, and they will all become inactive.
            if (queryPosition.HasValue)
            {
                Vector3 actualQueryPosition = queryPosition.GetValueOrDefault();

                int count = GetSphereColliderResults(actualQueryPosition, ref s_colliderResultsBuffer);
                UpdateActiveList(count, s_colliderResultsBuffer);
            }

            if (trackStateChanges)
            {
                _endedActiveObjects.Clear();
                _beganActiveObjects.Clear();

                foreach (var behaviour in _activeObjects)
                {
                    if (!_activeObjectsLastFrame.Contains(behaviour))
                    {
                        _beganActiveObjects.Add(behaviour);
                    }
                }

                foreach (var behaviour in _activeObjectsLastFrame)
                {
                    if (!_activeObjects.Contains(behaviour))
                    {
                        _endedActiveObjects.Add(behaviour);
                    }
                }

                _activeObjectsLastFrame.Clear();
                foreach (var behaviour in _activeObjects)
                {
                    _activeObjectsLastFrame.Add(behaviour);
                }
            }
        }

        private int GetSphereColliderResults(Vector3 position, ref Collider[] resultsBuffer)
        {
            int layerMask = activationLayerFunction == null ? activationLayerMask : activationLayerFunction();
            int overlapCount = 0;
            while (true)
            {
                overlapCount = Physics.OverlapSphereNonAlloc(position,
                                                             activationRadius,
                                                             resultsBuffer,
                                                             layerMask,
                                                             QueryTriggerInteraction.Collide);
                if (overlapCount < resultsBuffer.Length)
                {
                    break;
                }
                else
                {
                    // Non-allocating sphere-overlap fills the existing _resultsBuffer array.
                    // If the output overlapCount is equal to the array's length, there might be more collision results
                    // that couldn't be returned because the array wasn't large enough, so try again with increased length.
                    // The _in, _out argument setup allows allocating a new array from within this function.
                    resultsBuffer = new Collider[resultsBuffer.Length * 2];
                }
            }
            return overlapCount;
        }

        private void UpdateActiveList(int numResults, Collider[] results)
        {
            if (filter == null)
            {
                Debug.LogWarning("[ActivityManager] No filter provided for this ActivityManager; no objects will ever be returned."
                               + "Please make sure you specify a non-null filter property of any ActivityManagers you wish to use.");
                return;
            }

            for (int i = 0; i < numResults; i++)
            {
                T obj = filter(results[i]);
                if (obj != null)
                {
                    _activeObjects.Add(obj);
                }
            }
        }
    }
}