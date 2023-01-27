/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity
{

    /**
     * Detects when the parent GameObject is within the specified distance
     * of one of the target objects.
     * @since 4.1.2
     */
    public class ProximityDetector : Detector
    {
        /**
         * Dispatched when the proximity check succeeds.
         * The ProximityEvent object provides a reference to the proximate GameObject. 
         * @since 4.1.2
         */
        [Tooltip("Dispatched when close enough to a target.")]
        public ProximityEvent OnProximity;
        /**
         * The interval at which to check palm direction.
         * @since 4.1.2
         */
        [Units("seconds")]
        [MinValue(0)]
        [Tooltip("The interval in seconds at which to check this detector's conditions.")]
        public float Period = .1f; //seconds

        /**
         * The list of objects which can activate the detector by proximity.
         * @since 4.1.2
         */
        [Header("Detector Targets")]
        [Tooltip("The list of target objects.")]
        [DisableIf("UseLayersNotList", true)]
        public GameObject[] TargetObjects;

        /**
        * Include objects with the specified tag in the list of target objects.
        * Objects are not added dynamically, however, so objects spawned with the tag will
        * not be included.
        * @since 4.1.3
        */
        [Tooltip("Objects with this tag are added to the list of targets.")]
        [DisableIf("UseLayersNotList", true)]
        public string TagName = "";

        [Tooltip("Use a Layer instead of the target list.")]
        public bool UseLayersNotList = false;
        [Tooltip("The Layer containing the objects to check.")]
        [DisableIf("UseLayersNotList", false)]
        public LayerMask Layer;

        /**
         * The distance in meters between this game object and the target game object that
         * will pass the proximity check.
         * @since 4.1.2
         */
        [Header("Distance Settings")]
        [Tooltip("The target distance in meters to activate the detector.")]
        [MinValue(0)]
        public float OnDistance = .01f; //meters

        /**
         * The distance in meters between this game object and the target game object that
         * will turn off the detector. 
         * @since 4.1.2
         */
        [Tooltip("The distance in meters at which to deactivate the detector.")]
        public float OffDistance = .015f; //meters

        /**
         * The object that is close to the activated detector.
         * 
         * If more than one target object is within the required distance, it is
         * undefined which object will be current. Set to null when no targets
         * are close enough.
         * @since 4.1.2
         */
        public GameObject CurrentObject { get { return _currentObj; } }
        /** Whether to draw the detector's Gizmos for debugging. (Not every detector provides gizmos.)
         * @since 4.1.2 
         */
        [Header("")]
        [Tooltip("Draw this detector's Gizmos, if any. (Gizmos must be on in Unity edtor, too.)")]
        public bool ShowGizmos = true;

        private IEnumerator proximityWatcherCoroutine;
        private GameObject _currentObj = null;

        protected virtual void OnValidate()
        {
            //Activate value cannot be less than deactivate value
            if (OffDistance < OnDistance)
            {
                OffDistance = OnDistance;
            }
        }

        void Awake()
        {
            proximityWatcherCoroutine = proximityWatcher();
            if (TagName != "")
            {
                GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(TagName);
                List<GameObject> targets = new List<GameObject>(taggedObjects.Length + TargetObjects.Length);
                for (int t = 0; t < TargetObjects.Length; t++)
                {
                    targets.Add(TargetObjects[t]);
                }
                for (int t = 0; t < taggedObjects.Length; t++)
                {
                    targets.Add(taggedObjects[t]);
                }
                TargetObjects = targets.ToArray();
            }
        }

        void OnEnable()
        {
            StopCoroutine(proximityWatcherCoroutine);
            StartCoroutine(proximityWatcherCoroutine);
        }

        void OnDisable()
        {
            StopCoroutine(proximityWatcherCoroutine);
            Deactivate();
        }

        IEnumerator proximityWatcher()
        {
            bool proximityState = false;
            float onSquared, offSquared; //Use squared distances to avoid taking square roots
            while (true)
            {
                onSquared = OnDistance * OnDistance;
                offSquared = OffDistance * OffDistance;
                if (_currentObj != null)
                {
                    if (distanceSquared(_currentObj) > offSquared)
                    {
                        _currentObj = null;
                        proximityState = false;
                    }
                }
                else
                {
                    if (UseLayersNotList)
                    {
                        Collider[] nearby = Physics.OverlapSphere(transform.position, OnDistance, Layer);
                        if (nearby.Length > 0)
                        {
                            _currentObj = nearby[0].gameObject;
                            proximityState = true;
                            OnProximity.Invoke(_currentObj);
                        }
                    }
                    else
                    {
                        for (int obj = 0; obj < TargetObjects.Length; obj++)
                        {
                            GameObject target = TargetObjects[obj];
                            if (distanceSquared(target) < onSquared)
                            {
                                _currentObj = target;
                                proximityState = true;
                                OnProximity.Invoke(_currentObj);
                                break; // pick first match
                            }
                        }
                    }
                }
                if (proximityState)
                {
                    Activate();
                }
                else
                {
                    Deactivate();
                }
                yield return new WaitForSeconds(Period);
            }
        }

        private float distanceSquared(GameObject target)
        {
            Collider targetCollider = target.GetComponent<Collider>();
            Vector3 closestPoint;
            if (targetCollider != null)
            {
                closestPoint = targetCollider.ClosestPointOnBounds(transform.position);
            }
            else
            {
                closestPoint = target.transform.position;
            }
            return (closestPoint - transform.position).sqrMagnitude;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (ShowGizmos)
            {
                if (IsActive)
                {
                    Gizmos.color = Color.green;
                }
                else
                {
                    Gizmos.color = Color.red;
                }
                Gizmos.DrawWireSphere(transform.position, OnDistance);
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, OffDistance);
            }
        }
#endif
    }

    /**
     * An event class that is dispatched by a ProximityDetector when the detector's
     * game object comes close enough to a game object in its target list.
     * The event parameters provide the proximate game object.
     * @since 4.1.2
     */
    [System.Serializable]
    public class ProximityEvent : UnityEvent<GameObject> { }
}