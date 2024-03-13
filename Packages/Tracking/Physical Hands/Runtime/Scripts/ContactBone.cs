/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    public abstract class ContactBone : MonoBehaviour
    {
        #region Bone Parameters
        internal int finger, joint;
        internal bool isPalm = false;
        public int Finger => finger;
        public int Joint => joint;
        public bool IsPalm => isPalm;

        internal float width = 0f, length = 0f;
        internal Vector3 tipPosition = Vector3.zero;

        internal Rigidbody rigid;
        internal ArticulationBody articulation;

        internal CapsuleCollider boneCollider;
        internal BoxCollider palmCollider;
        internal CapsuleCollider[] palmEdgeColliders;
        public Collider Collider { get { return IsPalm ? palmCollider : boneCollider; } }

        public ContactHand contactHand { get; internal set; }
        internal ContactParent contactParent => contactHand.contactParent;
        #endregion

        #region Interaction Data
        // The distance from any collider that the bone must be to allow the hand to re-enable collision after a teleportation event
        private static float SAFETY_CLOSE_DISTANCE = 0.005f;

        internal class ClosestColliderDirection
        {
            /// <summary>
            /// Is this bone contacting the collider associated with this ClosestColliderDirection?
            /// </summary>
            public bool isContactingCollider;
            /// <summary>
            /// The closest position on the hovering bone
            /// </summary>
            public Vector3 bonePos = Vector3.zero;
            /// <summary>
            /// The direction from the closest pos on the bone to the closest pos on the collider
            /// </summary>
            public Vector3 direction = Vector3.zero;
            /// <summary>
            /// Distance from the edge of the bone
            /// </summary>
            public float distance = 0f;
        }

        private Vector3 _palmColCenter, _palmColHalfExtents, _palmExtentsReduced, _palmExtentsReducedFlipped;
        private Quaternion _palmColRotation;
        private Plane _palmPlane = new Plane();
        private Vector3[] _palmPoints = new Vector3[4];
        private Rigidbody[] _oldRigids = new Rigidbody[32];

        ///<summary>
        /// Dictionary of dictionaries of the directions from this bone to a grabbable object's colliders
        ///</summary>
        private Dictionary<Rigidbody, Dictionary<Collider, ClosestColliderDirection>> _nearbyObjects = new Dictionary<Rigidbody, Dictionary<Collider, ClosestColliderDirection>>(30);
        internal Dictionary<Rigidbody, Dictionary<Collider, ClosestColliderDirection>> NearbyObjects => _nearbyObjects;

        ///<summary>
        /// Dictionary of dictionaries of the directions from this bone to a grabbable object's colliders
        ///</summary>
        private Dictionary<Rigidbody, Dictionary<Collider, ClosestColliderDirection>> _grabbableDirections = new Dictionary<Rigidbody, Dictionary<Collider, ClosestColliderDirection>>(10);
        internal Dictionary<Rigidbody, Dictionary<Collider, ClosestColliderDirection>> GrabbableDirections => _grabbableDirections;


        [field: SerializeField, Tooltip("Is the bone hovering an object? The hover distances are set in the Physics Provider.")]
        public bool IsBoneHovering { get; private set; } = false;
        public bool IsBoneHoveringRigid(Rigidbody rigid)
        {
            return _nearbyObjects.TryGetValue(rigid, out Dictionary<Collider, ClosestColliderDirection> result);
        }

        [field: SerializeField, Tooltip("Is the bone contacting with an object? The contact distances are set in the Physics Provider.")]
        public bool IsBoneContacting { get; private set; } = false;

        /// <summary>
        /// Determine if this ContactBone is touching a given Rigidbody
        /// </summary>
        /// <param name="rigid">The rigidbody to check</param>
        /// <returns>Whether the bone is contacting the given RigidBody</returns>
        public bool IsBoneContactingRigid(Rigidbody rigid)
        {
            _nearbyObjects.TryGetValue(rigid, out Dictionary<Collider, ClosestColliderDirection> result);

            if (result != null)
            {
                foreach (var value in result.Values) // loop through values
                {
                    if (value.isContactingCollider)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Objects this bone is contacting and could possibly grab
        /// </summary>
        private HashSet<Rigidbody> _grabbableObjects = new HashSet<Rigidbody>();

        /// <summary>
        /// Objects that *can* be grabbed, not ones that are
        /// </summary>
        public HashSet<Rigidbody> GrabbableObjects => _grabbableObjects;
        [field: SerializeField, Tooltip("Is the bone ready to grab an object that sits in front of it?")]
        public bool IsBoneReadyToGrab { get; private set; } = false;

        private HashSet<Rigidbody> _grabbedObjects = new HashSet<Rigidbody>();
        /// <summary>
        /// Objects that are currently grabbed according to the grasp helper
        /// </summary>
        public HashSet<Rigidbody> GrabbedObjects => _grabbedObjects;
        /// <summary>
        /// Whether the grasp helper has reported that this bone is grabbing.
        /// If a bone further towards the tip is reported as grabbing, then this bone will also be.
        /// </summary>
        [field: SerializeField, Tooltip("Is the bone currently being used to grab an object via a grasp helper? If a bone further towards the tip is reported as grabbing, then this bone will also be.")]
        public bool IsBoneGrabbing { get; private set; } = false;

        /// <summary>
        /// Will report float.MaxValue if IsHovering is false
        /// </summary>
        public float NearestObjectDistance => _nearestObjectDistance;
        [SerializeField, Tooltip("The distance between the edge of the bone collider and the nearest object. Will report float.MaxValue if IsHovering is false.")]
        private float _nearestObjectDistance = float.MaxValue;
        #endregion

        private Vector3 _debugA, _debugB;

        internal abstract void UpdatePalmBone(Hand hand);
        internal abstract void UpdateBone(Bone prevBone, Bone bone);


        #region Interaction Functions
        internal void ProcessColliderQueue(Collider[] colliderCache, int count)
        {
            // Update the joint collider positions for cached checks, palm uses the collider directly
            if (IsPalm)
            {
                palmCollider.ToWorldSpaceBox(out _palmColCenter, out _palmColHalfExtents, out _palmColRotation);
                _palmColCenter -= _palmColRotation * new Vector3(0, palmCollider.center.y, 0);
                _palmPlane.SetNormalAndPosition(_palmColRotation * Vector3.up, transform.position);

                _palmExtentsReduced = new Vector3(_palmColHalfExtents.x, 0, _palmColHalfExtents.z);
                _palmExtentsReducedFlipped = new Vector3(_palmExtentsReduced.x, 0, _palmExtentsReduced.z * -1);
                _palmPoints[0] = _palmColCenter + (_palmColRotation * _palmExtentsReduced);
                _palmPoints[1] = _palmColCenter + (_palmColRotation * _palmExtentsReducedFlipped);
                _palmPoints[2] = _palmColCenter - (_palmColRotation * _palmExtentsReduced);
                _palmPoints[3] = _palmColCenter - (_palmColRotation * _palmExtentsReducedFlipped);

                width = palmEdgeColliders[0].radius;
                tipPosition = _palmColCenter + (_palmColRotation * (Vector3.forward * _palmColHalfExtents.z));
            }
            else
            {
                boneCollider.ToWorldSpaceCapsule(out tipPosition, out var temp, out width);
            }

            UpdateObjectDistances(colliderCache, count);

            UpdateContactHoverGrabs();

            IsBoneReadyToGrab = _grabbableObjects.Count > 0;
        }

        /// <summary>
        /// Find which colliders near the hand should be tracked in terms of contact possibilities and cache them.
        /// Populate a copy of <see cref="ClosestColliderDirection"/> for each collider for use in contact and grab detection.
        /// </summary>
        /// <param name="colliderCache">A cached array of colliders that are considered close enough to the ContactBone</param>
        /// <param name="count">The number of colliders to look through</param>
        private void UpdateObjectDistances(Collider[] colliderCache, int count)
        {
            _nearestObjectDistance = float.MaxValue;

            float distance, singleObjectDistance, boneDistance;
            Vector3 colliderPos, bonePos, midPoint, direction;
            bool hover;

            Collider collider;

            IsBoneHovering = false;
            IsBoneContacting = false;

            for (int i = 0; i < count; i++)
            {
                collider = colliderCache[i];

                if (IsPalm)
                {
                    // Do palm logic
                    colliderPos = ContactUtils.ClosestPointEstimationFromRectangle(_palmColCenter, _palmColRotation, _palmExtentsReduced.xz(), collider);
                    bonePos = ContactUtils.ClosestPointToRectangleFace(_palmPoints, _palmPlane.ClosestPointOnPlane(collider.transform.position));
                    midPoint = colliderPos + (bonePos + ((bonePos - colliderPos).normalized * width) - colliderPos) / 2f;

                    colliderPos = collider.ClosestPoint(midPoint);
                    bonePos = ContactUtils.ClosestPointToRectangleFace(_palmPoints, _palmPlane.ClosestPointOnPlane(midPoint));
                }
                else
                {
                    colliderPos = collider.ClosestPoint(Vector3.Lerp(transform.position, tipPosition, 0.5f));
                    // Do joint logic
                    bonePos = ContactUtils.GetClosestPointOnFiniteLine(collider.transform.position, transform.position, tipPosition);
                    // We need to extrude the line to get the bone's edge
                    midPoint = colliderPos + (bonePos + ((bonePos - colliderPos).normalized * width) - colliderPos) / 2f;

                    colliderPos = collider.ClosestPoint(midPoint);
                    bonePos = ContactUtils.GetClosestPointOnFiniteLine(midPoint, transform.position, tipPosition);
                }

                distance = Vector3.Distance(colliderPos, bonePos);
                direction = (colliderPos - bonePos).normalized;

                boneDistance = Mathf.Clamp(distance - width, 0, float.MaxValue);

                hover = boneDistance <= contactParent.physicalHandsManager.HoverDistance;

                // Only add valid objects
                if (collider.attachedRigidbody != null)
                {
                    if (hover)
                    {
                        bool contacting = boneDistance <= (IsPalm ? contactParent.physicalHandsManager.ContactDistance * 2f : contactParent.physicalHandsManager.ContactDistance);

                        IsBoneHovering = true;
                        contactHand.isHovering = true;

                        if (contacting)
                        {
                            IsBoneContacting = true;
                            contactHand.isContacting = true;
                        }

                        if (_nearbyObjects.ContainsKey(collider.attachedRigidbody))
                        {
                            if (_nearbyObjects[collider.attachedRigidbody].ContainsKey(collider))
                            {
                                _nearbyObjects[collider.attachedRigidbody][collider].isContactingCollider = contacting;
                                _nearbyObjects[collider.attachedRigidbody][collider].direction = direction;
                                _nearbyObjects[collider.attachedRigidbody][collider].bonePos = bonePos + (direction * width);
                                _nearbyObjects[collider.attachedRigidbody][collider].distance = boneDistance;
                            }
                            else
                            {
                                ClosestColliderDirection closestColliderDirectionPoolObj = Pool<ClosestColliderDirection>.Spawn();
                                closestColliderDirectionPoolObj.isContactingCollider = contacting;
                                closestColliderDirectionPoolObj.direction = direction;
                                closestColliderDirectionPoolObj.bonePos = bonePos + (direction * width);
                                closestColliderDirectionPoolObj.distance = boneDistance;

                                _nearbyObjects[collider.attachedRigidbody].Add(
                                    collider, closestColliderDirectionPoolObj);
                            }
                        }
                        else
                        {
                            Dictionary<Collider, ClosestColliderDirection> colliderDictPoolObj = Pool<Dictionary<Collider, ClosestColliderDirection>>.Spawn();
                            _nearbyObjects.Add(collider.attachedRigidbody, colliderDictPoolObj);

                            ClosestColliderDirection closestColliderDirectionPoolObj = Pool<ClosestColliderDirection>.Spawn();
                            closestColliderDirectionPoolObj.isContactingCollider = contacting;
                            closestColliderDirectionPoolObj.direction = direction;
                            closestColliderDirectionPoolObj.bonePos = bonePos + (direction * width);
                            closestColliderDirectionPoolObj.distance = boneDistance;

                            _nearbyObjects[collider.attachedRigidbody].Add(
                                collider, closestColliderDirectionPoolObj);
                        }
                    }
                    else
                    {
                        if (_nearbyObjects.ContainsKey(collider.attachedRigidbody))
                        {
                            _nearbyObjects[collider.attachedRigidbody].Remove(collider);
                        }
                    }
                }

                // Check if we are close to the collider to ignore collisions after a teleport
                if (boneDistance <= SAFETY_CLOSE_DISTANCE)
                {
                    contactHand.isCloseToObject = true;
                }
            }

            foreach (var colliderPairs in _nearbyObjects)
            {
                singleObjectDistance = float.MaxValue;
                foreach (var col in colliderPairs.Value)
                {
                    if (col.Value.distance < singleObjectDistance)
                    {
                        singleObjectDistance = col.Value.distance;
                        if (singleObjectDistance < _nearestObjectDistance)
                        {
                            _nearestObjectDistance = singleObjectDistance;
                            _debugA = col.Value.bonePos;
                            _debugB = col.Value.bonePos + (col.Value.direction * (col.Value.distance));
                        }
                    }
                }
            }
        }

        private void UpdateContactHoverGrabs()
        {
            int oldRigidCount = 0;

            foreach (var rigid in _nearbyObjects.Keys)
            {
                if (_nearbyObjects[rigid].Count == 0)
                {
                    _oldRigids[oldRigidCount] = rigid;
                    oldRigidCount++;
                }
                else
                {
                    bool isGrabable = false;

                    foreach (var value in _nearbyObjects[rigid].Values)
                    {
                        if (value.isContactingCollider)
                        {
                            isGrabable = true;
                            break;
                        }
                    }

                    // Add the Grab objects
                    if (isGrabable)
                    {
                        // add to grabbable rb dicts
                        _grabbableObjects.Add(rigid);

                        if (_grabbableDirections.ContainsKey(rigid))
                        {
                            _grabbableDirections[rigid] = _nearbyObjects[rigid];
                        }
                        else
                        {
                            _grabbableDirections.Add(rigid, _nearbyObjects[rigid]);
                        }
                    }
                    else
                    {
                        // Remove ungrabbable objects
                        _grabbableObjects.Remove(rigid);
                        _grabbableDirections.Remove(rigid);
                    }
                }
            }

            for (int i = 0; i < oldRigidCount; i++)
            {
                // Remove no longer contacting objects
                if (_nearbyObjects.TryGetValue(_oldRigids[i], out Dictionary<Collider, ClosestColliderDirection> disposePoolObj))
                {
                    disposePoolObj.Clear();
                    Pool<Dictionary<Collider, ClosestColliderDirection>>.Recycle(disposePoolObj);
                }
                _nearbyObjects.Remove(_oldRigids[i]);
                _grabbableObjects.Remove(_oldRigids[i]);


            }
        }

        internal void AddGrabbing(Rigidbody rigid)
        {
            _grabbedObjects.Add(rigid);
            IsBoneGrabbing = _grabbedObjects.Count > 0;
        }

        internal void RemoveGrabbing(Rigidbody rigid)
        {
            _grabbedObjects.Remove(rigid);
            IsBoneGrabbing = _grabbedObjects.Count > 0;
        }

        private void OnDrawGizmos()
        {
            if (IsBoneContacting)
            {
                Gizmos.color = Color.red;
            }
            else if (IsBoneHovering)
            {
                Gizmos.color = Color.cyan;
            }
            else
            {
                Gizmos.color = Color.grey;
            }
            Gizmos.DrawSphere(_debugA, 0.001f);
            Gizmos.DrawSphere(_debugB, 0.001f);
            Gizmos.DrawLine(_debugA, _debugB);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_palmColCenter, 0.001f);
            for (int i = 0; i < _palmPoints.Length; i++)
            {
                Gizmos.DrawSphere(_palmPoints[i], 0.001f);
            }
        }

        #endregion
    }
}