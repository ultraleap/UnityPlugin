/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using InteractionEngineUtility;
using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;
using Leap.Unity.Query;
using Leap.Unity.Interaction.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.Interaction {

  public partial class InteractionManager : MonoBehaviour, IRuntimeGizmoComponent {

    // TODO: Add customization regarding supported InteractionControllers here.

    [Header("Advanced Settings")]

    [SerializeField]
    #pragma warning disable 0414
    private bool _showAdvancedSettings = false; // Used by the custom editor script.
    #pragma warning restore 0414
    
    [Header("Interaction Settings")]

    [SerializeField]
    [Tooltip("Beyond this radius, an interaction object will not receive hover or primary "
           + "hover callbacks from an interaction controller. (Smaller values are "
           + "cheaper.) This value is automatically scaled under the hood by the "
           + "Interaction Manager's lossyScale.x, so it's recommended to keep your "
           + "Interaction Manager with unit scale underneath your 'Player' Transform if "
           + "you expect your player's hands or controllers to ever have non-unit scale.")]
    public float hoverActivationRadius = 0.2F;

    [Tooltip("Beyond this radius, an interaction object will not be considered for "
           + "contact or grasping logic. The radius should be small as an optimization "
           + "but certainly not smaller than an interaction controller and not too tight "
           + "around the controller to allow good behavior when it is moving quickly "
           + "through space. This value is automatically scaled under the hood by the "
           + "Interaction Manager's lossyScale.x, so it's recommended to keep your "
           + "Interaction Manager with unit scale underneath your 'Player' Transform if "
           + "you expect your player's hands or controllers to ever have non-unit scale.")]
    public float touchActivationRadius = 0.075F;

    [Header("Layer Settings")]
    [Tooltip("Whether or not to create the layers used for interaction when the scene "
           + "runs. Interactions require an interaction layer (for objects), a grasped "
           + "object layer, and a contact bone layer (for interaction controller 'bone'"
           + "colliders). Keep this checked to have these layers created for you, but be "
           + "aware that the generated layers will have blank names due to Unity "
           + "limitations.")]
    [SerializeField]
    [EditTimeOnly]
    protected bool _autoGenerateLayers = true;
    /// <summary>
    /// Gets whether auto-generate layers was enabled for this Interaction Manager.
    /// </summary>
    public bool autoGenerateLayers { get { return _autoGenerateLayers; } }

    [Tooltip("When automatically generating layers, the Interaction layer (for "
           + "interactable objects) will use the same physics collision flags as the "
           + "layer specified here.")]
    [SerializeField]
    protected SingleLayer _templateLayer = 0;
    public SingleLayer templateLayer { get { return _templateLayer; } }

    [Tooltip("The layer for interactable objects (i.e. InteractionBehaviours). Usually "
           + "this would have the same collision flags as the Default layer, but it "
           + "should be its own layer so interaction controllers don't have to check "
           + "collision against all physics objects in the scene.")]
    [SerializeField]
    protected SingleLayer _interactionLayer = 0;
    public SingleLayer interactionLayer { get { return _interactionLayer; } }

    [Tooltip("The layer objects are moved to when they become grasped, or if they are "
           + "otherwise ignoring controller contact. This layer should not collide with "
           + "the contact bone layer, but should collide with everything else that the "
           + "interaction layer collides with.")]
    [SerializeField]
    protected SingleLayer _interactionNoContactLayer = 0;
    public SingleLayer interactionNoContactLayer { get { return _interactionNoContactLayer; } }

    [Tooltip("The layer containing the collider 'bones' of the interaction controller. "
           + "This layer should collide with anything you'd like to be able to touch, "
           + "but it should not collide with the grasped object layer.")]
    [SerializeField]
    protected SingleLayer _contactBoneLayer = 0;
    public SingleLayer contactBoneLayer { get { return _contactBoneLayer; } }

    [Header("Debug Settings")]
    [SerializeField]
    [Tooltip("Rendering runtime gizmos requires having a Runtime Gizmo Manager somewhere "
           + "in the scene.")]
    private bool _drawControllerRuntimeGizmos = false;

    public Action OnGraphicalUpdate = () => { };
    public Action OnPrePhysicalUpdate = () => { };
    public Action OnPostPhysicalUpdate = () => { };

    private float _scale = 1F;

    /// <summary>
    /// Interaction objects further than this distance from a given controller's hover
    /// point will not be considered for any hover interactions with that controller.
    /// </summary>
    public float WorldHoverActivationRadius { get { return hoverActivationRadius * _scale; } }

    /// <summary>
    /// Interaction objects further than this distance from a given controller's hover
    /// point will not be considered for any contact or grasping interactions with that
    /// controller.
    /// </summary>
    public float WorldTouchActivationRadius { get { return touchActivationRadius * _scale; } }

    /// <summary>
    /// A scale that can be used to appropriately transform distances that otherwise expect
    /// one Unity unit to correspond to one meter.
    /// </summary>
    public float SimulationScale { get { return _scale; } }

    [SerializeField]
    private List<InteractionController> _interactionControllers = new List<InteractionController>();
    /// <summary>
    /// Gets the list of interaction controllers managed by this InteractionManager.
    /// </summary>
    public ReadonlyList<InteractionController> interactionControllers {
      get { return _interactionControllers; }
    }

    private HashSet<IInteractionBehaviour> _interactionBehaviours = new HashSet<IInteractionBehaviour>();

    private Dictionary<Rigidbody, IInteractionBehaviour> _interactionObjectBodies;
    public Dictionary<Rigidbody, IInteractionBehaviour> interactionObjectBodies {
      get {
        if (_interactionObjectBodies == null) {
          _interactionObjectBodies = new Dictionary<Rigidbody, IInteractionBehaviour>();
        }
        return _interactionObjectBodies;
      }
    }

    private Dictionary<Rigidbody, ContactBone> _contactBoneBodies;
    public Dictionary<Rigidbody, ContactBone> contactBoneBodies {
      get {
        if (_contactBoneBodies == null) {
          _contactBoneBodies = new Dictionary<Rigidbody, ContactBone>();
        }
        return _contactBoneBodies;
      }
    }

    /// <summary>
    /// Stores data for implementing Soft Contact for interaction controllers.
    /// </summary>
    [NonSerialized]
    public List<PhysicsUtility.SoftContact> _softContacts = new List<PhysicsUtility.SoftContact>(80);
    /// <summary>
    /// Stores data for implementing Soft Contact for interaction controllers.
    /// </summary>
    [NonSerialized]
    public Dictionary<Rigidbody, PhysicsUtility.Velocities> _softContactOriginalVelocities = new Dictionary<Rigidbody, PhysicsUtility.Velocities>(5);

    private static InteractionManager s_instance;
    /// <summary> Often, only one InteractionManager is necessary per Unity scene.
    /// This property will contain that InteractionManager as soon as its Awake()
    /// method is called. Using more than one InteractionManager is valid, but be
    /// sure to assign any InteractionBehaviour's desired manager appropriately.
    /// </summary>
    /// 
    /// <remarks> By default, this static property contains the first InteractionManager
    /// that has had its Awake() method called in the current scene. If an
    /// InteractionBehaviourBase does not have a non-null interactionManager by the
    /// time it has Start() called, it will default to using the InteractionManager
    /// referenced here.
    /// 
    /// If you have multiple InteractionManagers in your scene, you should be sure to
    /// assign InteractionBehaviours' managers appropriately. If you instantiate an
    /// InteractionBehaviour at runtime, you should assign its InteractionManager
    /// right after you instantiate it. </remarks>
    public static InteractionManager instance {
      get {
        if (s_instance == null) { s_instance = FindObjectOfType<InteractionManager>(); }
        return s_instance;
      }
      set { s_instance = value; }
    }

    void OnValidate() {
      if (!Application.isPlaying && _autoGenerateLayers) {
        generateAutomaticLayers();
      }
      
      refreshInteractionHands();
    }

    void Awake() {
      if (s_instance == null) s_instance = this;

      refreshInteractionHands();

      if (_autoGenerateLayers) {
        generateAutomaticLayers();
        setupAutomaticCollisionLayers();
      }

      #if UNITY_EDITOR
      if (_drawControllerRuntimeGizmos == true) {
        if (FindObjectOfType<RuntimeGizmoManager>() == null) {
          Debug.LogWarning("'_drawControllerRuntimeGizmos' is enabled, but there is no "
                         + "RuntimeGizmoManager in your scene. Please add one if you'd "
                         + "like to render gizmos in the editor and in your headset.");
        }
      }
      #endif
    }

    void OnDisable() {
      foreach (var intController in _interactionControllers) {
        // Disables the colliders in the interaction controller;
        // soft contact won't be applied if the controller is not updating.
        intController.EnableSoftContact();

        if (intController.isGraspingObject) {
          intController.ReleaseGrasp();
        }
      }
    }

    void FixedUpdate() {
      OnPrePhysicalUpdate();

      using (new ProfilerSample("Interaction Manager FixedUpdate", this.gameObject)) {
        // Ensure scale information is up-to-date.
        _scale = this.transform.lossyScale.x;
        
        // Update each interaction controller (Leap hands or supported VR controllers).
        fixedUpdateInteractionControllers();

        // Perform each interaction object's FixedUpdateObject.
        using (new ProfilerSample("FixedUpdateObject per-InteractionBehaviour")) {
          foreach (var interactionObj in _interactionBehaviours) {
            interactionObj.FixedUpdateObject();
          }
        }

        // Apply soft contacts from all controllers in a unified solve.
        // (This will clear softContacts and originalVelocities as well.)
        using (new ProfilerSample("Apply Soft Contacts")) {
          if (_softContacts.Count > 0) {
            PhysicsUtility.applySoftContacts(_softContacts, _softContactOriginalVelocities);
          }
        }
      }

      OnPostPhysicalUpdate();
    }

    void LateUpdate() {
      OnGraphicalUpdate();
    }

    #region Controller Interaction State & Callbacks Update

    private void fixedUpdateInteractionControllers() {
      using (new ProfilerSample("Fixed Update Controllers (General Update)")) {
        // Perform general controller update, for controller collider and point
        // representations.
        foreach (var controller in interactionControllers) {
          if (!controller.isActiveAndEnabled) continue;
          controller.FixedUpdateController();
        }
      }

      using (new ProfilerSample("Fixed Update Controllers (Interaction State and Callbacks)")) {

        /* 
         * Interactions are checked here in a very specific manner so that interaction
         * callbacks always occur in a strict order and interaction object state is
         * always updated directly before the relevant callbacks occur.
         * 
         * Interaction callbacks will only occur outside this order if a script
         * manually forces interaction state-changes; for example, calling
         * interactionController.ReleaseGrasp() will immediately call
         * interactionObject.OnPerControllerGraspEnd() on the formerly grasped object.
         * 
         * Callback order:
         * - Suspension (when a grasped object's grasping controller loses tracking)
         * - Just-Ended Interactions (Grasps, then Contacts, then Hovers)
         * - Just-Begun Interactions (Hovers, then Contacts, then Grasps)
         * - Sustained Interactions (Hovers, then Contacts, then Grasps)
         */

        // Suspension //

        // Check controllers beginning object suspension.
        foreach (var controller in _interactionControllers) {
          IInteractionBehaviour suspendedObj;
          if (controller.CheckSuspensionBegin(out suspendedObj)) {
            suspendedObj.BeginSuspension(controller);
          }
        }

        // Check controllers ending object suspension.
        foreach (var controller in _interactionControllers) {
          IInteractionBehaviour resumedObj;
          if (controller.CheckSuspensionEnd(out resumedObj)) {
            resumedObj.EndSuspension(controller);
          }
        }

        // Ending Interactions //

        // Check ending grasps.
        remapInteractionObjectStateChecks(
          stateCheckFunc: (InteractionController maybeReleasingController, out IInteractionBehaviour maybeReleasedObject) => {
            return maybeReleasingController.CheckGraspEnd(out maybeReleasedObject);
          },
          actionPerInteractionObject: (releasedObject, releasingIntControllers) => {
            releasedObject.EndGrasp(releasingIntControllers);
          });

        // Check ending contacts.
        remapMultiInteractionObjectStateChecks(
          multiObjectStateCheckFunc: (InteractionController maybeEndedContactingController, out HashSet<IInteractionBehaviour> endContactedObjects) => {
            return maybeEndedContactingController.CheckContactEnd(out endContactedObjects);
          },
          actionPerInteractionObject: (endContactedObject, endContactedIntControllers) => {
            endContactedObject.EndContact(endContactedIntControllers);
          });

        // Check ending primary hovers.
        remapInteractionObjectStateChecks(
          stateCheckFunc: (InteractionController maybeEndedPrimaryHoveringController, out IInteractionBehaviour endPrimaryHoveredObject) => {
            return maybeEndedPrimaryHoveringController.CheckPrimaryHoverEnd(out endPrimaryHoveredObject);
          },
          actionPerInteractionObject: (endPrimaryHoveredObject, noLongerPrimaryHoveringControllers) => {
            endPrimaryHoveredObject.EndPrimaryHover(noLongerPrimaryHoveringControllers);
          });

        // Check ending hovers.
        remapMultiInteractionObjectStateChecks(
          multiObjectStateCheckFunc: (InteractionController maybeEndedHoveringController, out HashSet<IInteractionBehaviour> endHoveredObjects) => {
            return maybeEndedHoveringController.CheckHoverEnd(out endHoveredObjects);
          },
          actionPerInteractionObject: (endHoveredObject, endHoveringIntControllers) => {
            endHoveredObject.EndHover(endHoveringIntControllers);
          });

        // Beginning Interactions //

        // Check beginning hovers.
        remapMultiInteractionObjectStateChecks(
          multiObjectStateCheckFunc: (InteractionController maybeBeganHoveringController, out HashSet<IInteractionBehaviour> beganHoveredObjects) => {
            return maybeBeganHoveringController.CheckHoverBegin(out beganHoveredObjects);
          },
          actionPerInteractionObject: (beganHoveredObject, beganHoveringIntControllers) => {
            beganHoveredObject.BeginHover(beganHoveringIntControllers);
          });

        // Check beginning primary hovers.
        remapInteractionObjectStateChecks(
          stateCheckFunc: (InteractionController maybeBeganPrimaryHoveringController, out IInteractionBehaviour primaryHoveredObject) => {
            return maybeBeganPrimaryHoveringController.CheckPrimaryHoverBegin(out primaryHoveredObject);
          },
          actionPerInteractionObject: (newlyPrimaryHoveredObject, beganPrimaryHoveringControllers) => {
            newlyPrimaryHoveredObject.BeginPrimaryHover(beganPrimaryHoveringControllers);
          });

        // Check beginning contacts.
        remapMultiInteractionObjectStateChecks(
          multiObjectStateCheckFunc: (InteractionController maybeBeganContactingController, out HashSet<IInteractionBehaviour> beganContactedObjects) => {
            return maybeBeganContactingController.CheckContactBegin(out beganContactedObjects);
          },
          actionPerInteractionObject: (beganContactedObject, beganContactingIntControllers) => {
            beganContactedObject.BeginContact(beganContactingIntControllers);
          });

        // Check beginning grasps.
        remapInteractionObjectStateChecks(
          stateCheckFunc: (InteractionController maybeBeganGraspingController, out IInteractionBehaviour graspedObject) => {
            return maybeBeganGraspingController.CheckGraspBegin(out graspedObject);
          },
          actionPerInteractionObject: (newlyGraspedObject, beganGraspingIntControllers) => {
            newlyGraspedObject.BeginGrasp(beganGraspingIntControllers);
          });

        // Sustained Interactions //

        // Check sustaining hover.
        remapMultiInteractionObjectStateChecks(
          multiObjectStateCheckFunc: (InteractionController maybeSustainedHoveringController, out HashSet<IInteractionBehaviour> hoveredObjects) => {
            return maybeSustainedHoveringController.CheckHoverStay(out hoveredObjects);
          },
          actionPerInteractionObject: (hoveredObject, hoveringIntControllers) => {
            hoveredObject.StayHovered(hoveringIntControllers);
          });

        // Check sustaining primary hovers.
        remapInteractionObjectStateChecks(
          stateCheckFunc: (InteractionController maybeSustainedPrimaryHoveringController, out IInteractionBehaviour primaryHoveredObject) => {
            return maybeSustainedPrimaryHoveringController.CheckPrimaryHoverStay(out primaryHoveredObject);
          },
          actionPerInteractionObject: (primaryHoveredObject, primaryHoveringControllers) => {
            primaryHoveredObject.StayPrimaryHovered(primaryHoveringControllers);
          });

        // Check sustained contact.
        remapMultiInteractionObjectStateChecks(
          multiObjectStateCheckFunc: (InteractionController maybeSustainedContactingController, out HashSet<IInteractionBehaviour> contactedObjects) => {
            return maybeSustainedContactingController.CheckContactStay(out contactedObjects);
          },
          actionPerInteractionObject: (contactedObject, contactingIntControllers) => {
            contactedObject.StayContacted(contactingIntControllers);
          });

        // Check sustained grasping.
        remapInteractionObjectStateChecks(
          stateCheckFunc: (InteractionController maybeSustainedGraspingController, out IInteractionBehaviour graspedObject) => {
            return maybeSustainedGraspingController.CheckGraspHold(out graspedObject);
          },
          actionPerInteractionObject: (contactedObject, contactingIntControllers) => {
            contactedObject.StayGrasped(contactingIntControllers);
          });

      }
    }
    
    private delegate bool StateChangeCheckFunc(InteractionController controller, out IInteractionBehaviour obj);
    private delegate bool MultiStateChangeCheckFunc(InteractionController controller, out HashSet<IInteractionBehaviour> objs);

    [ThreadStatic]
    private static Dictionary<IInteractionBehaviour, List<InteractionController>> s_objControllersMap = new Dictionary<IInteractionBehaviour, List<InteractionController>>();

    /// <summary>
    /// Checks object state per-controller, then calls an action per-object with all controller checks that reported back an object.
    /// </summary>
    private void remapInteractionObjectStateChecks(StateChangeCheckFunc stateCheckFunc,
                                                   Action<IInteractionBehaviour, List<InteractionController>> actionPerInteractionObject) {

      // Ensure the object->controllers buffer is non-null (ThreadStatic quirk) and clean.
      if (s_objControllersMap == null) s_objControllersMap = new Dictionary<IInteractionBehaviour,List<InteractionController>>();
      s_objControllersMap.Clear();

      // In a nutshell, this remaps methods per-controller that output an interaction object if the controller changed that object's state
      // to methods per-object with all of the controllers for which the check produced a state-change.
      foreach (var controller in _interactionControllers) {
        IInteractionBehaviour objectWhoseStateChanged;
        if (stateCheckFunc(controller, out objectWhoseStateChanged)) {
          if (!s_objControllersMap.ContainsKey(objectWhoseStateChanged)) {
            s_objControllersMap[objectWhoseStateChanged] = Pool<List<InteractionController>>.Spawn();
          }
          s_objControllersMap[objectWhoseStateChanged].Add(controller);
        }
      }
      // Finally, iterate through each (object, controllers) pair and call the action for each pair
      foreach (var objControllesPair in s_objControllersMap) {
        actionPerInteractionObject(objControllesPair.Key, objControllesPair.Value);

        // Clear each controllers list and return it to the list pool.
        objControllesPair.Value.Clear();
        Pool<List<InteractionController>>.Recycle(objControllesPair.Value);
      }
    }

    /// <summary>
    /// Checks object state per-controller, then calls an action per-object with all controller checks that reported back objects.
    /// </summary>
    private void remapMultiInteractionObjectStateChecks(MultiStateChangeCheckFunc multiObjectStateCheckFunc,
                                                        Action<IInteractionBehaviour, List<InteractionController>> actionPerInteractionObject) {
      // Ensure object<->controllers buffer is non-null (ThreadStatic quirk) and clean.
      if (s_objControllersMap == null) s_objControllersMap = new Dictionary<IInteractionBehaviour, List<InteractionController>>();
      s_objControllersMap.Clear();

      // In a nutshell, this remaps methods per-controller that output multiple interaction objects if the controller changed those objects' states
      // to methods per-object with all of the controllers for which the check produced a state-change.
      foreach (var controller in _interactionControllers) {
        HashSet<IInteractionBehaviour> stateChangedObjects;
        if (multiObjectStateCheckFunc(controller, out stateChangedObjects)) {
          foreach (var stateChangedObject in stateChangedObjects) {
            if (!s_objControllersMap.ContainsKey(stateChangedObject)) {
              s_objControllersMap[stateChangedObject] = Pool<List<InteractionController>>.Spawn();
            }
            s_objControllersMap[stateChangedObject].Add(controller);
          }
        }
      }
      // Finally, iterate through each (object, controllers) pair and call the action for each pair
      foreach (var objControllersPair in s_objControllersMap) {
        actionPerInteractionObject(objControllersPair.Key, objControllersPair.Value);

        // Clear each controllers list and return it to the list pool.
        objControllersPair.Value.Clear();
        Pool<List<InteractionController>>.Recycle(objControllersPair.Value);
      }
    }

    #endregion

    #region Object Registration

    public void RegisterInteractionBehaviour(IInteractionBehaviour interactionObj) {
      _interactionBehaviours.Add(interactionObj);
      interactionObjectBodies[interactionObj.rigidbody] = interactionObj;
    }

    /// <summary>
    /// Returns true if the Interaction Behaviour was registered with this manager;
    /// otherwise returns false. The manager is guaranteed not to have the Interaction
    /// Behaviour registered after calling this method.
    /// </summary>
    public bool UnregisterInteractionBehaviour(IInteractionBehaviour interactionObj) {
      bool wasRemovalSuccessful = _interactionBehaviours.Remove(interactionObj);
      if (wasRemovalSuccessful) {
        foreach (var intController in _interactionControllers) {
          intController.ReleaseObject(interactionObj);

          intController.NotifyObjectUnregistered(interactionObj);
        }
        interactionObjectBodies.Remove(interactionObj.rigidbody);
      }
      return wasRemovalSuccessful;
    }

    public bool IsBehaviourRegistered(IInteractionBehaviour interactionObj) {
      return _interactionBehaviours.Contains(interactionObj);
    }

    #endregion

    #region Internal

    private void refreshInteractionHands() {
      _interactionControllers.Clear();

      int handsIdx = 0;
      foreach (var child in this.transform.GetChildren()) {
        InteractionHand intHand = child.GetComponent<InteractionHand>();
        if (intHand != null) {
          if (_interactionControllers.Count == handsIdx) {
            _interactionControllers.Add(intHand);
          }
          else {
            _interactionControllers[handsIdx] = intHand;
          }
          handsIdx++;
        }
        if (handsIdx == 2) break;
      }

#if UNITY_EDITOR
      PrefabType prefabType = PrefabUtility.GetPrefabType(this.gameObject);
      if (prefabType == PrefabType.Prefab || prefabType == PrefabType.ModelPrefab) {
        return;
      }
#endif

      if (_interactionControllers[0] == null) {
        GameObject obj = new GameObject();
        _interactionControllers[0] = obj.AddComponent<InteractionHand>();
      }
      _interactionControllers[0].gameObject.name = "Interaction Hand (Left)";
      _interactionControllers[0].manager = this;
      _interactionControllers[0].transform.parent = this.transform;
      _interactionControllers[0].intHand.handDataMode = HandDataMode.PlayerLeft;

      if (_interactionControllers[1] == null) {
        GameObject obj = new GameObject();
        _interactionControllers[1] = obj.AddComponent<InteractionHand>();
      }
      _interactionControllers[1].gameObject.name = "Interaction Hand (Right)";
      _interactionControllers[1].manager = this;
      _interactionControllers[1].transform.parent = this.transform;
      _interactionControllers[1].intHand.handDataMode = HandDataMode.PlayerRight;

      // TODO: Move me somewhere else.
      // Scan the Interaction Manager for any other child InteractionController objects
      // and add them to the interaction controllers list.
      foreach (Transform child in this.transform.GetChildren()) {
        InteractionController controller = child.GetComponent<InteractionController>();
        if (controller is InteractionHand) continue;
        else {
          _interactionControllers.Add(controller);
        }
      }
    }

    protected void generateAutomaticLayers() {
      _interactionLayer = -1;
      _interactionNoContactLayer = -1;
      _contactBoneLayer = -1;
      for (int i = 8; i < 32; i++) {
        string layerName = LayerMask.LayerToName(i);
        if (string.IsNullOrEmpty(layerName)) {
          if (_interactionLayer == -1) {
            _interactionLayer = i;
          }
          else if (_interactionNoContactLayer == -1) {
            _interactionNoContactLayer = i;
          }
          else if (_contactBoneLayer == -1) {
            _contactBoneLayer = i;
            break;
          }
        }
      }

      if (_interactionLayer == -1 || _interactionNoContactLayer == -1 || _contactBoneLayer == -1) {
        if (Application.isPlaying) {
          enabled = false;
        }
        Debug.LogError("InteractionManager Could not find enough free layers for "
                     + "auto-setup; manual setup is required.", this.gameObject);
        _autoGenerateLayers = false;
        return;
      }
    }

    private void setupAutomaticCollisionLayers() {
      for (int i = 0; i < 32; i++) {
        // Copy ignore settings from template layer
        bool shouldIgnore = Physics.GetIgnoreLayerCollision(_templateLayer, i);
        Physics.IgnoreLayerCollision(_interactionLayer, i, shouldIgnore);
        Physics.IgnoreLayerCollision(_interactionNoContactLayer, i, shouldIgnore);

        // Set brush layer to collide with nothing
        Physics.IgnoreLayerCollision(_contactBoneLayer, i, true);
      }

      //After copy and set we enable the interaction between the brushes and interaction objects
      Physics.IgnoreLayerCollision(_contactBoneLayer, _interactionLayer, false);
    }

    #endregion

    #region Runtime Gizmos

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (_drawControllerRuntimeGizmos) {
        foreach (var controller in _interactionControllers) {
          if (controller != null) {
            controller.OnDrawRuntimeGizmos(drawer);
          }
        }
      }
    }

    #endregion

  }

}
