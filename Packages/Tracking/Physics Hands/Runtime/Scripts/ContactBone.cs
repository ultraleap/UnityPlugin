using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.PhysicsHands
{
    public abstract class ContactBone : MonoBehaviour
    {
        #region Bone Parameters
        internal int finger, joint;
        internal bool isPalm = false;
        public int Finger => finger;
        public int Joint => joint;
        public bool IsPalm => isPalm;

        internal float width = 0f, length = 0f, palmThickness = 0f;
        internal Vector3 tipPosition = Vector3.zero;
        internal Vector3 wristPosition = Vector3.zero;
        internal Vector3 center = Vector3.zero;

        internal Rigidbody rigid;
        internal ArticulationBody articulation;

        internal CapsuleCollider boneCollider;
        internal BoxCollider palmCollider;
        internal CapsuleCollider[] palmEdgeColliders;
        public Collider Collider { get { return IsPalm ? palmCollider : boneCollider; } }


        internal ContactHand contactHand;
        internal ContactParent contactParent => contactHand.contactParent;
        #endregion

        #region Physics Data

        #endregion

        #region Interaction Data
        private static float SAFETY_CLOSE_DISTANCE = 0.005f;

        public class ClosestObjectDirection
        {
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
        private Collider[] _oldColliders = new Collider[32];

        private Dictionary<Rigidbody, HashSet<Collider>> _colliderObjects = new Dictionary<Rigidbody, HashSet<Collider>>();
        private Dictionary<Rigidbody, float> _objectDistances = new Dictionary<Rigidbody, float>();

        private Dictionary<Rigidbody, HashSet<Collider>> _hoverObjects = new Dictionary<Rigidbody, HashSet<Collider>>();
        public Dictionary<Rigidbody, HashSet<Collider>> HoverObjects => _hoverObjects;

        [field: SerializeField, Tooltip("Is the bone hovering an object? The hover distances are set in the Physics Provider.")]
        public bool IsBoneHovering { get; private set; } = false;
        public bool IsBoneHoveringRigid(Rigidbody rigid)
        {
            return _hoverObjects.TryGetValue(rigid, out HashSet<Collider> result);
        }

        private Dictionary<Rigidbody, HashSet<Collider>> _contactObjects = new Dictionary<Rigidbody, HashSet<Collider>>();
        public Dictionary<Rigidbody, HashSet<Collider>> ContactObjects => _contactObjects;
        [field: SerializeField, Tooltip("Is the bone contacting with an object? The contact distances are set in the Physics Provider.")]
        public bool IsBoneContacting { get; private set; } = false;

        public bool IsBoneContactingRigid(Rigidbody rigid)
        {
            return _contactObjects.TryGetValue(rigid, out HashSet<Collider> result);
        }

        private HashSet<Rigidbody> _grabObjects = new HashSet<Rigidbody>();

        ///<summary>
        /// Dictionary of dictionaries of the directions from this bone to a hovered object's colliders
        ///</summary>
        public Dictionary<Rigidbody, Dictionary<Collider, ClosestObjectDirection>> HoverDirections => _objectDirections;
        private Dictionary<Rigidbody, Dictionary<Collider, ClosestObjectDirection>> _objectDirections = new Dictionary<Rigidbody, Dictionary<Collider, ClosestObjectDirection>>();

        ///<summary>
        /// Dictionary of dictionaries of the directions from this bone to a grabbable object's colliders
        ///</summary>
        public Dictionary<Rigidbody, Dictionary<Collider, ClosestObjectDirection>> GrabbableDirections => _grabbableDirections;
        private Dictionary<Rigidbody, Dictionary<Collider, ClosestObjectDirection>> _grabbableDirections = new Dictionary<Rigidbody, Dictionary<Collider, ClosestObjectDirection>>();

        /// <summary>
        /// Objects that *can* be grabbed, not ones that are
        /// </summary>
        public HashSet<Rigidbody> GrabbableObjects => _grabObjects;
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
        public float ObjectDistance => _objectDistance;
        [SerializeField, Tooltip("The distance between the edge of the bone collider and the nearest object. Will report float.MaxValue if IsHovering is false.")]
        private float _objectDistance = float.MaxValue;
        #endregion

        private Vector3 _debugA, _debugB;

        internal abstract void UpdatePalmBone(Hand hand);
        internal abstract void UpdateBone(Bone prevBone, Bone bone);

        internal abstract void PostFixedUpdateBone();


        #region Interaction Functions
        internal void ProcessColliderQueue(Collider[] colliderCache, int count)
        {
            RemoveOldColliders(colliderCache, count);

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
            }
            else
            {
                boneCollider.ToWorldSpaceCapsule(out tipPosition, out var temp, out width);
            }

            UpdateObjectDistances(colliderCache, count);

            UpdateContactHoverGrabs();

            IsBoneHovering = _hoverObjects.Count > 0;
            IsBoneContacting = _contactObjects.Count > 0;
            IsBoneReadyToGrab = _grabObjects.Count > 0;
        }

        private void RemoveOldColliders(Collider[] colliderCache, int count)
        {
            foreach (var rigid in _colliderObjects.Keys)
            {
                int oldColliderCount = 0;
                foreach (var collider in _colliderObjects[rigid])
                {
                    if (!colliderCache.ContainsRange(collider, count))
                    {
                        _oldColliders[oldColliderCount] = collider;
                        oldColliderCount++;
                        if (oldColliderCount > _oldColliders.Length)
                        {
                            break;
                        }
                    }
                }

                for (int i = 0; i < oldColliderCount; i++)
                {
                    _colliderObjects[rigid].Remove(_oldColliders[i]);

                    if (_oldColliders[i].attachedRigidbody != null)
                    {
                        _objectDirections[_oldColliders[i].attachedRigidbody].Remove(_oldColliders[i]);
                        _hoverObjects[_oldColliders[i].attachedRigidbody].Remove(_oldColliders[i]);
                        _contactObjects[_oldColliders[i].attachedRigidbody].Remove(_oldColliders[i]);
                    }
                }
            }
        }

        private void UpdateObjectDistances(Collider[] colliderCache, int count)
        {
            _objectDistance = float.MaxValue;

            float distance, singleObjectDistance, boneDistance;
            Vector3 colliderPos, bonePos, midPoint, direction;
            bool hover;

            Collider collider;
            for (int i = 0; i < count; i++)
            {
                collider = colliderCache[i];

                // Make sure we don't add invalid objects
                if (collider.attachedRigidbody == null)
                    continue;

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

                hover = boneDistance <= contactParent.physicsHandsManager.HoverDistance;

                if (hover)
                {
                    if (_objectDirections.ContainsKey(collider.attachedRigidbody))
                    {
                        if (_objectDirections[collider.attachedRigidbody].ContainsKey(collider))
                        {
                            _objectDirections[collider.attachedRigidbody][collider].direction = direction;
                            _objectDirections[collider.attachedRigidbody][collider].bonePos = bonePos + (direction * width);
                            _objectDirections[collider.attachedRigidbody][collider].distance = boneDistance;
                        }
                        else
                        {
                            _objectDirections[collider.attachedRigidbody].Add(
                                collider,
                                new ClosestObjectDirection()
                                {
                                    bonePos = bonePos + (direction * width),
                                    direction = direction,
                                    distance = boneDistance
                                }
                            );
                        }
                    }
                    else
                    {
                        _objectDirections.Add(collider.attachedRigidbody, new Dictionary<Collider, ClosestObjectDirection>());

                        _objectDirections[collider.attachedRigidbody].Add(
                            collider,
                            new ClosestObjectDirection()
                            {
                                bonePos = bonePos + (direction * width),
                                direction = direction,
                                distance = boneDistance
                            });
                    }
                }
                else
                {
                    if (_objectDirections.ContainsKey(collider.attachedRigidbody))
                    {
                        _objectDirections[collider.attachedRigidbody].Remove(collider);
                    }
                }

                // Hover
                UpdateHoverCollider(collider.attachedRigidbody, collider, hover);

                // Contact
                UpdateContactCollider(collider.attachedRigidbody, collider, boneDistance <= (IsPalm ? contactParent.physicsHandsManager.ContactDistance * 2f : contactParent.physicsHandsManager.ContactDistance));

                // Safety
                UpdateSafetyCollider(collider, boneDistance == 0, boneDistance <= SAFETY_CLOSE_DISTANCE);
            }

            foreach (var colliderPairs in _objectDirections)
            {
                singleObjectDistance = float.MaxValue;
                foreach (var col in colliderPairs.Value)
                {
                    if (col.Value.distance < singleObjectDistance)
                    {
                        singleObjectDistance = col.Value.distance;
                        if (singleObjectDistance < _objectDistance)
                        {
                            _objectDistance = singleObjectDistance;
                            _debugA = col.Value.bonePos;
                            _debugB = col.Value.bonePos + (col.Value.direction * (col.Value.distance));
                        }
                    }
                }

                // Store the distance for the object
                if (_objectDistances.ContainsKey(colliderPairs.Key))
                {
                    _objectDistances[colliderPairs.Key] = singleObjectDistance;
                }
                else
                {
                    _objectDistances.Add(colliderPairs.Key, singleObjectDistance);
                }
            }
        }

        // TODO: add events
        private void UpdateHoverCollider(Rigidbody rigid, Collider collider, bool hover)
        {
            if (hover)
            {
                contactHand.isHovering = true;

                if (_hoverObjects.ContainsKey(rigid))
                {
                    _hoverObjects[rigid].Add(collider);
                }
                else
                {
                    _hoverObjects.Add(rigid, new HashSet<Collider>() { collider });
                }
            }
            else
            {
                if (_hoverObjects.ContainsKey(rigid))
                {
                    _hoverObjects[rigid].Remove(collider);
                }
            }
        }

        // TODO: add events
        private void UpdateContactCollider(Rigidbody rigid, Collider collider, bool contact)
        {
            if (contact)
            {
                contactHand.isContacting = true;

                if (_contactObjects.ContainsKey(rigid))
                {
                    _contactObjects[rigid].Add(collider);
                }
                else
                {
                    _contactObjects.Add(rigid, new HashSet<Collider>() { collider });
                }
            }
            else
            {
                if (_contactObjects.ContainsKey(rigid))
                {
                    _contactObjects[rigid].Remove(collider);
                }
            }
        }

        private void UpdateSafetyCollider(Collider collider, bool intersect, bool closeToObject)
        {
            if (WillColliderAffectBone(collider))
            {
                if (closeToObject)
                {
                    contactHand.isCloseToObject = true;
                }
                if (intersect)
                {
                    contactHand.isIntersecting = true;
                }
            }
        }

        private void UpdateContactHoverGrabs()
        {
            int oldRigidCount = 0;

            foreach(var rigid in _hoverObjects.Keys)
            {
                if (_hoverObjects[rigid].Count == 0)
                {
                    _oldRigids[oldRigidCount] = rigid;
                    oldRigidCount++;
                }
            }

            for(int i = 0; i < oldRigidCount; i++)
            {
                // Remove no longer hovering objects
                _hoverObjects.Remove(_oldRigids[i]);
                _objectDirections.Remove(_oldRigids[i]);
                _objectDistances.Remove(_oldRigids[i]);
            }

            oldRigidCount = 0;

            foreach (var rigid in _contactObjects.Keys)
            {
                if (_contactObjects[rigid].Count == 0)
                {
                    _oldRigids[oldRigidCount] = rigid;
                    oldRigidCount++;
                }
                else
                {
                    // Add the Grab objects
                    if (IsObjectGrabbable(rigid))
                    {
                        // add to grabbable rb dicts
                        _grabObjects.Add(rigid);

                        if (_grabbableDirections.ContainsKey(rigid))
                        {
                            _grabbableDirections[rigid] = _objectDirections[rigid];
                        }
                        else
                        {
                            _grabbableDirections.Add(rigid, _objectDirections[rigid]);
                        }
                    }
                    else
                    {
                        // Remove ungrabbed objects
                        _grabObjects.Remove(rigid);
                        _grabbableDirections.Remove(rigid);
                    }
                }
            }

            for (int i = 0; i < oldRigidCount; i++)
            {
                // Remove no longer contacting objects
                _contactObjects.Remove(_oldRigids[i]);
                _grabObjects.Remove(_oldRigids[i]);
            }
        }

        private bool IsObjectGrabbable(Rigidbody rigidbody)
        {
            if (_objectDirections.TryGetValue(rigidbody, out var hoveredColliders))
            {
                Vector3 bonePosCenter, closestPoint, boneCenterToColliderDirection, jointDirection;
                foreach (var hoveredColliderDirection in hoveredColliders)
                {
                    bonePosCenter = transform.TransformPoint(0, 0, transform.InverseTransformPoint(hoveredColliderDirection.Value.bonePos).z);

                    bool boneInsideCollider = hoveredColliderDirection.Key.IsSphereWithinCollider(bonePosCenter, width);
                    if (boneInsideCollider)
                    {
                        return true;
                    }

                    closestPoint = hoveredColliderDirection.Key.ClosestPoint(bonePosCenter);
                    boneCenterToColliderDirection = (closestPoint - bonePosCenter).normalized;

                    if (joint == 2)
                    {
                        // Rotate the direction forward if the contact is closer to the tip
                        jointDirection = Quaternion.AngleAxis(Mathf.InverseLerp(length, 0, Vector3.Distance(bonePosCenter, tipPosition)) * 45f, -transform.right) * -transform.up;
                    }
                    else
                    {
                        jointDirection = -transform.up;
                    }

                    switch (Finger)
                    {
                        case 0:
                            // Point the thumb closer to the index
                            jointDirection = Quaternion.AngleAxis(contactHand.handedness == Chirality.Left ? -45f : 45f, transform.forward) * jointDirection;
                            break;
                        case 1:
                            // Point the index closer to the thumb
                            jointDirection = Quaternion.AngleAxis(contactHand.handedness == Chirality.Left ? 25f : -25f, transform.forward) * jointDirection;
                            break;
                    }

                    if (Vector3.Dot(boneCenterToColliderDirection, jointDirection) > (Finger == 0 ? 0.04f : 0.18f))
                    {
                        return true;
                    }
                }
            }

            return false;
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

        private bool WillColliderAffectBone(Collider collider)
        {
            if (contactHand.physicsHandsManager.InterHandCollisions && collider.gameObject.TryGetComponent<ContactBone>(out var bone)/* && collider.attachedRigidbody.TryGetComponent<PhysicsIgnoreHelpers>(out var temp) && temp.IsThisHandIgnored(this)*/)
            {
                return false;
            }
            if (collider.attachedRigidbody != null || collider != null)
            {
                return true;
            }
            return false;
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