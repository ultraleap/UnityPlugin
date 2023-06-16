using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    public class PhysicsBone : MonoBehaviour
    {
        public PhysicsHand Hand => _hand;
        [SerializeField, HideInInspector]
        private PhysicsHand _hand;

        public ArticulationBody ArticulationBody
        {
            get
            {
                if (_body == null)
                {
                    _body = GetComponent<ArticulationBody>();
                }
                return _body;
            }
        }

        private HashSet<Collider> _hoverQueue = new HashSet<Collider>(), _contactQueue = new HashSet<Collider>();

        private Dictionary<Rigidbody, HashSet<Collider>> _hoverObjects = new Dictionary<Rigidbody, HashSet<Collider>>();
        public Dictionary<Rigidbody, HashSet<Collider>> HoverObjects => _hoverObjects;

        public bool IsBoneHovering { get; private set; } = false;
        public bool IsBoneHoveringRigid(Rigidbody rigid)
        {
            return _hoverObjects.TryGetValue(rigid, out HashSet<Collider> result);
        }

        private Dictionary<Rigidbody, HashSet<Collider>> _contactObjects = new Dictionary<Rigidbody, HashSet<Collider>>();
        public Dictionary<Rigidbody, HashSet<Collider>> ContactObjects => _contactObjects;
        public bool IsBoneContacting { get; private set; } = false;

        public bool IsBoneContactingRigid(Rigidbody rigid)
        {
            return _contactObjects.TryGetValue(rigid, out HashSet<Collider> result);
        }

        private HashSet<Rigidbody> _grabObjects = new HashSet<Rigidbody>();
        /// <summary>
        /// Objects that *can* be grabbed, not ones that are
        /// </summary>
        public HashSet<Rigidbody> GrabbableObjects => _grabObjects;
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
        public bool IsGrabbing { get; private set; } = false;

        [SerializeField, HideInInspector]
        private ArticulationBody _body;

        private float _origXDriveLower = float.MinValue, _origXDriveUpper = float.MaxValue;
        public float OriginalXDriveLower => _origXDriveLower;
        public float OriginalXDriveUpper => _origXDriveUpper;

        public int Finger => _finger;
        [SerializeField, HideInInspector]
        private int _finger;

        public int Joint => _joint;
        [SerializeField, HideInInspector]
        private int _joint;

        /// <summary>
        /// Will report float.MaxValue if IsHovering is false
        /// </summary>
        public float ObjectDistance => _objectDistance;
        private float _objectDistance = float.MaxValue;

        /// <summary>
        /// This value will increase the further away from the palm the joint is.
        /// E.g. the finger tips will always be a significantly higher value than the knuckles
        /// </summary>
        public float DisplacementAmount { get; private set; } = 0;
        public float DisplacementDistance => _displacementDistance;
        public float DisplacementRotation => _displacementRotation;
        private float _displacementDistance = 0f, _displacementRotation = 0f;

        #region World Space Joint Info
        // Computed once so that it can be re-used

        public Vector3 JointCenter { get; private set; }
        public Vector3 JointBase { get; private set; }
        public Vector3 JointTip { get; private set; }
        /// <summary>
        /// When referencing palm, this will be the distance from the wrist to the knuckles
        /// </summary>
        public float JointDistance { get; private set; } = 0;
        /// <summary>
        /// Will not report any values for the palm
        /// </summary>
        public float JointRadius { get; private set; } = 0;
        /// <summary>
        /// Will only report values for the palm
        /// </summary>
        public Vector3 JointHalfExtents { get; private set; }
        #endregion

        public Collider Collider => _collider;
        private Collider _collider;
        private BoxCollider _palmCollider;
        private CapsuleCollider _jointCollider;

        // Used to visualise the distance points between bones and colliders
        private Vector3 _debugDistanceA, _debugDistanceB;

        public void Awake()
        {
            _hand = GetComponentInParent<PhysicsHand>();
            _body = GetComponent<ArticulationBody>();
            _collider = GetComponent<Collider>();
            if (_collider.GetType() == typeof(BoxCollider))
            {
                _palmCollider = (BoxCollider)_collider;
            }
            else
            {
                _jointCollider = (CapsuleCollider)_collider;
            }
        }

        private void OnEnable()
        {
            ResetValues();
        }

        private void OnDisable()
        {
            ResetValues();
        }

        private void ResetValues()
        {
            _objectDistance = float.MaxValue;
            DisplacementAmount = 0f;
            _displacementDistance = 0f;
            _displacementRotation = 0f;
        }

        public void SetBoneIndexes(int finger, int joint)
        {
            _finger = finger;
            _joint = joint;

            if (_body == null)
            {
                _body = GetComponent<ArticulationBody>();
            }
            if (_hand == null)
            {
                _hand = GetComponentInParent<PhysicsHand>(true);
            }
            if (_body != null)
            {
                _origXDriveLower = _body.xDrive.lowerLimit;
                _origXDriveUpper = _body.xDrive.upperLimit;
            }
        }

        internal void AddGrabbing(Rigidbody rigid)
        {
            _grabbedObjects.Add(rigid);
            IsGrabbing = _grabbedObjects.Count > 0;
        }

        internal void RemoveGrabbing(Rigidbody rigid)
        {
            _grabbedObjects.Remove(rigid);
            IsGrabbing = _grabbedObjects.Count > 0;
        }

        internal void UpdateBoneWorldSpace()
        {
            if (Finger == 5)
            {
                PhysExts.ToWorldSpaceBox(_palmCollider, out Vector3 center, out Vector3 halfExtents, out Quaternion orientation);
                JointCenter = center;
                JointHalfExtents = halfExtents;
                JointBase = center + (orientation * (Vector3.back * halfExtents.z));
                JointTip = center + (orientation * (Vector3.forward * halfExtents.z));
                JointDistance = Vector3.Distance(JointBase, JointTip);
            }
            else
            {
                PhysExts.ToWorldSpaceCapsule(_jointCollider, out Vector3 tip, out Vector3 bottom, out float radius);
                JointBase = bottom;
                JointTip = tip;
                JointRadius = radius;
                JointDistance = Vector3.Distance(bottom, tip);
                JointCenter = (bottom + tip) / 2f;
            }
        }

        internal void QueueHoverCollider(Collider collider)
        {
            _hoverQueue.Add(collider);
        }

        internal void QueueContactCollider(Collider collider)
        {
            _contactQueue.Add(collider);
        }

        internal void ProcessColliderQueue()
        {
            // Remove old hovers
            foreach (var key in _hoverObjects.Keys)
            {
                _hoverObjects[key].RemoveWhere(RemoveHoverQueue);
            }

            // Add new hovers
            foreach (var newHover in _hoverQueue)
            {
                if (_hoverObjects.TryGetValue(newHover.attachedRigidbody, out var temp))
                {
                    if (!_hoverObjects[newHover.attachedRigidbody].Contains(newHover))
                    {
                        _hoverObjects[newHover.attachedRigidbody].Add(newHover);
                    }
                }
                else
                {
                    _hoverObjects.Add(newHover.attachedRigidbody, new HashSet<Collider>() { newHover });
                    if (newHover.attachedRigidbody.TryGetComponent<IPhysicsBoneHover>(out var physicsBoneHover))
                    {
                        physicsBoneHover.OnBoneHover(this);
                    }
                }
            }

            // Remove empty entries
            var badKeys = _hoverObjects.Where(pair => pair.Value.Count == 0)
                        .Select(pair => pair.Key)
                        .ToList();
            foreach (var oldRigid in badKeys)
            {
                _hoverObjects.Remove(oldRigid);
                if (oldRigid.TryGetComponent<IPhysicsBoneHover>(out var physicsHandGrab))
                {
                    physicsHandGrab.OnBoneHoverExit(this);
                }
            }

            _hoverQueue = new HashSet<Collider>();

            // Remove old contacts
            foreach (var key in _contactObjects.Keys)
            {
                _contactObjects[key].RemoveWhere(RemoveContactQueue);
            }

            // Add new contacts
            foreach (var newContact in _contactQueue)
            {
                if (_contactObjects.TryGetValue(newContact.attachedRigidbody, out var temp))
                {
                    _contactObjects[newContact.attachedRigidbody].Add(newContact);
                }
                else
                {
                    _contactObjects.Add(newContact.attachedRigidbody, new HashSet<Collider>() { newContact });
                    if (newContact.attachedRigidbody.TryGetComponent<IPhysicsBoneContact>(out var physicsHandGrab))
                    {
                        physicsHandGrab.OnBoneContact(this);
                    }
                }
            }

            // Remove empty entries
            badKeys = _contactObjects.Where(pair => pair.Value.Count == 0)
                        .Select(pair => pair.Key)
                        .ToList();
            foreach (var oldRigid in badKeys)
            {
                _contactObjects.Remove(oldRigid);
                if (oldRigid.TryGetComponent<IPhysicsBoneContact>(out var physicsHandGrab))
                {
                    physicsHandGrab.OnBoneContactExit(this);
                }
            }

            _contactQueue = new HashSet<Collider>();

            // Remove the grabbed objects where they're no longer contacting
            _grabObjects.RemoveWhere(RemoveGrabQueue);

            foreach (var key in _contactObjects.Keys)
            {
                if (IsObjectGrabbable(_contactObjects[key]))
                {
                    _grabObjects.Add(key);
                }
                else
                {
                    _grabObjects.Remove(key);
                }
            }

            IsBoneHovering = _hoverObjects.Count > 0;
            IsBoneContacting = _contactObjects.Count > 0;
            IsBoneReadyToGrab = _grabObjects.Count > 0;

            // Update our distances once all the colliders are ok for the bone
            UpdateBoneHoverDistances();
        }

        private bool RemoveHoverQueue(Collider collider)
        {
            if (_hoverQueue.Contains(collider))
            {
                return false;
            }
            return true;
        }

        private bool RemoveContactQueue(Collider collider)
        {
            if (_contactQueue.Contains(collider))
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

        private bool IsObjectGrabbable(HashSet<Collider> colliders)
        {
            foreach (var collider in colliders)
            {
                // Center and the base of the bone
                if (Vector3.Dot(-transform.up, (collider.ClosestPoint(JointCenter) - JointCenter).normalized) > 0.25f || Vector3.Dot(-transform.up, (collider.ClosestPoint(transform.position) - transform.position).normalized) > 0.25f)
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateBoneHoverDistances()
        {
            _objectDistance = float.MaxValue;

            float tempDist;
            Vector3 tempA, tempB, tempMid;
            foreach (var hoverPairs in _hoverObjects)
            {
                foreach (var collider in hoverPairs.Value)
                {
                    tempA = collider.ClosestPoint(_collider.transform.position);
                    tempB = _collider.ClosestPoint(collider.transform.position);
                    tempMid = tempA + (tempB - tempA) / 2f;
                    tempA = collider.ClosestPoint(tempMid);
                    tempB = _collider.ClosestPoint(tempMid);

                    tempDist = Vector3.Distance(tempA, tempB);
                    if (tempDist < _objectDistance)
                    {
                        _objectDistance = tempDist;
                        _debugDistanceA = tempA;
                        _debugDistanceB = tempB;
                    }
                }
            }
        }

        internal void UpdateBoneDisplacement(Bone bone = null)
        {
            _displacementDistance = 0f;
            _displacementRotation = 0f;

            if (bone == null && Finger != 5)
            {
                return;
            }

            Vector3 bonePos, position;
            // Palm
            if (Finger == 5)
            {
                bonePos = _hand.GetOriginalLeapHand().PalmPosition;
                position = transform.position;
                _displacementRotation = Quaternion.Angle(transform.rotation, _hand.GetOriginalLeapHand().Rotation);
            }
            // Fingers
            else
            {
                bonePos = bone.NextJoint;
                CapsuleCollider capsule = (CapsuleCollider)_collider;
                capsule.ToWorldSpaceCapsule(out Vector3 tip, out Vector3 temp, out float rad);
                position = tip;

                if (_body.dofCount > 0)
                {
                    _displacementRotation = Mathf.Abs(_body.xDrive.target - _body.jointPosition[0] * Mathf.Rad2Deg);
                }
            }

            _displacementDistance = Vector3.Distance(position, bonePos);

            // We want the rotation displacement to be more powerful than the distance
            DisplacementAmount = ((Mathf.InverseLerp(0.01f, _hand.Provider.HandTeleportDistance, _displacementDistance) * 0.75f) + (Mathf.InverseLerp(5f, 35f, _displacementRotation) * 1.25f)) * (1 + (Joint * 0.5f));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_hand.ShowDistanceVisualisations)
            {
                Gizmos.color = Color.cyan;
                if (IsBoneHovering)
                {
                    Gizmos.DrawLine(_debugDistanceA, _debugDistanceB);
                    Gizmos.DrawSphere(_debugDistanceA, JointRadius / 2f);
                }
            }
        }
#endif
    }
}