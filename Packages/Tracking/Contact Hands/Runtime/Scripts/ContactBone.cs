using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.ContactHands
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

        internal CapsuleCollider boneCollider;
        internal BoxCollider palmCollider;
        protected Collider Collider { get { return IsPalm ? palmCollider : boneCollider; } }

        internal ContactHand contactHand;
        internal ContactParent contactParent => contactHand.contactParent;
        #endregion

        #region Physics Data

        #endregion

        #region Interaction Data
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
        }

        private Collider[] _hoverQueue = new Collider[32], _contactQueue = new Collider[32];
        private int _hoverQueueCount = 0, _contactQueueCount = 0;

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
        public Dictionary<Rigidbody, Dictionary<Collider, ClosestObjectDirection>> HoverDirections => _hoverDirections;
        private Dictionary<Rigidbody, Dictionary<Collider, ClosestObjectDirection>> _hoverDirections = new Dictionary<Rigidbody, Dictionary<Collider, ClosestObjectDirection>>();

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

        internal abstract void UpdatePalmBone(Hand hand);
        internal abstract void UpdateBone(Bone prevBone, Bone bone);

        internal abstract void PostFixedUpdateBone();


        #region Interaction Functions
        internal void QueueHoverCollider(Collider collider)
        {
            _hoverQueue[_hoverQueueCount] = collider;
            _hoverQueueCount++;
        }

        internal void QueueContactCollider(Collider collider)
        {
            _contactQueue[_contactQueueCount] = collider;
            _contactQueueCount++;
        }

        internal void ProcessColliderQueue()
        {
            // Remove old hovers
            for (int i = 0; i < _hoverObjects.Keys.Count; i++)
            {
                _hoverObjects[_hoverObjects.Keys.ElementAt(i)].RemoveWhere(RemoveHoverQueue);

                Rigidbody hoverRigidbody = _hoverObjects.Keys.ElementAt(i);

                if (_hoverObjects[hoverRigidbody].Count == 0)
                {
                    _hoverObjects.Remove(hoverRigidbody);
                    _hoverDirections.Remove(hoverRigidbody);
                    i--;
                }
            }

            // Add new hovers
            for (int i = 0; i < _hoverQueueCount; i++)
            {
                Rigidbody rb = _hoverQueue[i].attachedRigidbody;

                if (_hoverObjects.ContainsKey(rb))
                {
                    if (!_hoverObjects[rb].Contains(_hoverQueue[i]))
                    {
                        _hoverObjects[rb].Add(_hoverQueue[i]);
                    }
                }
                else
                {
                    _hoverObjects.Add(rb, new HashSet<Collider>() { _hoverQueue[i] });

                    //if (rb.TryGetComponent<IPhysicsBoneHover>(out var physicsBoneHover))
                    //{
                    //    physicsBoneHover.OnBoneHover(this);
                    //}
                }

                if (_hoverDirections.ContainsKey(rb))
                {
                    if (!_hoverDirections[rb].ContainsKey(_hoverQueue[i]))
                    {
                        _hoverDirections[rb].Add(_hoverQueue[i], new ClosestObjectDirection());
                    }
                }
                else
                {
                    _hoverDirections.Add(rb, new Dictionary<Collider, ClosestObjectDirection>());
                    _hoverDirections[rb].Add(_hoverQueue[i], new ClosestObjectDirection());
                }

            }

            _hoverQueueCount = 0;

            // Remove old contacts
            for (int i = 0; i < _contactObjects.Keys.Count; i++)
            {
                _contactObjects[_hoverObjects.Keys.ElementAt(i)].RemoveWhere(RemoveContactQueue);

                Rigidbody contactRigidbody = _contactObjects.Keys.ElementAt(i);

                if (_contactObjects[contactRigidbody].Count == 0)
                {
                    _contactObjects.Remove(contactRigidbody);
                    i--;
                }
            }

            // Add new contacts
            for (int i = 0; i < _contactQueueCount; i++)
            {
                if (_contactObjects.TryGetValue(_contactQueue[i].attachedRigidbody, out var temp))
                {
                    _contactObjects[_contactQueue[i].attachedRigidbody].Add(_contactQueue[i]);
                }
                else
                {
                    // Make sure we respect ignored objects
                    //if (_contactQueue[i].attachedRigidbody.TryGetComponent<PhysicsIgnoreHelpers>(out var physicsIgnoreHelpers))
                    //{
                    //    if (physicsIgnoreHelpers.IsThisBoneIgnored(this))
                    //        continue;
                    //}
                    _contactObjects.Add(_contactQueue[i].attachedRigidbody, new HashSet<Collider>() { _contactQueue[i] });
                    //if (_contactQueue[i].attachedRigidbody.TryGetComponent<IPhysicsBoneContact>(out var physicsHandGrab))
                    //{
                    //    physicsHandGrab.OnBoneContact(this);
                    //}
                }
            }

            _contactQueueCount = 0;

            // Update our distances once all the colliders are ok for the bone
            UpdateBoneHoverDistances();

            // Remove the grabbed objects where they're no longer contacting
            _grabObjects.RemoveWhere(RemoveGrabQueue);

            foreach (var key in _contactObjects.Keys)
            {
                if (IsObjectGrabbable(key))
                {
                    // add to grabbable rb dicts
                    _grabObjects.Add(key);

                    if (_grabbableDirections.ContainsKey(key))
                    {
                        _grabbableDirections[key] = _hoverDirections[key];
                    }
                    else
                    {
                        _grabbableDirections.Add(key, _hoverDirections[key]);
                    }
                }
                else
                {
                    _grabObjects.Remove(key);
                    _grabbableDirections.Remove(key);
                }
            }

            IsBoneHovering = _hoverObjects.Count > 0;
            IsBoneContacting = _contactObjects.Count > 0;
            IsBoneReadyToGrab = _grabObjects.Count > 0;
        }

        private bool RemoveHoverQueue(Collider collider)
        {
            if (_hoverQueue.ContainsRange(collider, _hoverQueueCount))
            {
                return false;
            }

            if (collider.attachedRigidbody != null)
            {
                if (_hoverDirections.ContainsKey(collider.attachedRigidbody))
                {
                    _hoverDirections.Remove(collider.attachedRigidbody);
                }
            }
            return true;
        }

        private bool RemoveContactQueue(Collider collider)
        {
            if (_contactQueue.ContainsRange(collider, _contactQueueCount))
            {
                return false;
            }
            return true;
        }

        private bool RemoveGrabQueue(Rigidbody rigid)
        {
            if (_contactObjects.TryGetValue(rigid, out var temp))
            {
                return false;
            }
            return true;
        }

        private void UpdateBoneHoverDistances()
        {
            _objectDistance = float.MaxValue;

            float distance;
            Vector3 colliderPos, bonePos, midPoint, direction;
            foreach (var hoverPairs in _hoverObjects)
            {
                foreach (var hoveredCollider in hoverPairs.Value)
                {
                    colliderPos = hoveredCollider.ClosestPoint(Collider.transform.position);
                    bonePos = Collider.ClosestPoint(hoveredCollider.transform.position);

                    midPoint = colliderPos + (bonePos - colliderPos) / 2f;

                    colliderPos = hoveredCollider.ClosestPoint(midPoint);
                    bonePos = Collider.ClosestPoint(midPoint);


                    distance = Vector3.Distance(colliderPos, bonePos);
                    direction = (colliderPos - bonePos).normalized;

                    if (_hoverDirections[hoverPairs.Key].ContainsKey(hoveredCollider))
                    {
                        _hoverDirections[hoverPairs.Key][hoveredCollider].direction = direction;
                        _hoverDirections[hoverPairs.Key][hoveredCollider].bonePos = bonePos;
                    }
                    else
                    {
                        _hoverDirections[hoverPairs.Key].Add(
                            hoveredCollider,
                            new ClosestObjectDirection()
                            {
                                bonePos = bonePos,
                                direction = direction
                            }
                        );
                    }

                    if (distance < _objectDistance)
                    {
                        _objectDistance = distance;
                    }
                }
            }
        }

        private bool IsObjectGrabbable(Rigidbody rigidbody)
        {
            if (_hoverDirections.TryGetValue(rigidbody, out var hoveredColliders))
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

        #endregion
    }
}