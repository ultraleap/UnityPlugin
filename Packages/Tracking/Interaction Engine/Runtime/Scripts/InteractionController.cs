/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Interaction.Internal.InteractionEngineUtility;
using Leap.Unity.Attributes;

using Leap.Unity.RuntimeGizmos;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction
{
    [Serializable]
    public class InteractionControllerSet : SerializableHashSet<InteractionController> { }

    /// <summary>
    /// Specified on a per-object basis to allow Interaction objects
    /// to ignore hover for the left hand, right hand, or both hands.
    /// </summary>
    public enum IgnoreHoverMode { None, Left, Right, Both }

    /// <summary>
    /// The Interaction Engine can be controlled by hands tracked by the
    /// Leap Motion Controller, or by remote-style held controllers
    /// such as the Oculus Touch or Vive controller.
    /// </summary>
    public enum ControllerType { Hand, XRController }

    /// <summary>
    /// Defines an abstract class with a set of actions the interaction engine is capable of raising, InteractionHand and InteractionXRController inherit from this.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class InteractionController : MonoBehaviour,
                                                  IInternalInteractionController
    {

        #region Inspector

        [Tooltip("The manager responsible for this interaction controller. Interaction "
               + "controllers should be children of their interaction manager.")]
        public InteractionManager manager;

        [Header("Interaction Types")]

        [Tooltip("If disabled, this interaction controller will not be used to generate "
               + "hover information or primary hover information. Warning: Primary hover "
               + "data is required for Interaction Engine user interface components like "
               + "InteractionButton and InteractionSlider to function, so this controller "
               + "won't able to interact with UI components.")]
        [SerializeField]
        [OnEditorChange("hoverEnabled")]
        private bool _hoverEnabled = true;
        /// <summary>
        /// If disabled, this interaction controller will not be used to generate hover information or primary hover information. Warning: Primary hover data is required for Interaction Engine user interface components like InteractionButton and InteractionSlider to function, so this controller won't able to interact with UI components.
        /// </summary>
        public bool hoverEnabled
        {
            get { return _hoverEnabled; }
            set
            {
                _hoverEnabled = value;

                if (!_hoverEnabled)
                {
                    ClearHoverTracking();
                }
            }
        }

        [Tooltip("If disabled, this interaction controller will not collide with interaction "
               + "objects and objects will not receive contact callbacks.")]
        [SerializeField]
        [OnEditorChange("contactEnabled")]
        private bool _contactEnabled = true;
        /// <summary>
        /// If disabled, this interaction controller will not collide with interaction objects and objects will not receive contact callbacks.
        /// </summary>
        public bool contactEnabled
        {
            get { return _contactEnabled; }
            set
            {
                _contactEnabled = value;

                if (!_contactEnabled)
                {
                    disableContactBoneCollision();

                    ClearContactTracking();
                }
                else
                {
                    resetContactBonePose();

                    EnableSoftContact();
                }
            }
        }

        [Tooltip("If disabled, this interaction controller will not be able to grasp "
               + "interaction objects.")]
        [SerializeField]
        [OnEditorChange("graspingEnabled")]
        private bool _graspingEnabled = true;

        /// <summary>
        /// If disabled, this interaction controller will not be able to grasp interaction objects.
        /// </summary>
        public bool graspingEnabled
        {
            get { return _graspingEnabled; }
            set
            {
                _graspingEnabled = value;

                if (!_graspingEnabled)
                {
                    EnableSoftContact();
                    ReleaseGrasp();
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets whether the underlying object (Leap hand or a held controller) is currently
        /// in a tracked state. Objects grasped by a controller that becomes untracked will
        /// become "suspended" and receive specific suspension callbacks. (Implementing any
        /// behaviour during the suspension state is left up to the developer, however.)
        /// </summary>
        public abstract bool isTracked { get; }

        /// <summary>
        /// Gets whether the underlying object (Leap hand or a held controller) is currently
        /// being moved or being actively manipulated by the player.
        /// </summary>
        public abstract bool isBeingMoved { get; }

        /// <summary>
        /// Gets whether the underlying object (Leap hand or a held controller) represents
        /// or is held by a left hand (true) or a right hand (false).
        /// </summary>
        public abstract bool isLeft { get; }

        /// <summary>
        /// Gets whether the underlying object (Leap hand or a held controller) represents
        /// or is held by a right hand (true) or a left hand (false).
        /// </summary>
        public bool isRight { get { return !isLeft; } }

        /// <summary>
        /// Returns the current position of this controller.
        /// </summary>
        public abstract Vector3 position { get; }

        /// <summary>
        /// Returns the current rotation of this controller.
        /// </summary>
        public abstract Quaternion rotation { get; }

        /// <summary>
        /// Returns the current velocity of this controller.
        /// </summary>
        public abstract Vector3 velocity { get; }

        /// <summary>
        /// Gets the type of controller this object represents underneath the
        /// InteractionController abstraction. If the type is ControllerType.Hand, the
        /// intHand property will contain the InteractionHand object this object abstracts
        /// from.
        /// </summary>
        public abstract ControllerType controllerType { get; }

        /// <summary>
        /// If this InteractionController's controllerType is ControllerType.Hand,
        /// this gets the InteractionHand, otherwise this returns null.
        /// </summary>
        public abstract InteractionHand intHand { get; }

        /// <summary>
        /// Contact requires knowledge of the controller's scale. Non-uniformly scaled
        /// controllers are NOT supported.
        /// </summary>
        public float scale { get { return this.transform.lossyScale.x; } }

        #endregion

        #region Events

        /// <summary>
        /// Called when this InteractionController begins primarily hovering over an InteractionBehaviour.
        /// If the controller transitions to primarily hovering a new object, OnEndPrimaryHoveringObject will
        /// first be called on the old object, then OnBeginPrimaryHoveringObject will be called for
        /// the new object.
        /// </summary>
        public Action<InteractionBehaviour> OnBeginPrimaryHoveringObject = (intObj) => { };

        /// <summary>
        /// Called when this InteractionController stops primarily hovering over an InteractionBehaviour.
        /// If the controller transitions to primarily-hovering a new object, OnEndPrimaryHoveringObject will
        /// first be called on the old object, then OnBeginPrimaryHoveringObject will be called for
        /// the new object.
        /// </summary>
        public Action<InteractionBehaviour> OnEndPrimaryHoveringObject = (intObj) => { };

        /// <summary>
        /// Called every (fixed) frame this InteractionController is primarily hovering over an InteractionBehaviour.
        /// </summary>
        public Action<InteractionBehaviour> OnStayPrimaryHoveringObject = (intObj) => { };

        /// <summary>
        /// Called when the InteractionController begins grasping an object.
        /// </summary>
        public Action OnGraspBegin = () => { };

        /// <summary>
        /// Called while the InteractionController is grasping an object.
        /// </summary>
        public Action OnGraspStay = () => { };

        /// <summary>
        /// Called when the InteractionController releases an object.
        /// </summary>
        public Action OnGraspEnd = () => { };

        /// <summary>
        /// Called when contact data is initialized.
        /// </summary>
        public Action<InteractionController> OnContactInitialized = (intCtrl) => { };

        #endregion

        #region Unity Events

        protected virtual void Reset()
        {
            if (manager == null) manager = GetComponentInParent<InteractionManager>();
        }

        protected virtual void OnEnable()
        {
            if (_contactInitialized)
            {
                EnableSoftContact();

                resetContactBonePose();
            }
        }

        protected virtual void Start()
        {
            if (manager == null) manager = InteractionManager.instance;
        }

        protected virtual void OnDisable()
        {
            if (_contactInitialized)
            {
                EnableSoftContact();
            }
            ReleaseGrasp();

            ClearHoverTracking();
            ClearPrimaryHoverTracking();
            ClearContactTracking();
        }

        #endregion

        // A list of InteractionControllers for use as a temporary buffer.
        private List<InteractionController> _controllerListBuffer = new List<InteractionController>();

        /// <summary>
        /// Called by the InteractionManager every fixed (physics) frame to populate the
        /// Interaction Hand with state from the Leap hand and perform bookkeeping operations.
        /// </summary>
        void IInternalInteractionController.FixedUpdateController()
        {
            fixedUpdateController();

            if (hoverEnabled) fixedUpdateHovering();
            if (contactEnabled) fixedUpdateContact();
            if (graspingEnabled) fixedUpdateGrasping();
        }

        /// <summary>
        /// Unregister an interaction Behaviour from the hover and contact tracking for an object
        /// </summary>
        public void NotifyObjectUnregistered(IInteractionBehaviour intObj)
        {
            ClearHoverTrackingForObject(intObj);
            ClearContactTrackingForObject(intObj);

            onObjectUnregistered(intObj);
        }

        /// <summary>
        /// This method is called by the InteractionController when it is notified by the
        /// InteractionManager that an InteractionBehaviour has been unregistered from the
        /// Interaction Engine. If your controller has any state that remembers or tracks
        /// interaction objects, this method should clear that state, because unregistered
        /// objects won't receive state updates or callbacks from this controller's
        /// Interaction Manager anymore.
        /// </summary>
        protected abstract void onObjectUnregistered(IInteractionBehaviour intObj);

        /// <summary>
        /// Called just before the InteractionController proceeds with its usual FixedUpdate.
        ///
        /// It's generally better to override this method instead of having your
        /// InteractionController implement FixedUpdate because its execution order relative
        /// to the Interaction Manager is fixed.
        /// </summary>
        protected virtual void fixedUpdateController() { }

        #region Hovering

        /// <summary>
        /// In addition to standard hover validity checks, you can set this filter property
        /// to further filter objects for hover consideration. Only objects for which this
        /// function returns true will be hover candidates (if the filter is not null).
        /// </summary>
        public Func<IInteractionBehaviour, bool> customHoverActivityFilter = null;

        // Hover Activity Filter
        private Func<Collider, IInteractionBehaviour> hoverActivityFilter;
        private IInteractionBehaviour hoverFilterFunc(Collider collider)
        {
            Rigidbody rigidbody = collider.attachedRigidbody;
            IInteractionBehaviour intObj = null;

            bool objectValidForHover = rigidbody != null
                                      && manager.interactionObjectBodies.TryGetValue(rigidbody, out intObj)
                                      && !intObj.ShouldIgnoreHover(this)
                                      && (customHoverActivityFilter == null || customHoverActivityFilter(intObj));

            if (objectValidForHover) return intObj;
            else return null;
        }

        // Layer mask for the hover acitivity manager.
        private Func<int> hoverLayerMaskAccessor;

        // Hover Activity Manager
        private ActivityManager<IInteractionBehaviour> _hoverActivityManager;

        /// <summary>
        /// Get the _hoverActivityManager OR make a new one if it does not exist
        /// </summary>
        public ActivityManager<IInteractionBehaviour> hoverActivityManager
        {
            get
            {
                if (_hoverActivityManager == null)
                {
                    if (hoverActivityFilter == null) hoverActivityFilter = hoverFilterFunc;
                    if (hoverLayerMaskAccessor == null) hoverLayerMaskAccessor = manager.GetInteractionLayerMask;

                    _hoverActivityManager = new ActivityManager<IInteractionBehaviour>(manager.hoverActivationRadius,
                                                                                       hoverActivityFilter);

                    _hoverActivityManager.activationLayerFunction = hoverLayerMaskAccessor;
                }
                return _hoverActivityManager;
            }
        }

        /// <summary>
        /// Disables broadphase checks if an object is currently interacting with this hand.
        /// </summary>
        private bool _primaryHoverLocked = false;
        /// <summary>
        /// When set to true, locks the current primarily hovered object, even if the hand
        /// gets closer to a different object.
        /// </summary>
        public bool primaryHoverLocked
        {
            get { return _primaryHoverLocked; }
            set { _primaryHoverLocked = value; }
        }

        /// <summary>
        /// Sets the argument interaction object to be the current primary hover of this
        /// interaction controller and locks the primary hover state of the interaction
        /// controller.
        /// <see cref="primaryHoverLocked"/>
        /// </summary>
        public void LockPrimaryHover(InteractionBehaviour intObj)
        {
            _primaryHoveredObject = intObj;
            _primaryHoverLocked = true;
        }

        /// <summary>
        /// Gets the current position to check against nearby objects for hovering.
        /// Position is only used if the controller is currently tracked. For example,
        /// InteractionHand returns the center of the palm of the underlying Leap hand.
        /// </summary>
        public abstract Vector3 hoverPoint { get; }

        private HashSet<IInteractionBehaviour> _hoveredObjects = new HashSet<IInteractionBehaviour>();
        /// <summary>
        /// Returns a set of all Interaction objects currently hovered by this
        /// InteractionController.
        /// </summary>
        public ReadonlyHashSet<IInteractionBehaviour> hoveredObjects { get { return _hoveredObjects; } }

        protected abstract List<Transform> _primaryHoverPoints { get; }
        /// <summary>
        /// Gets the list of Transforms to consider against nearby objects to determine
        /// the closest object (primary hover) of this controller.
        /// </summary>
        public IReadOnlyList<Transform> primaryHoverPoints { get { return _primaryHoverPoints; } }

        /// <summary>
        /// Gets whether the InteractionController is currently primarily hovering over
        /// any interaction object.
        /// </summary>
        public bool isPrimaryHovering { get { return primaryHoveredObject != null; } }

        private IInteractionBehaviour _primaryHoveredObject;
        /// <summary>
        /// Gets the InteractionBehaviour that is currently this InteractionController's
        /// primary hovered object, if there is one.
        /// </summary>
        public IInteractionBehaviour primaryHoveredObject { get { return _primaryHoveredObject; } }

        private float _primaryHoverDistance = float.PositiveInfinity;
        /// <summary>
        /// Gets the distance from the closest primary hover point on this controller to its
        /// primarily hovered object, if there are any.
        /// </summary>
        public float primaryHoverDistance { get { return _primaryHoverDistance; } }

        /// <summary>
        /// Gets the position of the primary hovering point that is closest to its primary
        /// hovered object, if this controller has a primary hover. Otherwise, returns
        /// Vector3.zero.
        /// </summary>
        public Vector3 primaryHoveringPoint
        {
            get
            {
                return isPrimaryHovering ? _primaryHoverPoints[_primaryHoverPointIdx].position
                                         : Vector3.zero;
            }
        }

        /// <summary>
        /// Gets the index in the primaryHoverPoints array of the primary hover point that is
        /// currently closest to this controller's primary hover object.
        /// </summary>
        public int primaryHoveringPointIndex { get { return _primaryHoverPointIdx; } }

        /// <summary> Index of the closest primary hover point in the primaryHoverPoints list. </summary>
        private int _primaryHoverPointIdx = -1;
        private List<IInteractionBehaviour> _perPointPrimaryHovered = new List<IInteractionBehaviour>();
        private List<float> _perPointPrimaryHoverDistance = new List<float>();

        private void fixedUpdateHovering()
        {
            // Reset hover lock if the controller loses tracking.
            if (!isTracked && primaryHoverLocked)
            {
                _primaryHoverLocked = false;
            }

            // Update hover state.
            hoverActivityManager.activationRadius = manager.WorldHoverActivationRadius;

            Vector3? queryPosition = isTracked ? (Vector3?)hoverPoint : null;
            hoverActivityManager.UpdateActivityQuery(queryPosition);

            refreshHoverState(_hoverActivityManager.ActiveObjects);

            // Refresh buffer information from the previous frame to be able to fire
            // the appropriate hover state callbacks.
            refreshHoverStateBuffers();
            refreshPrimaryHoverStateBuffers();
        }

        // Hover history, handled as part of the Interaction Manager's state-check calls.
        private IInteractionBehaviour _primaryHoveredLastFrame = null;
        private HashSet<IInteractionBehaviour> _hoveredLastFrame = new HashSet<IInteractionBehaviour>();

        /// <summary>
        /// Clears the previous hover state data and calculates it anew based on the
        /// latest hover and primary hover point data.
        /// </summary>
        private void refreshHoverState(HashSet<IInteractionBehaviour> hoverCandidates)
        {
            // Prepare data from last frame for hysteresis later on.
            int primaryHoverPointIdxLastFrame = _primaryHoveredLastFrame != null ? _primaryHoverPointIdx : -1;

            _hoveredObjects.Clear();

            IInteractionBehaviour lockedPrimaryHoveredObject = null;
            if (primaryHoverLocked)
            {
                lockedPrimaryHoveredObject = _primaryHoveredObject;
            }
            _primaryHoveredObject = null;
            _primaryHoverDistance = float.PositiveInfinity;
            _primaryHoverPointIdx = -1;
            _perPointPrimaryHovered.Clear();
            _perPointPrimaryHoverDistance.Clear();
            for (int i = 0; i < primaryHoverPoints.Count; i++)
            {
                _perPointPrimaryHovered.Add(null);
                _perPointPrimaryHoverDistance.Add(float.PositiveInfinity);
            }

            // We can only update hover information if there's tracked data.
            if (!isTracked) return;

            // Determine values to apply hysteresis to the primary hover state.
            float maxNewPrimaryHoverDistance = float.PositiveInfinity;
            if (_primaryHoveredLastFrame != null && primaryHoverPointIdxLastFrame != -1
                && primaryHoverPoints[primaryHoverPointIdxLastFrame] != null)
            {

                if (_contactBehaviours.ContainsKey(_primaryHoveredLastFrame))
                {
                    // If we're actually touching the last primary hover, prevent the primary hover from changing at all.
                    maxNewPrimaryHoverDistance = 0F;
                }
                else
                {
                    float distanceToLastPrimaryHover = _primaryHoveredLastFrame.GetHoverDistance(
                                                         primaryHoverPoints[primaryHoverPointIdxLastFrame].position);
                    // Otherwise...
                    // The distance to a new object must be even closer than the current primary hover
                    // distance in order for that object to become the new primary hover.
                    maxNewPrimaryHoverDistance = distanceToLastPrimaryHover
                                                 * distanceToLastPrimaryHover.Map(0.009F, 0.018F, 0.4F, 0.95F);

                }
            }

            foreach (IInteractionBehaviour behaviour in hoverCandidates)
            {
                // All hover candidates automatically count as hovered.
                _hoveredObjects.Add(behaviour);

                // Some objects can ignore consideration for primary hover as an
                // optimization, since it can require a lot of distance checks.
                if (behaviour.ignorePrimaryHover) continue;

                // Do further processing to determine the primary hover if primary hover isn't
                // locked.
                else if (!primaryHoverLocked)
                {
                    processPrimaryHover(behaviour, maxNewPrimaryHoverDistance);
                }
            }

            // If the primary hover is locked, we need to process primary hover specifically
            // for the locked object.
            if (primaryHoverLocked && lockedPrimaryHoveredObject != null)
            {
                processPrimaryHover(lockedPrimaryHoveredObject, float.PositiveInfinity);
            }
        }

        private void processPrimaryHover(IInteractionBehaviour behaviour, float maxNewPrimaryHoverDistance)
        {
            // Check against all positions currently registered as primary hover points,
            // finding the closest one and updating hover data accordingly.
            float shortestPointDistance = float.PositiveInfinity;
            for (int i = 0; i < primaryHoverPoints.Count; i++)
            {
                var primaryHoverPoint = primaryHoverPoints[i];

                // It's possible to disable primary hover points to ignore them for hover
                // consideration.
                if (primaryHoverPoint == null) continue;
                if (!primaryHoverPoint.gameObject.activeInHierarchy) continue;

                // Skip non-index fingers for InteractionHands if they aren't extended.
                if (intHand != null)
                {
                    if (!(intHand.leapHand).Fingers[i].IsExtended && i != 1) { continue; }
                }

                // Check primary hover for the primary hover point.
                float behaviourDistance = behaviour.GetHoverDistance(primaryHoverPoint.position);
                if (behaviourDistance < shortestPointDistance)
                {

                    // This is the closest behaviour to this primary hover point.
                    _perPointPrimaryHovered[i] = behaviour;
                    _perPointPrimaryHoverDistance[i] = behaviourDistance;
                    shortestPointDistance = behaviourDistance;

                    if (primaryHoverLocked)
                    {
                        // If primary hover is locked, there's only one object to consider,
                        // and the current primary hover point is the closest one to the object
                        // so far, so update primary hover accordingly.
                        _primaryHoveredObject = _perPointPrimaryHovered[i]; // (redundant)
                        _primaryHoverDistance = _perPointPrimaryHoverDistance[i];
                        _primaryHoverPointIdx = i;
                    }
                    else if (shortestPointDistance < _primaryHoverDistance
                             && (behaviour == _primaryHoveredLastFrame || behaviourDistance < maxNewPrimaryHoverDistance))
                    {

                        // This is the closest behaviour to ANY primary hover point, and the
                        // distance is less than the hysteresis distance to transition away from
                        // the previous primary hovered object.
                        _primaryHoveredObject = _perPointPrimaryHovered[i];
                        _primaryHoverDistance = _perPointPrimaryHoverDistance[i];
                        _primaryHoverPointIdx = i;
                    }
                }
            }
        }

        /// <summary>
        /// Clears all hover tracking state and fires the hover-end callbacks immediately.
        /// If objects are still in the hover radius around this controller and the
        /// controller and manager are still active, HoverBegin callbacks will be invoked
        /// again on the next fixed frame.
        /// </summary>
        public void ClearHoverTracking()
        {
            _controllerListBuffer.Clear();
            _controllerListBuffer.Add(this);

            var tempObjs = Pool<HashSet<IInteractionBehaviour>>.Spawn();
            try
            {
                foreach (var intObj in hoveredObjects)
                {
                    tempObjs.Add(intObj);
                }

                foreach (var intObj in tempObjs)
                {
                    // Prevents normal hover state checking on the next frame from firing duplicate
                    // hover-end callbacks. If the object is still hovered (and the controller is
                    // still in an enabled state), the object WILL receive a hover begin callback
                    // on the next frame.
                    _hoveredObjects.Remove(intObj);
                    _hoveredLastFrame.Remove(intObj);

                    intObj.EndHover(_controllerListBuffer);
                }
            }
            finally
            {
                tempObjs.Clear();
                Pool<HashSet<IInteractionBehaviour>>.Recycle(tempObjs);
            }
        }

        /// <summary>
        /// Clears the hover tracking state for an object and fires the hover-end callback
        /// for that object immediately.
        ///
        /// If the object is still in the hover radius of this controller and the controller
        /// and manager are still active, the hover will begin anew on the next fixed frame.
        /// </summary>
        public void ClearHoverTrackingForObject(IInteractionBehaviour intObj)
        {
            if (!hoveredObjects.Contains(intObj)) return;

            _hoveredObjects.Remove(intObj);
            _hoveredLastFrame.Remove(intObj);

            _controllerListBuffer.Clear();
            _controllerListBuffer.Add(this);

            intObj.EndHover(_controllerListBuffer);
        }

        #region Hover State Checks

        private HashSet<IInteractionBehaviour> _hoverEndedBuffer = new HashSet<IInteractionBehaviour>();
        private HashSet<IInteractionBehaviour> _hoverBeganBuffer = new HashSet<IInteractionBehaviour>();

        private List<IInteractionBehaviour> _hoverRemovalCache = new List<IInteractionBehaviour>();
        private void refreshHoverStateBuffers()
        {
            _hoverBeganBuffer.Clear();
            _hoverEndedBuffer.Clear();

            var trackedBehaviours = _hoverActivityManager.ActiveObjects;
            foreach (var hoverable in trackedBehaviours)
            {
                bool inLastFrame = false, inCurFrame = false;
                if (hoveredObjects.Contains(hoverable))
                {
                    inCurFrame = true;
                }
                if (_hoveredLastFrame.Contains(hoverable))
                {
                    inLastFrame = true;
                }

                if (inCurFrame && !inLastFrame)
                {
                    _hoverBeganBuffer.Add(hoverable);
                    _hoveredLastFrame.Add(hoverable);
                }
                if (!inCurFrame && inLastFrame)
                {
                    _hoverEndedBuffer.Add(hoverable);
                    _hoveredLastFrame.Remove(hoverable);
                }
            }

            foreach (var hoverable in _hoveredLastFrame)
            {
                if (!trackedBehaviours.Contains(hoverable))
                {
                    _hoverEndedBuffer.Add(hoverable);
                    _hoverRemovalCache.Add(hoverable);
                }
            }
            foreach (var hoverable in _hoverRemovalCache)
            {
                _hoveredLastFrame.Remove(hoverable);
            }
            _hoverRemovalCache.Clear();
        }

        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Outputs objects that stopped being hovered by this hand this frame into hoverEndedObjects and returns whether
        /// the output set is empty.
        /// </summary>
        bool IInternalInteractionController.CheckHoverEnd(out HashSet<IInteractionBehaviour> hoverEndedObjects)
        {
            // Hover checks via the activity manager are robust to destroyed or made-invalid objects,
            // so no additional validity checking is required.
            hoverEndedObjects = _hoverEndedBuffer;

            return _hoverEndedBuffer.Count > 0;
        }

        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Outputs objects that began being hovered by this hand this frame into hoverBeganObjects and returns whether
        /// the output set is empty.
        /// </summary>
        bool IInternalInteractionController.CheckHoverBegin(out HashSet<IInteractionBehaviour> hoverBeganObjects)
        {
            hoverBeganObjects = _hoverBeganBuffer;
            return _hoverBeganBuffer.Count > 0;
        }

        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Outputs objects that are currently hovered by this hand into hoveredObjects and returns whether the
        /// output set is empty.
        /// </summary>
        bool IInternalInteractionController.CheckHoverStay(out HashSet<IInteractionBehaviour> hoveredObjects)
        {
            hoveredObjects = _hoveredObjects;
            return hoveredObjects.Count > 0;
        }

        #endregion

        /// <summary>
        /// Clears primary hover tracking state for the current primary hovered object.
        ///
        /// If the current primary hover is still the most eligible hovered object and this
        /// controller and its manager are still active, primary hover will begin anew on
        /// the next fixed frame.
        /// </summary>
        public void ClearPrimaryHoverTracking()
        {
            if (!isPrimaryHovering) return;

            // This will cause the primary-hover-end check to return this object, and prevent
            // a duplicate primary-hover-end call when refreshPrimaryHoverStateBuffers() is
            // called on the next frame.
            var formerlyPrimaryHoveredObj = _primaryHoveredObject;
            _primaryHoveredObject = null;
            _primaryHoveredLastFrame = null;

            if (primaryHoverLocked) primaryHoverLocked = false;

            _controllerListBuffer.Clear();
            _controllerListBuffer.Add(this);

            formerlyPrimaryHoveredObj.EndPrimaryHover(_controllerListBuffer);
        }

        #region Primary Hover State Checks

        private IInteractionBehaviour _primaryHoverEndedObject = null;
        private IInteractionBehaviour _primaryHoverBeganObject = null;

        private void refreshPrimaryHoverStateBuffers()
        {
            if (primaryHoveredObject != _primaryHoveredLastFrame)
            {
                if (_primaryHoveredLastFrame != null) _primaryHoverEndedObject = _primaryHoveredLastFrame;
                else _primaryHoverEndedObject = null;

                _primaryHoveredLastFrame = primaryHoveredObject;

                if (_primaryHoveredLastFrame != null) _primaryHoverBeganObject = _primaryHoveredLastFrame;
                else _primaryHoverBeganObject = null;
            }
            else
            {
                _primaryHoverEndedObject = null;
                _primaryHoverBeganObject = null;
            }
        }

        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Returns whether an object stopped being primarily hovered by this hand this frame and, if so,
        /// outputs the object into primaryHoverEndedObject (it will be null otherwise).
        /// </summary>
        bool IInternalInteractionController.CheckPrimaryHoverEnd(out IInteractionBehaviour primaryHoverEndedObject)
        {
            primaryHoverEndedObject = _primaryHoverEndedObject;
            bool primaryHoverEnded = primaryHoverEndedObject != null;

            if (primaryHoverEnded && _primaryHoverEndedObject is InteractionBehaviour)
            {
                OnEndPrimaryHoveringObject(_primaryHoverEndedObject as InteractionBehaviour);
            }

            return primaryHoverEnded;
        }

        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Returns whether an object began being primarily hovered by this hand this frame and, if so,
        /// outputs the object into primaryHoverBeganObject (it will be null otherwise).
        /// </summary>
        bool IInternalInteractionController.CheckPrimaryHoverBegin(out IInteractionBehaviour primaryHoverBeganObject)
        {
            primaryHoverBeganObject = _primaryHoverBeganObject;
            bool primaryHoverBegan = primaryHoverBeganObject != null;

            if (primaryHoverBegan && _primaryHoverBeganObject is InteractionBehaviour)
            {
                OnBeginPrimaryHoveringObject(_primaryHoverBeganObject as InteractionBehaviour);
            }

            return primaryHoverBegan;
        }

        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Returns whether any object is the primary hover of this hand and, if so, outputs
        /// the object into primaryHoveredObject.
        /// </summary>
        bool IInternalInteractionController.CheckPrimaryHoverStay(out IInteractionBehaviour primaryHoveredObject)
        {
            primaryHoveredObject = _primaryHoveredObject;
            bool primaryHoverStayed = primaryHoveredObject != null;

            if (primaryHoverStayed && primaryHoveredObject is InteractionBehaviour)
            {
                OnStayPrimaryHoveringObject(primaryHoveredObject as InteractionBehaviour);
            }

            return primaryHoverStayed;
        }

        #endregion

        #endregion

        #region Contact

        /// <summary>
        /// Gets the set of interaction objects that are currently touching this
        /// interaction controller.
        /// </summary>
        public ReadonlyHashSet<IInteractionBehaviour> contactingObjects { get { return _contactBehavioursSet; } }

        #region Contact Bones

        protected const float DEAD_ZONE_FRACTION = 0.04F;

        private float _softContactDislocationDistance = 0.03F;
        protected float softContactDislocationDistance
        {
            get { return _softContactDislocationDistance; }
            set { _softContactDislocationDistance = value; }
        }

        private static PhysicMaterial s_defaultContactBoneMaterial;
        protected static PhysicMaterial defaultContactBoneMaterial
        {
            get
            {
                if (s_defaultContactBoneMaterial == null)
                {
                    initDefaultContactBoneMaterial();
                }
                return s_defaultContactBoneMaterial;
            }
        }

        /// <summary>
        /// ContactBones should have PhysicMaterials with a bounciness of
        /// zero and a bounce combine set to minimum.
        /// </summary>
        private static void initDefaultContactBoneMaterial()
        {
            if (s_defaultContactBoneMaterial == null)
            {
                s_defaultContactBoneMaterial = new PhysicMaterial();
            }
            s_defaultContactBoneMaterial.hideFlags = HideFlags.HideAndDontSave;
            s_defaultContactBoneMaterial.bounceCombine = PhysicMaterialCombine.Minimum;
            s_defaultContactBoneMaterial.bounciness = 0F;
        }

        private bool _contactInitialized = false;
        protected bool _wasContactInitialized { get { return _contactInitialized; } set { _contactInitialized = value; } }
        public abstract ContactBone[] contactBones { get; }
        protected abstract GameObject contactBoneParent { get; }
        protected float lastObjectTouchedAdjustedMassMass = 0.2f;

        private Vector3[] _boneTargetPositions;
        private Quaternion[] _boneTargetRotations;

        /// <summary>
        /// Called to initialize contact colliders. See remarks for implementation
        /// requirements.
        /// </summary>
        /// <remarks>
        /// initContact() should:
        /// - Return false at any time if initialization cannot be performed.
        /// - Ensure the "contactBones" property returns all contact colliders.
        ///   - (Construct contact colliders if they don't already exist.)
        /// - Ensure the "contactBoneParent" property returns the common parent of all
        ///   contact colliders.
        ///   - (Construct the contact bone parent if it doesn't already exist.)
        /// - Return true if initialization was successful.
        ///
        /// Contact will only begin updating after initialization succeeds, otherwise
        /// it will try to initialize again on the next fixed frame.
        ///
        /// After initialization, the contact bone parent's layer will be set to
        /// the Interaction Manager's contactBoneLayer.
        /// </remarks>
        protected abstract bool initContact();

        private void finishInitContact()
        {
            contactBoneParent.layer = manager.contactBoneLayer;
            contactBoneParent.transform.parent = manager.transform;

            var comment = contactBoneParent.GetComponent<ContactBoneParent>();
            if (comment == null)
            {
                comment = contactBoneParent.AddComponent<ContactBoneParent>();
            }
            comment.controller = this;

            foreach (var contactBone in contactBones)
            {
                contactBone.rigidbody.maxAngularVelocity = 30F;

                contactBone.gameObject.layer = manager.contactBoneLayer;
            }

            _boneTargetPositions = new Vector3[contactBones.Length];
            _boneTargetRotations = new Quaternion[contactBones.Length];
        }

        private void fixedUpdateContact()
        {
            // Make sure contact data is initialized.
            if (!_contactInitialized)
            {
                if (initContact())
                {
                    finishInitContact();
                    _contactInitialized = true;
                    if (OnContactInitialized != null) OnContactInitialized(this);
                }
                else
                {
                    return;
                }
            }

            // Clear contact data if we lose tracking.
            if (!isTracked && _contactBehaviours.Count > 0)
            {
                _contactBehaviours.Clear();

                // Also clear soft contact state if tracking is lost.
                _softContactCollisions.Clear();
            }

            // Disable contact bone parent if we lose tracking.
            if (!isTracked)
            {
                contactBoneParent.SetActive(false);
                return;
            }
            else
            {
                if (!contactBoneParent.activeSelf)
                {
                    contactBoneParent.SetActive(true);
                }
            }

            // Request and store target bone positions and rotations
            // for use during the contact update.
            for (int i = 0; i < contactBones.Length; i++)
            {
                getColliderBoneTargetPositionRotation(i, out _boneTargetPositions[i],
                                                         out _boneTargetRotations[i]);
            }

            normalizeBoneMasses();
            for (int i = 0; i < contactBones.Length; i++)
            {
                updateContactBone(i, _boneTargetPositions[i], _boneTargetRotations[i]);
            }

            fixedUpdateSoftContact();
            fixedUpdateContactState();
        }

        /// <summary>
        /// If your controller features no moving colliders relative to itself, simply
        /// return the desired position and rotation for the given indexed contact bone
        /// in the contactBones array. (For example, by recording the local position and
        /// local rotation of each contact bone in initContact()). More complex controllers,
        /// such as InteractionHand, uses this method to set ContactBone target positions and
        /// rotations based on the tracked Leap hand.
        /// </summary>
        protected abstract void getColliderBoneTargetPositionRotation(int contactBoneIndex,
                                                              out Vector3 targetPosition,
                                                              out Quaternion targetRotation);

        private void normalizeBoneMasses()
        {
            //If any of the contact bones have contacted an object that the others have not
            //Propagate that change in mass to the rest of the bones in the hand
            float tempAdjustedMass = lastObjectTouchedAdjustedMassMass;
            for (int i = 0; i < contactBones.Length; i++)
            {
                if (contactBones[i]._lastObjectTouchedAdjustedMass != tempAdjustedMass)
                {
                    tempAdjustedMass = contactBones[i]._lastObjectTouchedAdjustedMass;
                    for (int j = 0; j < contactBones.Length; j++)
                    {
                        contactBones[j]._lastObjectTouchedAdjustedMass = tempAdjustedMass;
                    }
                    break;
                }
            }
            lastObjectTouchedAdjustedMassMass = tempAdjustedMass;
        }

        private void updateContactBone(int contactBoneIndex, Vector3 targetPosition, Quaternion targetRotation)
        {
            ContactBone contactBone = contactBones[contactBoneIndex];
            Rigidbody body = contactBone.rigidbody;

            // Infer ahead if the Interaction Manager has a moving frame of reference.
            //manager.TransformAheadByFixedUpdate(targetPosition, targetRotation, out targetPosition, out targetRotation);

            // Set a fixed rotation for bones; otherwise most friction is lost
            // as any capsule or spherical bones will roll on contact.
            body.MoveRotation(targetRotation);

            // Calculate how far off its target the contact bone is.
            float errorDistance = 0f;
            float errorFraction = 0f;

            float boneWidth = contactBone.width;

            Vector3 lastTargetPositionTransformedAhead = contactBone.lastTargetPosition;
            if (manager.hasMovingFrameOfReference)
            {
                manager.TransformAheadByFixedUpdate(contactBone.lastTargetPosition, out lastTargetPositionTransformedAhead);
            }
            errorDistance = Vector3.Distance(lastTargetPositionTransformedAhead, body.position);
            errorFraction = errorDistance / boneWidth;

            // Adjust the mass of the contact bone based on the mass of
            // the object it is currently touching.
            float speed = 0f;
            speed = velocity.magnitude;
            float massScale = Mathf.Clamp(1.0F - (errorFraction * 2.0F), 0.1F, 1.0F)
                          * Mathf.Clamp(speed * 10F, 1F, 10F);
            if (massScale * contactBone._lastObjectTouchedAdjustedMass > 0)
            {
                body.mass = massScale * contactBone._lastObjectTouchedAdjustedMass;
            }

            // Potentially enable Soft Contact if our error is too large.
            if (!_softContactEnabled && errorDistance >= softContactDislocationDistance
              && speed < 1.5F
                /* && boneArrayIndex != NUM_FINGERS * BONES_PER_FINGER */)
            {
                EnableSoftContact();
            }

            // Attempt to move the contact bone to its target position and rotation
            // by setting its target velocity and angular velocity. Include a "deadzone"
            // for position to avoid tiny vibrations.
            float deadzone = Mathf.Min(DEAD_ZONE_FRACTION * boneWidth, 0.01F * scale);
            Vector3 delta = (targetPosition - body.position);
            float deltaMag = delta.magnitude;
            if (deltaMag <= deadzone)
            {
                body.velocity = Vector3.zero;
                contactBone.lastTargetPosition = body.position;
            }
            else
            {
                delta *= (deltaMag - deadzone) / deltaMag;
                contactBone.lastTargetPosition = body.position + delta;

                Vector3 targetVelocity = delta / Time.fixedDeltaTime;
                float targetVelocityMag = targetVelocity.magnitude;
                body.velocity = (targetVelocity / targetVelocityMag)
                              * Mathf.Clamp(targetVelocityMag, 0F, 100F);
            }
            Quaternion deltaRot = targetRotation * Quaternion.Inverse(body.rotation);
            body.angularVelocity = PhysicsUtility.ToAngularVelocity(deltaRot, Time.fixedDeltaTime);
        }

        #endregion

        #region Soft Contact

        private bool _softContactEnabled = false;
        /// <summary>
        /// Is soft contact enabled?
        /// </summary>
        public bool softContactEnabled { get { return _softContactEnabled; } }

        private bool _disableSoftContactEnqueued = false;
        private IEnumerator _delayedDisableSoftContactCoroutine;

        private Collider[] _softContactColliderBuffer = new Collider[32];

        private bool _notTrackedLastFrame = true;

        private void fixedUpdateSoftContact()
        {
            if (!isTracked)
            {
                _notTrackedLastFrame = true;
                return;
            }
            else
            {
                // If the hand was just initialized, initialize with soft contact.
                if (_notTrackedLastFrame)
                {
                    EnableSoftContact();
                }

                _notTrackedLastFrame = false;
            }

            if (_softContactEnabled)
            {
                foreach (var contactBone in contactBones)
                {
                    Collider contactBoneCollider = contactBone.collider;
                    if (contactBoneCollider is SphereCollider)
                    {
                        var boneSphere = contactBoneCollider as SphereCollider;

                        int numCollisions = Physics.OverlapSphereNonAlloc(contactBone.transform.TransformPoint(boneSphere.center),
                                                                          contactBone.transform.lossyScale.x * boneSphere.radius,
                                                                          _softContactColliderBuffer,
                                                                          manager.GetInteractionLayerMask(),
                                                                          QueryTriggerInteraction.Ignore);
                        for (int i = 0; i < numCollisions; i++)
                        {
                            //NotifySoftContactOverlap(contactBone, _softContactColliderBuffer[i]);

                            // If the rigidbody is null, the object may have been destroyed.
                            if (_softContactColliderBuffer[i] == null || _softContactColliderBuffer[i].attachedRigidbody == null) continue;
                            IInteractionBehaviour intObj;
                            if (manager.interactionObjectBodies.TryGetValue(_softContactColliderBuffer[i].attachedRigidbody, out intObj))
                            {
                                // Skip soft contact if the object is ignoring contact.
                                if (intObj.ignoreContact) continue;
                                if (intObj.isGrasped) continue;
                            }

                            PhysicsUtility.generateSphereContact(boneSphere, 0, _softContactColliderBuffer[i],
                                                                 ref manager._softContacts,
                                                                 ref manager._softContactOriginalVelocities);
                        }
                    }
                    else if (contactBoneCollider is CapsuleCollider)
                    {
                        var boneCapsule = contactBoneCollider as CapsuleCollider;

                        Vector3 point0, point1;
                        boneCapsule.GetCapsulePoints(out point0, out point1);

                        int numCollisions = Physics.OverlapCapsuleNonAlloc(point0, point1,
                                                                           contactBone.transform.lossyScale.x * boneCapsule.radius,
                                                                           _softContactColliderBuffer,
                                                                           manager.GetInteractionLayerMask(),
                                                                           QueryTriggerInteraction.Ignore);
                        for (int i = 0; i < numCollisions; i++)
                        {
                            //NotifySoftContactOverlap(contactBone, _softContactColliderBuffer[i]);

                            // If the rigidbody is null, the object may have been destroyed.
                            if (_softContactColliderBuffer[i] == null || _softContactColliderBuffer[i].attachedRigidbody == null) continue;
                            IInteractionBehaviour intObj;
                            if (manager.interactionObjectBodies.TryGetValue(_softContactColliderBuffer[i].attachedRigidbody, out intObj))
                            {
                                // Skip soft contact if the object is ignoring contact.
                                if (intObj.ignoreContact) continue;
                                if (intObj.isGrasped) continue;
                            }

                            PhysicsUtility.generateCapsuleContact(boneCapsule, 0,
                                                                  _softContactColliderBuffer[i],
                                                                  ref manager._softContacts,
                                                                  ref manager._softContactOriginalVelocities);
                        }
                    }
                    else
                    {
                        var boneBox = contactBoneCollider as BoxCollider;

                        if (boneBox == null)
                        {
                            Debug.LogError("Unsupported collider type in ContactBone. Supported "
                                         + "types are SphereCollider, CapsuleCollider, and "
                                         + "BoxCollider.", this);
                            continue;
                        }

                        int numCollisions = Physics.OverlapBoxNonAlloc(boneBox.transform.TransformPoint(boneBox.center),
                                                                       Vector3.Scale(boneBox.size * 0.5F, contactBone.transform.lossyScale),
                                                                       _softContactColliderBuffer,
                                                                       boneBox.transform.rotation,
                                                                       manager.GetInteractionLayerMask(),
                                                                       QueryTriggerInteraction.Ignore);
                        for (int i = 0; i < numCollisions; i++)
                        {
                            //NotifySoftContactOverlap(contactBone, _softContactColliderBuffer[i]);

                            // If the rigidbody is null, the object may have been destroyed.
                            if (_softContactColliderBuffer[i] == null || _softContactColliderBuffer[i].attachedRigidbody == null) continue;
                            IInteractionBehaviour intObj;
                            if (manager.interactionObjectBodies.TryGetValue(_softContactColliderBuffer[i].attachedRigidbody, out intObj))
                            {
                                // Skip soft contact if the object is ignoring contact.
                                if (intObj.ignoreContact) continue;
                                if (intObj.isGrasped) continue;
                            }

                            PhysicsUtility.generateBoxContact(boneBox, 0, _softContactColliderBuffer[i],
                                                              ref manager._softContacts,
                                                              ref manager._softContactOriginalVelocities);
                        }
                    }
                }

                // TODO: Implement me to replace trigger colliders
                //FinishSoftContactOverlapChecks();

                //for (int i = 0; i < contactBones.Length; i++) {
                //  Vector3 bonePosition = _boneTargetPositions[i];
                //   Quaternion boneRotation = _boneTargetRotations[i];

                //   Generate soft contact data based on spheres at each bonePosition
                //   of radius softContactBoneRadius.
                //  bool sphereIntersecting;
                //  using (new ProfilerSample("Generate Soft Contacts")) {
                //    sphereIntersecting = PhysicsUtility.generateSphereContacts(bonePosition,
                //                                                               _softContactBoneRadius,
                //                                                               (bonePosition - _bonePositionsLastFrame[i]) / Time.fixedDeltaTime,
                //                                                               1 << manager.interactionLayer,
                //                                                               ref manager._softContacts,
                //                                                               ref manager._softContactOriginalVelocities,
                //                                                               ref _tempColliderArray);
                //  }

                //  _bonePositionsLastFrame[i] = bonePosition;

                //  softlyContacting = sphereIntersecting ? true : softlyContacting;
                //}

                if (_softContactCollisions.Count > 0)
                {
                    _disableSoftContactEnqueued = false;
                    if (_delayedDisableSoftContactCoroutine != null)
                    {
                        manager.StopCoroutine(_delayedDisableSoftContactCoroutine);
                    }
                }
                else
                {
                    // If there are no detected Contacts, exit soft contact mode.
                    DisableSoftContact();
                }
            }
        }

        /// <summary>
        /// Optionally override this method to perform logic just before soft contact
        /// is enabled for this controller.
        ///
        /// The InteractionHand implementation takes the opportunity to reset its contact
        /// bone's joints, which may have initialized slightly out of alignment on initial
        /// construction.
        /// </summary>
        protected virtual void onPreEnableSoftContact() { }

        /// <summary>
        /// Optionally override this method to perform logic just after soft contact
        /// is disabled for this controller.
        ///
        /// The InteractionHand implementation takes the opportunity to reset its contact
        /// bone's joints, which my have initialized slightly out of alignment on initial
        /// construction.
        /// </summary>
        protected virtual void onPostDisableSoftContact() { }

        /// <summary>
        /// Enable soft contact for each of the contact bones
        /// </summary>
        public void EnableSoftContact()
        {
            if (!isTracked) return;

            _disableSoftContactEnqueued = false;
            if (!_softContactEnabled)
            {
                onPreEnableSoftContact();

                _softContactEnabled = true;

                if (_delayedDisableSoftContactCoroutine != null)
                {
                    manager.StopCoroutine(_delayedDisableSoftContactCoroutine);
                }

                if (contactBones != null)
                {
                    for (int i = 0; i < contactBones.Length; i++)
                    {
                        if (contactBones[i].collider == null) continue;

                        disableContactBoneCollision();
                    }
                }
            }
        }

        /// <summary>
        /// Disable soft contact for each of the contact bones
        /// </summary>
        public void DisableSoftContact()
        {
            if (!_disableSoftContactEnqueued)
            {
                _delayedDisableSoftContactCoroutine = DelayedDisableSoftContact();
                manager.StartCoroutine(_delayedDisableSoftContactCoroutine);
                _disableSoftContactEnqueued = true;
            }
        }

        private IEnumerator DelayedDisableSoftContact()
        {
            yield return new WaitForSecondsRealtime(0.3f);
            if (_disableSoftContactEnqueued)
            {
                _softContactEnabled = false;
                enableContactBoneCollision();
                onPostDisableSoftContact();
            }
        }

        #region Soft Contact Collision Tracking
        /*
        // TODO: Make this a thing so we aren't using triggers
        private void NotifySoftContactOverlap(ContactBone contactBone, Collider otherCollider) {

        }

        // TODO: Make this a thing so we aren't using triggers
        private void FinishSoftContactOverlapChecks() {

        }*/

        // TODO: Maintaining a reference to the interaction object doesn't appear to be
        // necessary here, so get rid of the Pair class as a small optimization
        private Dictionary<BoneIntObjPair, HashSet<Collider>> _softContactCollisions = new Dictionary<BoneIntObjPair, HashSet<Collider>>();

        private struct BoneIntObjPair : IEquatable<BoneIntObjPair>
        {
            public ContactBone bone;
            public IInteractionBehaviour intObj;

            public override bool Equals(object obj)
            {
                return obj is BoneIntObjPair && this == (BoneIntObjPair)obj;
            }
            public bool Equals(BoneIntObjPair other)
            {
                return this == other;
            }
            public static bool operator !=(BoneIntObjPair one, BoneIntObjPair other)
            {
                return !(one == other);
            }
            public static bool operator ==(BoneIntObjPair one, BoneIntObjPair other)
            {
                return one.bone == other.bone && one.intObj == other.intObj;
            }
            public override int GetHashCode()
            {
                return bone.GetHashCode() ^ intObj.GetHashCode();
            }
        }

        public void NotifySoftContactCollisionEnter(ContactBone bone,
                                                    IInteractionBehaviour intObj,
                                                    Collider collider)
        {
            var pair = new BoneIntObjPair() { bone = bone, intObj = intObj };

            if (!_softContactCollisions.ContainsKey(pair))
            {
                _softContactCollisions[pair] = new HashSet<Collider>();
            }
            _softContactCollisions[pair].Add(collider);
        }

        public void NotifySoftContactCollisionExit(ContactBone bone,
                                                   IInteractionBehaviour intObj,
                                                   Collider collider)
        {
            var pair = new BoneIntObjPair() { bone = bone, intObj = intObj };

            if (!_softContactCollisions.ContainsKey(pair))
            {
                Debug.LogError("No collision set found for this pair of collisions; Exit method "
                             + "was called without a prior, corresponding Enter method!", this);
            }
            _softContactCollisions[pair].Remove(collider);

            if (_softContactCollisions[pair].Count == 0)
            {
                _softContactCollisions.Remove(pair);
            }
        }

        #endregion

        #endregion

        #region Contact Callbacks

        private HashSet<IInteractionBehaviour> _contactBehavioursSet = new HashSet<IInteractionBehaviour>();
        private List<InteractionBehaviour> _contactingBehavious = new List<InteractionBehaviour>();

        private Dictionary<IInteractionBehaviour, int> _contactBehaviours = new Dictionary<IInteractionBehaviour, int>();
        private HashSet<IInteractionBehaviour> _contactBehavioursLastFrame = new HashSet<IInteractionBehaviour>();
        private List<IInteractionBehaviour> _contactBehaviourRemovalCache = new List<IInteractionBehaviour>();

        private HashSet<IInteractionBehaviour> _contactEndedBuffer = new HashSet<IInteractionBehaviour>();
        private HashSet<IInteractionBehaviour> _contactBeganBuffer = new HashSet<IInteractionBehaviour>();

        /// <summary>
        /// Add contact bone to set of ongoing collisions with given interaction behaviour
        /// </summary>
        public void NotifyContactBoneCollisionEnter(ContactBone contactBone, IInteractionBehaviour interactionObj)
        {
            int count;
            if (_contactBehaviours.TryGetValue(interactionObj, out count))
            {
                _contactBehaviours[interactionObj] = count + 1;
            }
            else
            {
                _contactBehaviours[interactionObj] = 1;
                _contactBehavioursSet.Add(interactionObj);
            }
        }

        /// <summary>
        /// Refresh contact bone state as being in an ongoing collision with given interaction object.
        /// </summary>
        public void NotifyContactBoneCollisionStay(ContactBone contactBone, IInteractionBehaviour interactionObj)
        {
            // If Contact state is cleared manually or due to the controller being disabled,
            // it will be restored here.

            int count;
            if (!_contactBehaviours.TryGetValue(interactionObj, out count))
            {
                _contactBehaviours[interactionObj] = 1;
                _contactBehavioursSet.Add(interactionObj);
            }
        }

        /// <summary>
        /// Remove contact bone from set of ongoing collisions with given interaction behaviour.
        /// </summary>
        public void NotifyContactBoneCollisionExit(ContactBone contactBone, IInteractionBehaviour interactionObj)
        {
            if (interactionObj.ignoreContact)
            {
                if (_contactBehaviours.ContainsKey(interactionObj)) _contactBehaviours.Remove(interactionObj);
                return;
            }

            // Sometimes when the controller is disabled and re-enabled, we might be missing the
            // key in the dictionary already.
            if (!_contactBehaviours.ContainsKey(interactionObj)) return;

            int count = _contactBehaviours[interactionObj];
            if (count == 1)
            {
                _contactBehaviours.Remove(interactionObj);
                _contactBehavioursSet.Remove(interactionObj);
            }
            else
            {
                _contactBehaviours[interactionObj] = count - 1;
            }
        }

        /// <summary>
        /// Clears contact state for this controller and fires the appropriate ContactEnd
        /// callbacks on currently-contacted interaction objects immediately.
        ///
        /// If the controller is still contacting objects and it and its manager are still
        /// active, contact will begin anew on the next fixed frame.
        /// </summary>
        public void ClearContactTracking()
        {
            _controllerListBuffer.Clear();
            _controllerListBuffer.Add(this);

            var tempObjs = Pool<HashSet<IInteractionBehaviour>>.Spawn();
            try
            {
                foreach (var intObj in contactingObjects)
                {
                    tempObjs.Add(intObj);
                }

                foreach (var intObj in tempObjs)
                {
                    _contactBehavioursSet.Remove(intObj);
                    _contactBehaviours.Remove(intObj);
                    _contactBehavioursLastFrame.Remove(intObj);

                    intObj.EndContact(_controllerListBuffer);
                }
            }
            finally
            {
                tempObjs.Clear();
                Pool<HashSet<IInteractionBehaviour>>.Recycle(tempObjs);
            }
        }

        /// <summary>
        /// Clears contact state for the specified object and fires its ContactEnd callbacks
        /// immediately.
        ///
        /// If the controller is still contacting the object and it and its manager are still
        /// active, contact will begin anew on the next fixed frame.
        /// </summary>
        public void ClearContactTrackingForObject(IInteractionBehaviour intObj)
        {
            if (!contactingObjects.Contains(intObj)) return;

            _contactBehavioursSet.Remove(intObj);
            _contactBehaviours.Remove(intObj);
            _contactBehavioursLastFrame.Remove(intObj);

            _controllerListBuffer.Clear();
            _controllerListBuffer.Add(this);

            intObj.EndContact(_controllerListBuffer);
        }

        /// <summary>
        /// Called as a part of the Interaction Hand's general fixed frame update,
        /// before any specific-callback-related updates.
        /// </summary>
        private void fixedUpdateContactState()
        {
            _contactEndedBuffer.Clear();
            _contactBeganBuffer.Clear();

            // Update contact ended state.
            _contactBehaviourRemovalCache.Clear();
            foreach (var interactionObj in _contactBehavioursLastFrame)
            {
                if (!_contactBehaviours.ContainsKey(interactionObj)
                 || !contactBoneParent.activeInHierarchy
                 /* || !contactEnabled TODO: Use properties to support disabling contact at runtime! */)
                {
                    _contactEndedBuffer.Add(interactionObj);
                    _contactBehaviourRemovalCache.Add(interactionObj);
                }
            }
            foreach (var interactionObj in _contactBehaviourRemovalCache)
            {
                _contactBehavioursLastFrame.Remove(interactionObj);
            }

            // Update contact began state.
            if (contactBoneParent.activeInHierarchy /* && contactEnabled TODO: can this just be removed cleanly?*/)
            {
                foreach (var intObjCountPair in _contactBehaviours)
                {
                    var interactionObj = intObjCountPair.Key;
                    if (!_contactBehavioursLastFrame.Contains(interactionObj))
                    {
                        _contactBeganBuffer.Add(interactionObj);
                        _contactBehavioursLastFrame.Add(interactionObj);
                    }
                }
            }
        }

        private List<IInteractionBehaviour> _removeContactObjsBuffer = new List<IInteractionBehaviour>();
        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Outputs interaction objects that stopped being touched by this hand this frame into contactEndedObjects
        /// and returns whether the output set is empty.
        /// </summary>
        bool IInternalInteractionController.CheckContactEnd(out HashSet<IInteractionBehaviour> contactEndedObjects)
        {
            // Ensure contact objects haven't been destroyed or set to ignore contact
            _removeContactObjsBuffer.Clear();
            foreach (var objTouchCountPair in _contactBehaviours)
            {
                if (objTouchCountPair.Key.gameObject == null
                    || objTouchCountPair.Key.rigidbody == null
                    || objTouchCountPair.Key.ignoreContact
                    || !isTracked)
                {
                    _removeContactObjsBuffer.Add(objTouchCountPair.Key);
                }
            }

            // Clean out removed, invalid, or ignoring-contact objects
            foreach (var intObj in _removeContactObjsBuffer)
            {
                _contactBehaviours.Remove(intObj);
                _contactEndedBuffer.Add(intObj);
            }

            contactEndedObjects = _contactEndedBuffer;
            return _contactEndedBuffer.Count > 0;
        }

        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Outputs interaction objects that started being touched by this hand this frame into contactBeganObjects
        /// and returns whether the output set is empty.
        /// </summary>
        bool IInternalInteractionController.CheckContactBegin(out HashSet<IInteractionBehaviour> contactBeganObjects)
        {
            contactBeganObjects = _contactBeganBuffer;
            return _contactBeganBuffer.Count > 0;
        }

        private HashSet<IInteractionBehaviour> _contactedObjects = new HashSet<IInteractionBehaviour>();
        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Outputs interaction objects that are currently being touched by the hand into contactedObjects
        /// and returns whether the output set is empty.
        /// </summary>
        bool IInternalInteractionController.CheckContactStay(out HashSet<IInteractionBehaviour> contactedObjects)
        {
            _contactedObjects.Clear();
            foreach (var objCountPair in _contactBehaviours)
            {
                _contactedObjects.Add(objCountPair.Key);
            }

            contactedObjects = _contactedObjects;
            return contactedObjects.Count > 0;
        }

        #endregion

        private void disableContactBoneCollision()
        {
            foreach (var contactBone in contactBones)
            {
                contactBone.collider.isTrigger = true;
            }
        }

        private void enableContactBoneCollision()
        {
            foreach (var contactBone in contactBones)
            {
                contactBone.collider.isTrigger = false;
            }
        }

        private void resetContactBonePose()
        {
            int index = 0;
            foreach (var contactBone in contactBones)
            {
                Vector3 position;
                Quaternion rotation;
                getColliderBoneTargetPositionRotation(index++, out position, out rotation);

                contactBone.rigidbody.position = position;
                contactBone.rigidbody.rotation = rotation;
                contactBone.rigidbody.velocity = Vector3.zero;
                contactBone.rigidbody.angularVelocity = Vector3.zero;
            }
        }

        #endregion

        #region Grasping


        /// <summary>
        /// Gets whether the controller is currently grasping an object.
        /// </summary>
        public bool isGraspingObject { get { return _graspedObject != null; } }

        /// <summary>
        /// Gets the object the controller is currently grasping, or null if there is no such object. 
        /// </summary>
        public IInteractionBehaviour graspedObject { get { return _graspedObject; } }

        /// <summary>
        /// Gets the set of objects currently considered graspable.
        /// </summary>
        public ReadonlyHashSet<IInteractionBehaviour> graspCandidates { get { return graspActivityManager.ActiveObjects; } }

        /// <summary>
        /// Gets the points of the controller to add to the calculation to determine how
        /// held objects should move as the controller moves. Interaction Controllers utilize
        /// the Kabsch algorithm to determine this, which is most noticeable when using
        /// Leap hands via InteractionHands to manipulate held objects. Rigid controllers
        /// may simply return a single rigid point on the controller. Refer to InteractionHand
        /// for a reference implementation for dynamic controllers (e.g. hands).
        /// </summary>
        public abstract List<Vector3> graspManipulatorPoints { get; }

        /// <summary>
        /// Returns approximately where the controller is grasping the currently grasped
        /// InteractionBehaviour.
        /// This method will print an error if the controller is not currently grasping an object.
        /// </summary>
        public abstract Vector3 GetGraspPoint();

        /// <summary>
        /// Checks if the provided interaction object can be grasped by this interaction
        /// controller in its current state. If so, the controller will initiate a grasp and
        /// this method will return true, otherwise this method returns false.
        /// </summary>
        public bool TryGrasp(IInteractionBehaviour intObj)
        {
            if (checkShouldGraspAtemporal(intObj))
            {
                _graspedObject = intObj;
                OnGraspBegin();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Seamlessly swap the currently grasped object for a replacement object.  It will
        /// behave like the hand released the current object, and then grasped the new object.
        /// 
        /// This method will not teleport the replacement object or move it in any way, it will
        /// just cause it to be grasped.  That means that you will be responsible for moving
        /// the replacement object into a reasonable position for it to be grasped.
        /// </summary>
        public virtual void SwapGrasp(IInteractionBehaviour replacement)
        {
            if (_graspedObject == null)
            {
                throw new InvalidOperationException("Cannot swap grasp if we are not currently grasping.");
            }

            if (replacement == null)
            {
                throw new ArgumentNullException("The replacement object is null!");
            }

            if (replacement.isGrasped && !replacement.allowMultiGrasp)
            {
                throw new InvalidOperationException("Cannot swap grasp if the replacement object is already grasped and does not support multi grasp.");
            }

            //Notify the currently grasped object that it is being released
            _releasingControllersBuffer.Clear();
            _releasingControllersBuffer.Add(this);
            _graspedObject.EndGrasp(_releasingControllersBuffer);
            OnGraspEnd();

            //Switch to the replacement object
            _graspedObject = replacement;

            var tempControllers = Pool<List<InteractionController>>.Spawn();
            try
            {
                //Let the replacement object know that it is being grasped
                tempControllers.Add(this);
                replacement.BeginGrasp(tempControllers);
                OnGraspBegin();
            }
            finally
            {
                tempControllers.Clear();
                Pool<List<InteractionController>>.Recycle(tempControllers);
            }
        }

        /// <summary>
        /// Checks if the provided interaction object can be grasped by this interaction
        /// controller in its current state. If so, the controller will initiate a grasp and
        /// this method will return true, otherwise this method returns false.
        /// 
        /// This method is useful if the controller requires conditions to initiate a grasp
        /// that differ from the conditions necessary to maintain a grasp after it has been
        /// initiated. This method allows a grasp to occur if certain initiation conditions
        /// are not met, such as the motion of a hand's fingers towards the palm,
        /// but if the grasp holding conditions are met, such as the penetration of a hand's
        /// fingers inside the interaction object.
        /// </summary>
        protected abstract bool checkShouldGraspAtemporal(IInteractionBehaviour intObj);

        private Func<Collider, IInteractionBehaviour> graspActivityFilter;
        private IInteractionBehaviour graspFilterFunc(Collider collider)
        {
            Rigidbody body = collider.attachedRigidbody;
            IInteractionBehaviour intObj = null;

            bool validForGrasping = body != null
                                 && manager.interactionObjectBodies.TryGetValue(body, out intObj)
                                 && !intObj.ShouldIgnoreGrasping(this)
                                 && !intObj.ignoreGrasping;

            if (validForGrasping) return intObj;

            return null;
        }

        // Layer mask for the hover acitivity manager.
        private Func<int> graspLayerMaskAccessor;

        // Grasp Activity Manager
        private ActivityManager<IInteractionBehaviour> _graspActivityManager;
        /// <summary> Determines which objects are graspable any given frame. </summary>
        private ActivityManager<IInteractionBehaviour> graspActivityManager
        {
            get
            {
                if (_graspActivityManager == null)
                {
                    if (graspActivityFilter == null) graspActivityFilter = graspFilterFunc;
                    if (graspLayerMaskAccessor == null) graspLayerMaskAccessor = manager.GetInteractionLayerMask;

                    _graspActivityManager = new ActivityManager<IInteractionBehaviour>(1F, graspActivityFilter);

                    _graspActivityManager.activationLayerFunction = graspLayerMaskAccessor;
                }
                return _graspActivityManager;
            }
        }

        private IInteractionBehaviour _graspedObject = null;

        private void fixedUpdateGrasping()
        {
            Vector3? graspPoint = isTracked ? (Vector3?)hoverPoint : null;
            graspActivityManager.UpdateActivityQuery(graspPoint);

            fixedUpdateGraspingState();
        }

        /// <summary>
        /// Called every fixed frame if grasping is enabled in the Interaction Manager.
        ///
        /// graspActivityManager.ActiveObjects will contain objects around the hoverPoint
        /// within the grasping radius -- in other words, objects eligible to be grasped
        /// by the controller. Refer to it to avoid checking grasp eligibility against all
        /// graspable objects in your scene.
        /// </summary>
        protected abstract void fixedUpdateGraspingState();

        /// <summary>
        /// Optionally override this method to perform logic just before a grasped object is
        /// released because it is no longer eligible to be grasped by this controller or
        /// ReleaseGrasp() was manually called on the controller.
        /// </summary>
        protected virtual void onGraspedObjectForciblyReleased(IInteractionBehaviour objectToBeReleased) { }

        /// <summary>
        /// Returns whether this controller should grasp an object this fixed frame, and if so,
        /// sets objectToGrasp to the object the controller should grasp.
        /// </summary>
        protected abstract bool checkShouldGrasp(out IInteractionBehaviour objectToGrasp);

        /// <summary>
        /// Returns whether this controller should release an object this fixed frame, and if so,
        /// sets objectToRelease to the object the controller should release.
        /// </summary>
        protected abstract bool checkShouldRelease(out IInteractionBehaviour objectToRelease);

        private List<InteractionController> _releasingControllersBuffer = new List<InteractionController>();
        /// <summary>
        /// Releases the object this hand is holding and returns true if the hand was holding an object,
        /// or false if there was no object to release. The released object will dispatch OnGraspEnd()
        /// immediately. The hand is guaranteed not to be holding an object directly after this method
        /// is called.
        /// </summary>
        public bool ReleaseGrasp()
        {
            if (_graspedObject == null)
            {
                return false;
            }
            else
            {
                // Release this controller's grasp.
                _releasingControllersBuffer.Clear();
                _releasingControllersBuffer.Add(this);

                // Calling things in the right order requires we remember the object we're
                // releasing.
                var tempGraspedObject = _graspedObject;

                // Clear controller grasped object, and enable soft contact.
                OnGraspEnd();
                _graspedObject = null;
                EnableSoftContact();

                // Fire object's grasp-end callback.
                tempGraspedObject.EndGrasp(_releasingControllersBuffer);

                // The grasped object was forcibly released; some controllers hook into this
                // by virtual method implementation.
                onGraspedObjectForciblyReleased(tempGraspedObject);

                return true;
            }
        }

        /// <summary>
        /// Helper static method for forcing multiple controllers to release their grasps
        /// on a single object simultaneously. All of the provided controllers must be
        /// grasping the argument interaction object.
        /// </summary>
        /// <details>
        /// The input controllers List is copied to a temporary (pooled) buffer before 
        /// release operations are actually carried out. This prevents errors that might
        /// arise from modifying a held-controllers list while enumerating through the same
        /// list.
        /// </details>
        public static void ReleaseGrasps(IInteractionBehaviour graspedObj,
                                         ReadonlyHashSet<InteractionController> controllers)
        {
            var controllersBuffer = Pool<List<InteractionController>>.Spawn();
            try
            {
                foreach (var controller in controllers)
                {
                    if (controller.graspedObject != graspedObj)
                    {
                        Debug.LogError("Argument intObj " + graspedObj.name + " is not held by "
                                     + "controller " + controller.name + "; skipping release for this "
                                     + "controller.");
                        continue;
                    }

                    controllersBuffer.Add(controller);
                }

                // Enable soft contact on releasing controllers, and clear grasp state.
                // Note: controllersBuffer is iterated twice to preserve state modification order.
                // For reference order, see InteractionController.ReleaseGrasp() above.
                foreach (var controller in controllersBuffer)
                {
                    // Fire grasp end callback for the controller.
                    controller.OnGraspEnd();

                    // Clear grasped object state.
                    controller._graspedObject = null;

                    // Avoid "popping" of released objects by enabling soft contact on releasing
                    // controllers.
                    controller.EnableSoftContact();
                }

                // Evaluate object logic for being released by each controller.
                graspedObj.EndGrasp(controllersBuffer);

                // Object was forcibly released, so fire virtual callbacks on each controller.
                foreach (var controller in controllersBuffer)
                {
                    controller.onGraspedObjectForciblyReleased(graspedObj);
                }
            }
            finally
            {
                controllersBuffer.Clear();
                Pool<List<InteractionController>>.Recycle(controllersBuffer);
            }
        }

        /// <summary>
        /// As ReleaseGrasp(), but also outputs the released object into releasedObject if the hand
        /// successfully released an object.
        /// </summary>
        public bool ReleaseGrasp(out IInteractionBehaviour releasedObject)
        {
            releasedObject = _graspedObject;

            if (ReleaseGrasp())
            {
                // releasedObject will be non-null
                return true;
            }

            // releasedObject will be null
            return false;
        }

        /// <summary>
        /// Attempts to release this hand's object, but only if the argument object is the object currently
        /// grasped by this hand. If the hand was holding the argument object, returns true, otherwise returns false.
        /// </summary>
        public bool ReleaseObject(IInteractionBehaviour toRelease)
        {
            if (toRelease == null) return false;

            if (_graspedObject == toRelease)
            {
                ReleaseGrasp();
                return true;
            }
            else
            {
                return false;
            }
        }

        #region Grasp State Checking

        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Returns true if the hand just released an object and outputs the released object into releasedObject.
        /// </summary>
        bool IInternalInteractionController.CheckGraspEnd(out IInteractionBehaviour releasedObject)
        {
            releasedObject = null;

            bool shouldReleaseObject = false;

            // Check releasing against interaction state.
            if (_graspedObject == null)
            {
                return false;
            }
            else if (_graspedObject.ignoreGrasping)
            {
                onGraspedObjectForciblyReleased(_graspedObject);

                releasedObject = _graspedObject;
                shouldReleaseObject = true;
            }

            // Actually check whether the controller implementation will release its grasp.
            if (!shouldReleaseObject) shouldReleaseObject = checkShouldRelease(out releasedObject);

            if (shouldReleaseObject)
            {
                OnGraspEnd();
                _graspedObject = null;
                EnableSoftContact(); // prevent objects popping out of the hand on release
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Returns true if the hand just grasped an object and outputs the grasped object into graspedObject.
        /// </summary>
        bool IInternalInteractionController.CheckGraspBegin(out IInteractionBehaviour newlyGraspedObject)
        {
            newlyGraspedObject = null;

            // Check grasping against interaction state.
            if (_graspedObject != null)
            {
                // Can't grasp any object if we're already grasping one or
                // if grasping is disabled.
                return false;
            }

            // Actually check whether the controller implementation will grasp.
            bool shouldGraspObject = checkShouldGrasp(out newlyGraspedObject);
            if (shouldGraspObject)
            {
                _graspedObject = newlyGraspedObject;
                OnGraspBegin();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Returns whether there the hand is currently grasping an object and, if it is, outputs that
        /// object into graspedObject.
        /// </summary>
        bool IInternalInteractionController.CheckGraspHold(out IInteractionBehaviour graspedObject)
        {
            graspedObject = _graspedObject;
            if (graspedObject != null) OnGraspStay();
            return graspedObject != null;
        }

        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Returns whether the hand began suspending an object this frame and, if it did, outputs that
        /// object into suspendedObject.
        /// </summary>
        bool IInternalInteractionController.CheckSuspensionBegin(out IInteractionBehaviour suspendedObject)
        {
            suspendedObject = null;

            if (_graspedObject != null && !isTracked && !_graspedObject.isSuspended)
            {
                suspendedObject = _graspedObject;
            }

            return suspendedObject != null;
        }

        /// <summary>
        /// Called by the Interaction Manager every fixed frame.
        /// Returns whether the hand stopped suspending an object this frame and, if it did, outputs that
        /// object into resumedObject.
        /// </summary>
        bool IInternalInteractionController.CheckSuspensionEnd(out IInteractionBehaviour resumedObject)
        {
            resumedObject = null;

            if (_graspedObject != null && isTracked && _graspedObject.isSuspended)
            {
                resumedObject = _graspedObject;
            }

            return resumedObject != null;
        }

        #endregion

        #endregion

        #region Gizmos

        public static class GizmoColors
        {

            public static Color ContactBone { get { return Color.green.WithAlpha(0.5F); } }
            public static Color SoftContactBone { get { return Color.white.WithAlpha(0.5F); } }

            public static Color HoverPoint { get { return Color.yellow.WithAlpha(0.5F); } }
            public static Color PrimaryHoverPoint { get { return Color.Lerp(Color.red, Color.yellow, 0.5F).WithAlpha(0.5F); } }

            public static Color GraspPoint { get { return Color.Lerp(Color.blue, Color.cyan, 0.3F).WithAlpha(0.5F); } }
            public static Color Graspable { get { return Color.cyan.WithAlpha(0.5F); } }

        }

        /// <summary>
        /// By default, this method will draw all of the colliders found in the
        /// contactBoneParent hierarchy, or draw the controller's soft contact
        /// representation when in soft contact mode. Optionally override this
        /// method to modify its behavior.
        /// </summary>
        public virtual void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer)
        {
            if (!this.isActiveAndEnabled) return;

            if (contactBoneParent != null)
            {
                if (!softContactEnabled)
                {
                    drawer.color = GizmoColors.ContactBone;
                }
                else
                {
                    drawer.color = GizmoColors.SoftContactBone;
                }

                drawer.DrawColliders(contactBoneParent, true, true, true);
            }

            // Hover Point
            if (hoverEnabled)
            {
                drawHoverPoint(drawer, hoverPoint);
            }

            // Primary Hover Points
            if (hoverEnabled)
            {
                foreach (var point in primaryHoverPoints)
                {
                    if (point == null) continue;
                    drawPrimaryHoverPoint(drawer, point.position);
                }
            }
        }

        protected static void drawHoverPoint(RuntimeGizmoDrawer drawer, Vector3 pos)
        {
            drawer.color = GizmoColors.HoverPoint;
            drawer.DrawWireSphere(pos, 0.03F);
        }

        protected static void drawPrimaryHoverPoint(RuntimeGizmoDrawer drawer, Vector3 pos)
        {
            drawer.color = GizmoColors.PrimaryHoverPoint;
            drawer.DrawWireSphere(pos, 0.015F);
        }

        #endregion

    }
}