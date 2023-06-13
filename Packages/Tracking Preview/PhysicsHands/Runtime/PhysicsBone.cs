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

        private HashSet<Rigidbody> _grabRigids = new HashSet<Rigidbody>();

        private HashSet<Collider> _hoverQueue = new HashSet<Collider>(), _contactQueue = new HashSet<Collider>();

        private Dictionary<Rigidbody, HashSet<Collider>> _hoverObjects = new Dictionary<Rigidbody, HashSet<Collider>>();
        public Dictionary<Rigidbody, HashSet<Collider>> HoverObjects => _hoverObjects;

        public bool IsBoneHovering { get; private set; } = false;
        public bool IsBoneHoveringRigid(Rigidbody rigid)
        {
            return _hoverObjects.TryGetValue(rigid, out HashSet<Collider> result);
        }

        private Dictionary<Rigidbody, HashSet<Collider>> _contactObjects2 = new Dictionary<Rigidbody, HashSet<Collider>>();
        public Dictionary<Rigidbody, HashSet<Collider>> ContactObjects => _contactObjects2;
        public bool IsBoneContacting { get; private set; } = false;

        public bool IsBoneContactingRigid(Rigidbody rigid)
        {
            return _contactObjects2.TryGetValue(rigid, out HashSet<Collider> result);
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
        /// Whether the grasp helper has reported that this bone is grabbing
        /// </summary>
        public bool IsGrabbing { get; private set; } = false;

        public HashSet<Rigidbody> ContactingObjects => _contactObjects;
        private HashSet<Rigidbody> _contactObjects = new HashSet<Rigidbody>();
        public HashSet<Rigidbody> GraspingObjects => _graspingObjects;
        private HashSet<Rigidbody> _graspingObjects = new HashSet<Rigidbody>();

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
        /// Reports whether any object is near to the bone
        /// </summary>
        public bool IsObjectNearBone { get; private set; } = false;

        /// <summary>
        /// Will report float.MaxValue if IsObjectNearBone is false
        /// </summary>
        public float ObjectDistance => _objectDistance;
        /// <summary>
        /// Will report float.MaxValue if IsGraspedObjectNearBone is false
        /// </summary>
        public float GraspedObjectDistance => _graspedObjectDistance;
        private float _objectDistance = -1f, _graspedObjectDistance = -1f;

        /// <summary>
        /// This value will increase the further away from the palm the joint is.
        /// E.g. the finger tips will always be a significantly higher value than the knuckles
        /// </summary>
        public float DisplacementAmount { get; private set; } = 0;
        public float DisplacementDistance => _displacementDistance;
        public float DisplacementRotation => _displacementRotation;
        private float _displacementDistance = 0f, _displacementRotation = 0f;

        private int _colliderCount = 0;
        private Vector3 _center, _direction;
        public Collider Collider => _collider;
        private Collider _collider;
        private BoxCollider _palmCollider;
        private CapsuleCollider _jointCollider;

        private List<float> _distanceCache = new List<float>();

        private RaycastHit[] _rayCache = new RaycastHit[6];
        private Ray _graspRay = new Ray();

        

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
            IsObjectNearBone = false;
            _objectDistance = 0f;
            _graspedObjectDistance = 0f;
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

        internal void UpdateBoneCenter(Vector3 center)
        {
            _center = center;
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
                    _hoverObjects[newHover.attachedRigidbody].Add(newHover);
                }
                else
                {
                    _hoverObjects.Add(newHover.attachedRigidbody, new HashSet<Collider>() { newHover });
                }
            }

            // Remove empty entries
            var badKeys = _hoverObjects.Where(pair => pair.Value.Count == 0)
                        .Select(pair => pair.Key)
                        .ToList();
            foreach (var badKey in badKeys)
            {
                _hoverObjects.Remove(badKey);
            }

            _hoverQueue = new HashSet<Collider>();

            // Remove old contacts
            foreach (var key in _contactObjects2.Keys)
            {
                _contactObjects2[key].RemoveWhere(RemoveContactQueue);
            }

            // Add new contacts
            foreach (var newContact in _contactQueue)
            {
                if (_contactObjects2.TryGetValue(newContact.attachedRigidbody, out var temp))
                {
                    _contactObjects2[newContact.attachedRigidbody].Add(newContact);
                }
                else
                {
                    _contactObjects2.Add(newContact.attachedRigidbody, new HashSet<Collider>() { newContact });
                }
            }

            // Remove empty entries
            badKeys = _contactObjects2.Where(pair => pair.Value.Count == 0)
                        .Select(pair => pair.Key)
                        .ToList();
            foreach (var badKey in badKeys)
            {
                _contactObjects2.Remove(badKey);
            }

            _contactQueue = new HashSet<Collider>();

            // Remove the grabbed objects where they're no longer contacting
            _grabObjects.RemoveWhere(RemoveGrabQueue);

            foreach (var key in _contactObjects2.Keys)
            {
                if (IsObjectGrabbable(_contactObjects2[key]))
                {
                    _grabObjects.Add(key);
                }
                else
                {
                    _grabObjects.Remove(key);
                }
            }

            IsBoneHovering = _hoverObjects.Count > 0;
            IsBoneContacting = _contactObjects2.Count > 0;
            IsBoneReadyToGrab = _grabObjects.Count > 0;
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
            if (_contactObjects2.TryGetValue(rigid, out var temp))
            {
                return false;
            }
            return true;
        }

        private bool IsObjectGrabbable(HashSet<Collider> colliders)
        {
            foreach (var collider in colliders)
            {
                if (Vector3.Dot(-transform.up, (collider.ClosestPoint(_center) - _center).normalized) > 0.25f || Vector3.Dot(-transform.up, (collider.ClosestPoint(transform.position) - transform.position).normalized) > 0.25f)
                {
                    return true;
                }
            }
            return false;
        }

        internal void UpdateBoneHeuristics(Bone bone, ref Collider[] colliderCache, ref bool[] foundCache)
        {
            if (_palmCollider == null)
            {
                UpdateJointOverlaps(ref colliderCache);
            }
            else
            {
                UpdatePalmOverlaps(ref colliderCache);
            }
            UpdateOverlappedObjects(ref colliderCache, ref foundCache);
            UpdateObjectDirections();
            UpdateBoneDisplacement(bone);
            UpdateBoneDistances();
        }

        private void UpdatePalmOverlaps(ref Collider[] colliderCache)
        {
            _colliderCount = PhysExts.OverlapBoxNonAllocOffset(_palmCollider, Vector3.zero, colliderCache, _hand.Provider.InteractionMask, QueryTriggerInteraction.Ignore, _hand.GetPhysicsHand().contactDistance);
            PhysExts.ToWorldSpaceBox(_palmCollider, out _center, out var extents, out var orient);
        }

        private void UpdateJointOverlaps(ref Collider[] colliderCache)
        {
            _colliderCount = PhysExts.OverlapCapsuleNonAllocOffset(_jointCollider, Vector3.zero, colliderCache, _hand.Provider.InteractionMask, QueryTriggerInteraction.Ignore, _hand.GetPhysicsHand().contactDistance);
            PhysExts.ToWorldSpaceCapsule(_jointCollider, out var point0, out var point1, out var radius);
            _center = (point0 + point1) / 2f;
        }

        private void UpdateOverlappedObjects(ref Collider[] colliderCache, ref bool[] foundCache)
        {
            //for (int i = 0; i < _colliderCount; i++)
            //{
            //    if (colliderCache[i].attachedRigidbody != null)
            //    {
            //        _contactColliders.Add(colliderCache[i]);
            //        foundCache[i] = true;
            //    }
            //    else
            //    {
            //        foundCache[i] = false;
            //    }
            //}
            //for (int i = 0; i < _colliderCount; i++)
            //{
            //    if (!foundCache[i])
            //    {
            //        _contactColliders.Remove(colliderCache[i]);
            //        _grabRigids.Remove(colliderCache[i].attachedRigidbody);
            //    }
            //}
        }

        private void UpdateObjectDirections()
        {
            //foreach (var collider in _contactColliders)
            //{
            //    if (Vector3.Dot(-transform.up, (collider.ClosestPointOnBounds(_center) - _center).normalized) > 0.25f)
            //    {
            //        _grabRigids.Add(collider.attachedRigidbody);
            //    }
            //    else
            //    {
            //        _grabRigids.Remove(collider.attachedRigidbody);
            //    }
            //}
        }

        private void UpdateBoneDistances()
        {
            _objectDistance = float.MaxValue;
            _graspedObjectDistance = float.MaxValue;
            IsObjectNearBone = false;

            float height, radius;

            if (Finger == 5)
            {
                BoxCollider palm = (BoxCollider)_collider;
                height = palm.size.z;
                radius = palm.size.y / 2f;
            }
            else
            {
                CapsuleCollider capsule = (CapsuleCollider)_collider;
                height = capsule.height;
                radius = capsule.radius;
            }

            // Move forward from center by 25% of the finger so that we're not off the very tip
            Vector3 position = _collider.bounds.center + transform.rotation * new Vector3(0, 0, height * .25f);

            // If we're on the thumb we want to tilt slightly towards the other fingers
            Vector3 upDirection = Finger == 0 ? Vector3.Lerp(-transform.up, _hand.Handedness == Chirality.Left ? -transform.right : transform.right, 0.3f) :
                Vector3.Lerp(-transform.up, _hand.Handedness == Chirality.Left ? transform.right : -transform.right, 0.1f);

            CalculateJointDistance(position, upDirection, radius, height * 2f, true);

            if (Joint == 2)
            {
                // Run a second test pointing forward a bit for the tip
                CalculateJointDistance(position + transform.rotation * new Vector3(0, radius * .2f, height * .15f), Vector3.Lerp(upDirection, transform.forward, 0.6f), radius, height * 2f);
                // Add a third one for pointing out from the tip to improve button detection
                CalculateJointDistance(position + transform.rotation * new Vector3(0, radius * .2f, height * .15f), transform.forward, radius, height * 0.5f);
            }

            if (Joint > 0)
            {
                // Second set of distance checks closer to the center of the bone
                CalculateJointDistance(_collider.bounds.center + transform.rotation * new Vector3(0, 0, -height * .15f), upDirection, radius, height * 2f);
            }

            if (Finger == 0)
            {
                // Thumb distances closer towards the object, pointing down a bit
                Vector3 direction = Vector3.Lerp(-transform.up, _hand.Handedness == Chirality.Left ? transform.right : -transform.right, 0.6f);
                CalculateJointDistance(position, direction, radius, height * 2f, debug: true);
            }
        }

        private void CalculateJointDistance(Vector3 origin, Vector3 direction, float radius, float length, bool forceUpdate = false, bool debug = false)
        {
            DistanceCalculation(origin, direction, radius, length, out bool anyFound, out float objectDistance, out float graspDistance);

            if (anyFound)
            {
                IsObjectNearBone = true;
                if (objectDistance < _objectDistance || forceUpdate)
                {
                    _objectDistance = objectDistance;
                }
                Debug.DrawLine(origin, origin + (direction * _objectDistance), Color.yellow, Time.fixedDeltaTime);
            }
        }

        internal void UpdateBoneDistances(float newDistance)
        {
            _distanceCache.Add(newDistance);
        }

        internal void ProcessBoneDistances()
        {
            _objectDistance = 0f;
            if(_distanceCache.Count > 0)
            {
                IsObjectNearBone = true;
                _objectDistance = _distanceCache.Min();
            }
            else
            {
                IsObjectNearBone = false;
            }
            _distanceCache.Clear();
        }

        private void DistanceCalculation(Vector3 origin, Vector3 direction, float radius, float length, out bool anyFound, out float anyDistance, out float graspDistance)
        {
            anyDistance = 0f;
            graspDistance = 0f;

            _graspRay.origin = origin;
            _graspRay.direction = direction;

            // Using a sphere cast here will result in more reliable contact checks
            int cols = Physics.SphereCastNonAlloc(_graspRay, radius, _rayCache, length, _hand.Provider.InteractionMask, QueryTriggerInteraction.Ignore);

            anyFound = false;

            for (int i = 0; i < cols; i++)
            {
                if (!anyFound)
                {
                    anyDistance = _rayCache[i].distance;
                    anyFound = true;
                }
                else if (_rayCache[i].distance < anyDistance)
                {
                    anyDistance = _rayCache[i].distance;
                }
            }
        }

    }
}