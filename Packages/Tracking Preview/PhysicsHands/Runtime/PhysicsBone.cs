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
        [SerializeField, Tooltip("The distance between the edge of the bone collider and the nearest object. Will report float.MaxValue if IsHovering is false.")]
        private float _objectDistance = float.MaxValue;

        /// <summary>
        /// This value will increase the further away from the palm the joint is.
        /// E.g. the finger tips will always be a significantly higher value than the knuckles
        /// </summary>
        [field: SerializeField, Tooltip("This value will increase the further away from the palm the joint is. E.g. the finger tips will always be a significantly higher value than the knuckles")]
        public float DisplacementAmount { get; private set; } = 0;
        public float DisplacementDistance => _displacementDistance;
        public float DisplacementRotation => _displacementRotation;
        private float _displacementDistance = 0f;
        private float _displacementRotation = 0f;

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

        // Computed so that we can move the grab positions/directions back a bit to improve coverage
        private Vector3 _grabDirection, _grabDirectionCenter, _grabPositionCenter, _grabPositionBase;
        #endregion

        public Collider Collider => _collider;
        private Collider _collider;
        private BoxCollider _palmCollider;
        private CapsuleCollider _jointCollider;

        // Used to visualise the distance points between bones and colliders
        private Vector3 _debugDistanceA, _debugDistanceB;

        public void Awake()
        {
#if UNITY_2021_3_OR_NEWER
            _hand = GetComponentInParent<PhysicsHand>(true);
#else
            _hand = GetComponentInParent<PhysicsHand>();
#endif
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
            // Ensure we have the hand
            if (_hand == null)
            {
#if UNITY_2021_3_OR_NEWER
                _hand = GetComponentInParent<PhysicsHand>(true);
#else
                _hand = GetComponentInParent<PhysicsHand>();
#endif
            }
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
#if UNITY_2021_3_OR_NEWER
                _hand = GetComponentInParent<PhysicsHand>(true);
#else
                _hand = GetComponentInParent<PhysicsHand>();
#endif
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
            IsBoneGrabbing = _grabbedObjects.Count > 0;
        }

        internal void RemoveGrabbing(Rigidbody rigid)
        {
            _grabbedObjects.Remove(rigid);
            IsBoneGrabbing = _grabbedObjects.Count > 0;
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
                _grabPositionBase = JointBase - (-transform.up * (JointHalfExtents.y * 0.25f));
                _grabPositionCenter = JointCenter - (transform.up * (JointHalfExtents.y * 0.25f));
            }
            else
            {
                PhysExts.ToWorldSpaceCapsule(_jointCollider, out Vector3 tip, out Vector3 bottom, out float radius);
                JointBase = bottom;
                JointTip = tip;
                JointRadius = radius;
                JointDistance = Vector3.Distance(bottom, tip);
                JointCenter = (bottom + tip) / 2f;
                _grabPositionBase = JointBase - (-transform.up * (JointRadius * 0.25f));
                _grabPositionCenter = JointCenter - (transform.up * (JointRadius * 0.25f));
            }
            switch (Finger)
            {
                case 0:
                    _grabDirection = Vector3.Lerp(Hand.Handedness == Chirality.Left ? -transform.right : transform.right, -transform.up, Joint == 2 ? 0.55f : 0.75f);
                    break;
                case 1:
                    _grabDirection = Vector3.Lerp(Hand.Handedness == Chirality.Left ? transform.right : -transform.right, -transform.up, Joint == 2 ? 0.65f : 0.85f);
                    break;
                default:
                    _grabDirection = -transform.up;
                    break;
            }
            _grabDirectionCenter = Joint == 2 ? Vector3.Lerp(_grabDirection, transform.forward, Finger == 0 ? 0.45f : 0.25f) : _grabDirection;
        }

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
            foreach (var key in _hoverObjects.Keys)
            {
                _hoverObjects[key].RemoveWhere(RemoveHoverQueue);
            }

            // Add new hovers
            for (int i = 0; i < _hoverQueueCount; i++)
            {
                if (_hoverObjects.TryGetValue(_hoverQueue[i].attachedRigidbody, out var temp))
                {
                    if (!_hoverObjects[_hoverQueue[i].attachedRigidbody].Contains(_hoverQueue[i]))
                    {
                        _hoverObjects[_hoverQueue[i].attachedRigidbody].Add(_hoverQueue[i]);
                    }
                }
                else
                {
                    _hoverObjects.Add(_hoverQueue[i].attachedRigidbody, new HashSet<Collider>() { _hoverQueue[i] });
                    if (_hoverQueue[i].attachedRigidbody.TryGetComponent<IPhysicsBoneHover>(out var physicsBoneHover))
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
                if (oldRigid != null && oldRigid.TryGetComponent<IPhysicsBoneHover>(out var physicsHandGrab))
                {
                    physicsHandGrab.OnBoneHoverExit(this);
                }
            }

            _hoverQueueCount = 0;

            // Remove old contacts
            foreach (var key in _contactObjects.Keys)
            {
                _contactObjects[key].RemoveWhere(RemoveContactQueue);
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
                    if (_contactQueue[i].attachedRigidbody.TryGetComponent<PhysicsIgnoreHelpers>(out var physicsIgnoreHelpers))
                    {
                        if (physicsIgnoreHelpers.IsThisBoneIgnored(this))
                            continue;
                    }
                    _contactObjects.Add(_contactQueue[i].attachedRigidbody, new HashSet<Collider>() { _contactQueue[i] });
                    if (_contactQueue[i].attachedRigidbody.TryGetComponent<IPhysicsBoneContact>(out var physicsHandGrab))
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
                if (oldRigid != null && oldRigid.TryGetComponent<IPhysicsBoneContact>(out var physicsHandGrab))
                {
                    physicsHandGrab.OnBoneContactExit(this);
                }
            }

            _contactQueueCount = 0;

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
            if (_hoverQueue.ContainsRange(collider, _hoverQueueCount))
            {
                return false;
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

        private bool IsObjectGrabbable(HashSet<Collider> colliders)
        {
            foreach (var collider in colliders)
            {
                // Center and the base of the bone
                if (Vector3.Dot(_grabDirectionCenter, (collider.ClosestPoint(_grabPositionCenter) - _grabPositionCenter).normalized) > 0.18f || Vector3.Dot(_grabDirection, (collider.ClosestPoint(_grabPositionBase) - _grabPositionBase).normalized) > 0.2f)
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