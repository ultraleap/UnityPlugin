using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public abstract class ContactBone : MonoBehaviour
    {
        #region Bone Parameters
        internal int finger, joint;
        internal bool isPalm = false;
        public int Finger => finger;
        public int Bone => joint;
        public bool IsPalm => isPalm;

        internal float width = 0f, length = 0f, palmThickness = 0f;
        internal Vector3 tipPosition = Vector3.zero;
        internal Vector3 wristPosition = Vector3.zero;

        internal CapsuleCollider boneCollider;
        internal BoxCollider palmCollider;
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
        #endregion

        internal abstract void UpdatePalmBone(Hand hand);
        internal abstract void UpdateBone(Bone bone);

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
        #endregion
    }
}