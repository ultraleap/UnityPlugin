using InteractionEngineUtility;
using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;
using Leap.Unity.Query;
using Leap.Unity.UI.Interaction.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public partial class InteractionManager : MonoBehaviour, IRuntimeGizmoComponent {

    [Header("Interaction Types")]
    [Tooltip("Hovering provides callbacks to Interaction Behaviours when hands are nearby.")]
    public bool enableHovering = true;
    [Tooltip("Contact allows hands to collide with Interaction Behaviours in an intuitive way, "
           + "and enables contact callbacks to Interaction Behaviours.")]
    public bool enableContact  = true;
    [Tooltip("With grasping enabled, hands can pick up, place, pass, or throw Interaction "
           + "Behaviours. Grasping also provides grasp-related callbacks to Interaction "
           + "Behaviours for specifying custom behavior.")]
    public bool enableGrasping = true;

    [Header("Advanced Settings")]
    [SerializeField]
    #pragma warning disable 0414
    private bool _showAdvancedSettings = false;
    #pragma warning restore 0414
    
    [Header("Interaction Settings")]
    [SerializeField]
    [DisableIf("enableHovering", isEqualTo: false)]
    [Tooltip("Beyond this radius, an interaction object will not receive hover callbacks from a hand. (Smaller values are cheaper.) This value is automatically scaled under the hood by the Leap Service Provider's localScale.x.")]
    public float hoverActivationRadius = 0.2F;
    [DisableIfAll("enableContact", "enableGrasping", areEqualTo: false)]
    [Tooltip("Beyond this radius, an interaction object will not be considered for contact or grasping logic. The radius should be small as an optimization but certainly not smaller than a hand and not too tight around the hand to allow good behavior when the hand is moving quickly through space. This value is automatically scaled under the hood by the Leap Service Provider's localScale.x.")]
    public float touchActivationRadius = 0.075F;

    [Header("Layer Settings")]
    [Tooltip("Whether or not to create the layers used for interaction when the scene runs. Hand interactions require an interaction layer (for objects), a grasped object layer, and a contact bone layer (for hand bones). Keep this checked to have these layers created for you, but be aware that the layers will have blank names due to Unity limitations.")]
    [SerializeField]
    protected bool _autoGenerateLayers = true;
    /// <summary> Gets whether auto-generate layers was enabled for this Interaction Manager. </summary>
    public bool autoGenerateLayers { get { return _autoGenerateLayers; } }

    [Tooltip("When automatically generating layers, the Interaction layer (for interactable objects) will use the same physics collision flags as the layer specified here.")]
    [SerializeField]
    protected SingleLayer _templateLayer = 0;
    public SingleLayer templateLayer { get { return _templateLayer; } }

    [Tooltip("The layer for interactable objects (i.e. InteractionBehaviours). Usually this would have the same collision flags as the Default layer, but it should be its own layer so hands don't have to check collision against all physics objects in the scene.")]
    [SerializeField]
    protected SingleLayer _interactionLayer = 0;
    public SingleLayer interactionLayer { get { return _interactionLayer; } }

    [Tooltip("The layer objects are moved to when they become grasped, or if they are otherwise ignoring hand contact. This layer should not collide with the hand bone layer, but should collide with everything else that the interaction layer collides with.")]
    [SerializeField]
    protected SingleLayer _interactionNoContactLayer = 0;
    public SingleLayer interactionNoContactLayer { get { return _interactionNoContactLayer; } }

    [Tooltip("The layer containing the collider bones of the hand. This layer should collide with anything you'd like to be able to touch, but it should not collide with the grasped object layer.")]
    [SerializeField]
    protected SingleLayer _contactBoneLayer = 0;
    public SingleLayer ContactBoneLayer { get { return _contactBoneLayer; } }

    [Header("Debug Settings")]
    [SerializeField]
    [Tooltip("Rendering runtime gizmos requires having a Runtime Gizmo Manager somewhere in the scene.")]
    private bool _drawHandRuntimeGizmos = false;

    /// <summary>
    /// Provides Frame objects consisting of any and all Hands in the scene. 
    /// 
    /// Set by default on Awake(), but can be overridden manually for special situations
    /// involving multiple LeapServiceProviders.
    /// </summary>
    public LeapServiceProvider Provider { get; set; }
    private float _providerScale = 1F;

    public Action OnGraphicalUpdate = () => { };
    public Action OnPrePhysicalUpdate = () => { };
    public Action OnPostPhysicalUpdate = () => { };

    /// <summary>
    /// Interaction objects further than this distance from a given hand will not be
    /// considered for any hover interactions with that hand.
    /// </summary>
    public float WorldHoverActivationRadius { get { return hoverActivationRadius * _providerScale; } }

    /// <summary>
    /// Interaction objects further than this distance from a given hand will not be
    /// considered for any contact or grasping interactions with that hand.
    /// </summary>
    public float WorldTouchActivationRadius { get { return touchActivationRadius * _providerScale; } }

    /// <summary>
    /// A scale that can be used to appropriately transform distances that otherwise expect
    /// one Unity unit to correspond to one meter.
    /// </summary>
    public float SimulationScale { get { return _providerScale; } }

    private InteractionHand[] _interactionHands = new InteractionHand[2];
    private HashSet<IInteractionBehaviour> _interactionBehaviours = new HashSet<IInteractionBehaviour>();

    private Dictionary<Rigidbody, IInteractionBehaviour> _rigidbodyRegistry;
    public Dictionary<Rigidbody, IInteractionBehaviour> rigidbodyRegistry {
      get {
        if (_rigidbodyRegistry == null) {
          _rigidbodyRegistry = new Dictionary<Rigidbody, IInteractionBehaviour>();
        }
        return _rigidbodyRegistry;
      }
    }

    /// <summary> Stores data for implementing Soft Contact for InteractionHands. </summary>
    [NonSerialized]
    public List<PhysicsUtility.SoftContact> _softContacts = new List<PhysicsUtility.SoftContact>(80);
    /// <summary> Stores data for implementing Soft Contact for InteractionHands. </summary>
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

      Provider = Hands.Provider;

      refreshInteractionHands();

      if (_autoGenerateLayers) {
        generateAutomaticLayers();
        setupAutomaticCollisionLayers();
      }
    }

    private Func<Hand> _getFixedLeftHand = new Func<Hand>(() => Hands.FixedLeft);
    private Func<Hand> _getFixedRightHand = new Func<Hand>(() => Hands.FixedRight);

    void OnEnable() {
      if (Provider == null) {
        Debug.LogError("[InteractionManager] No LeapServiceProvider found.");
        this.enabled = false;
      }
    }

    void OnDisable() {
      foreach (var intHand in _interactionHands) {
        intHand.EnableSoftContact(); // disables the colliders in the InteractionHand; soft contact won't be applied if the hand is not updating.
        if (intHand.isGraspingObject) intHand.ReleaseGrasp();
      }
    }

    void FixedUpdate() {
      OnPrePhysicalUpdate();

      using (new ProfilerSample("Interaction Manager FixedUpdate", this.gameObject)) {
        // Ensure provider scale information is up-to-date.
        if (Provider != null) {
          _providerScale = Provider.transform.lossyScale.x;
        }

        // Update each hand's interactions.
        fixedUpdateHands();

        // Perform each interaction object's FixedUpdateObject.
        foreach (var interactionObj in _interactionBehaviours) {
          interactionObj.FixedUpdateObject();
        }

        // Apply soft contacts from both hands in unified solve.
        // (This will clear softContacts and originalVelocities as well.)
        if (_softContacts.Count > 0) {
          PhysicsUtility.applySoftContacts(_softContacts, _softContactOriginalVelocities);
        }
      }

      OnPostPhysicalUpdate();
    }

    void LateUpdate() {
      OnGraphicalUpdate();
    }

    #region Public Methods

    /// <summary>
    /// Returns the InteractionHand object that corresponds to the given Hand object.
    /// 
    /// Currently, the InteractionManager supports only two InteractionHands at one time: one player's left and right hands.
    /// </summary>
    public InteractionHand GetInteractionHand(Hand hand) {
      if (hand.IsLeft) {
        return _interactionHands[0];
      }
      else {
        return _interactionHands[1];
      }
    }
    public InteractionHand GetInteractionHand(bool isLeft) {
      if (isLeft) {
        return _interactionHands[0];
      } else {
        return _interactionHands[1];
      }
    }

    /// <summary>
    /// Returns true if the object was released from a grasped hand, or false
    /// if the object was not held in the first place. This method will fail and return
    /// false if the argument interaction object is not registered with this manager.
    /// </summary>
    public bool TryReleaseObjectFromGrasp(IInteractionBehaviour interactionObj) {
      if (!_interactionBehaviours.Contains(interactionObj)) {
        Debug.LogError("ReleaseObjectFromGrasp was called, but the interaction object " + interactionObj.transform.name + " is not registered "
                     + "with this InteractionManager.");
        return false;
      }

      var didRelease = false;
      foreach (var hand in _interactionHands) {
        if (hand.graspedObject == interactionObj) {
          hand.ReleaseGrasp();
          didRelease = true;
        }
      }
      return didRelease;
    }

    #endregion

    #region Hand Interactions State & Callbacks Update

    private void fixedUpdateHands() {
      using (new ProfilerSample("Fixed Update Hands (Hand Representations)")) {
        // Perform general hand update, for hand representations
        for (int i = 0; i < _interactionHands.Length; i++) {
          _interactionHands[i].FixedUpdateHand(enableHovering, enableContact, enableGrasping);
        }
      }

      using (new ProfilerSample("Fixed Update Hands (Interaction State and Callbacks)")) {

        /* 
         * Interactions are checked here in a very specific manner so that interaction
         * callbacks always occur in a strict order and interaction object state is
         * always updated directly before the relevant callbacks occur.
         * 
         * Interaction callbacks will only occur outside this order if a script
         * manually forces interaction state-changes; for example, calling
         * interactionHand.ReleaseGrasp() will immediately call interactionObject.OnGraspEnd()
         * on the formerly grasped object.
         * 
         * Callback order:
         * - Suspension (when a grasped object's grasping hand loses tracking)
         * - Just-Ended Interactions (Grasps, then Contacts, then Hovers)
         * - Just-Begun Interactions (Hovers, then Contacts, then Grasps)
         * - Sustained Interactions (Hovers, then Contacts, then Grasps)
         */

        // Suspension //

        // Check hands beginning object suspension.
        foreach (var hand in _interactionHands) {
          IInteractionBehaviour suspendedObj;
          if (hand.CheckSuspensionBegin(out suspendedObj)) {
            suspendedObj.BeginSuspension(hand);
          }
        }

        // Check hands ending object suspension.
        foreach (var hand in _interactionHands) {
          IInteractionBehaviour resumedObj;
          if (hand.CheckSuspensionEnd(out resumedObj)) {
            resumedObj.EndSuspension(hand);
          }
        }

        // Ending Interactions //

        // Check ending grasps.
        remapInteractionObjectStateChecks(
          stateCheckFunc: (InteractionHand maybeReleasingHand, out IInteractionBehaviour maybeReleasedObject) => {
            return maybeReleasingHand.CheckGraspEnd(out maybeReleasedObject);
          },
          actionPerInteractionObject: (releasedObject, releasingIntHands) => {
            releasedObject.EndGrasp(releasingIntHands);
          });

        // Check ending contacts.
        remapMultiInteractionObjectStateChecks(
          multiObjectStateCheckFunc: (InteractionHand maybeEndedContactingHand, out HashSet<IInteractionBehaviour> endContactedObjects) => {
            return maybeEndedContactingHand.CheckContactEnd(out endContactedObjects);
          },
          actionPerInteractionObject: (endContactedObject, endContactedIntHands) => {
            endContactedObject.EndContact(endContactedIntHands);
          });

        // Check ending primary hovers.
        remapInteractionObjectStateChecks(
          stateCheckFunc: (InteractionHand maybeEndedPrimaryHoveringHand, out IInteractionBehaviour endPrimaryHoveredObject) => {
            return maybeEndedPrimaryHoveringHand.CheckPrimaryHoverEnd(out endPrimaryHoveredObject);
          },
          actionPerInteractionObject: (endPrimaryHoveredObject, noLongerPrimaryHoveringHands) => {
            endPrimaryHoveredObject.EndPrimaryHover(noLongerPrimaryHoveringHands);
          });

        // Check ending hovers.
        remapMultiInteractionObjectStateChecks(
          multiObjectStateCheckFunc: (InteractionHand maybeEndedHoveringHand, out HashSet<IInteractionBehaviour> endHoveredObjects) => {
            return maybeEndedHoveringHand.CheckHoverEnd(out endHoveredObjects);
          },
          actionPerInteractionObject: (endHoveredObject, endHoveringIntHands) => {
            endHoveredObject.EndHover(endHoveringIntHands);
          });

        // Beginning Interactions //

        // Check beginning hovers.
        if (enableHovering) {
          remapMultiInteractionObjectStateChecks(
            multiObjectStateCheckFunc: (InteractionHand maybeBeganHoveringHand, out HashSet<IInteractionBehaviour> beganHoveredObjects) => {
              return maybeBeganHoveringHand.CheckHoverBegin(out beganHoveredObjects);
            },
            actionPerInteractionObject: (beganHoveredObject, beganHoveringIntHands) => {
              beganHoveredObject.BeginHover(beganHoveringIntHands);
            });
        }

        // Check beginning primary hovers.
        if (enableHovering) {
          remapInteractionObjectStateChecks(
            stateCheckFunc: (InteractionHand maybeBeganPrimaryHoveringHand, out IInteractionBehaviour primaryHoveredObject) => {
              return maybeBeganPrimaryHoveringHand.CheckPrimaryHoverBegin(out primaryHoveredObject);
            },
            actionPerInteractionObject: (newlyPrimaryHoveredObject, beganPrimaryHoveringHands) => {
              newlyPrimaryHoveredObject.BeginPrimaryHover(beganPrimaryHoveringHands);
            });
        }

        // Check beginning contacts.
        if (enableContact) {
          remapMultiInteractionObjectStateChecks(
            multiObjectStateCheckFunc: (InteractionHand maybeBeganContactingHand, out HashSet<IInteractionBehaviour> beganContactedObjects) => {
              return maybeBeganContactingHand.CheckContactBegin(out beganContactedObjects);
            },
            actionPerInteractionObject: (beganContactedObject, beganContactingIntHands) => {
              beganContactedObject.BeginContact(beganContactingIntHands);
            });
        }

        // Check beginning grasps.
        if (enableGrasping) {
          remapInteractionObjectStateChecks(
            stateCheckFunc: (InteractionHand maybeBeganGraspingHand, out IInteractionBehaviour graspedObject) => {
              return maybeBeganGraspingHand.CheckGraspBegin(out graspedObject);
            },
            actionPerInteractionObject: (newlyGraspedObject, beganGraspingIntHands) => {
              newlyGraspedObject.BeginGrasp(beganGraspingIntHands);
            });
        }

        // Sustained Interactions //

        // Check sustaining hover.
        if (enableHovering) {
          remapMultiInteractionObjectStateChecks(
            multiObjectStateCheckFunc: (InteractionHand maybeSustainedHoveringHand, out HashSet<IInteractionBehaviour> hoveredObjects) => {
              return maybeSustainedHoveringHand.CheckHoverStay(out hoveredObjects);
            },
            actionPerInteractionObject: (hoveredObject, hoveringIntHands) => {
              hoveredObject.StayHovered(hoveringIntHands);
            });
        }

        // Check sustaining primary hovers.
        if (enableHovering) {
          remapInteractionObjectStateChecks(
            stateCheckFunc: (InteractionHand maybeSustainedPrimaryHoveringHand, out IInteractionBehaviour primaryHoveredObject) => {
              return maybeSustainedPrimaryHoveringHand.CheckPrimaryHoverStay(out primaryHoveredObject);
            },
            actionPerInteractionObject: (primaryHoveredObject, primaryHoveringHands) => {
              primaryHoveredObject.StayPrimaryHovered(primaryHoveringHands);
            });
        }

        // Check sustained contact.
        if (enableContact) {
          remapMultiInteractionObjectStateChecks(
            multiObjectStateCheckFunc: (InteractionHand maybeSustainedContactingHand, out HashSet<IInteractionBehaviour> contactedObjects) => {
              return maybeSustainedContactingHand.CheckContactStay(out contactedObjects);
            },
            actionPerInteractionObject: (contactedObject, contactingIntHands) => {
              contactedObject.StayContacted(contactingIntHands);
            });
        }

        // Check sustained grasping.
        if (enableContact) {
          remapInteractionObjectStateChecks(
            stateCheckFunc: (InteractionHand maybeSustainedGraspingHand, out IInteractionBehaviour graspedObject) => {
              return maybeSustainedGraspingHand.CheckGraspHold(out graspedObject);
            },
            actionPerInteractionObject: (contactedObject, contactingIntHands) => {
              contactedObject.StayGrasped(contactingIntHands);
            });
        }

      }
    }
    
    private delegate bool StateChangeCheckFunc(InteractionHand hand, out IInteractionBehaviour obj);
    private delegate bool MultiStateChangeCheckFunc(InteractionHand hand, out HashSet<IInteractionBehaviour> objs);

    [ThreadStatic]
    private static Dictionary<IInteractionBehaviour, List<InteractionHand>> s_objHandsMap = new Dictionary<IInteractionBehaviour, List<InteractionHand>>();

    /// <summary>
    /// Checks object state per-hand, then calls an action per-object with all hand checks that reported back an object.
    /// </summary>
    private void remapInteractionObjectStateChecks(StateChangeCheckFunc stateCheckFunc,
                                                   Action<IInteractionBehaviour, List<InteractionHand>> actionPerInteractionObject) {

      // Ensure the object->hands buffer is non-null (ThreadStatic quirk) and clean.
      if (s_objHandsMap == null) s_objHandsMap = new Dictionary<IInteractionBehaviour,List<InteractionHand>>();
      s_objHandsMap.Clear();

      // In a nutshell, this remaps methods per-hand that output an interaction object if the hand changed that object's state
      // to methods per-object with all of the hands for which the check produced a state-change.
      foreach (var hand in _interactionHands) {
        IInteractionBehaviour objectWhoseStateChanged;
        if (stateCheckFunc(hand, out objectWhoseStateChanged)) {
          if (!s_objHandsMap.ContainsKey(objectWhoseStateChanged)) {
            s_objHandsMap[objectWhoseStateChanged] = Pool<List<InteractionHand>>.Spawn();
          }
          s_objHandsMap[objectWhoseStateChanged].Add(hand);
        }
      }
      // Finally, iterate through each (object, hands) pair and call the action for each pair
      foreach (var objHandsPair in s_objHandsMap) {
        actionPerInteractionObject(objHandsPair.Key, objHandsPair.Value);

        // Clear each hands list and return it to the list pool.
        objHandsPair.Value.Clear();
        Pool<List<InteractionHand>>.Recycle(objHandsPair.Value);
      }
    }

    /// <summary>
    /// Checks object state per-hand, then calls an action per-object with all hand checks that reported back objects.
    /// </summary>
    private void remapMultiInteractionObjectStateChecks(MultiStateChangeCheckFunc multiObjectStateCheckFunc,
                                                        Action<IInteractionBehaviour, List<InteractionHand>> actionPerInteractionObject) {
      // Ensure object<->hands buffer is non-null (ThreadStatic quirk) and clean.
      if (s_objHandsMap == null) s_objHandsMap = new Dictionary<IInteractionBehaviour, List<InteractionHand>>();
      s_objHandsMap.Clear();

      // In a nutshell, this remaps methods per-hand that output multiple interaction objects if the hand changed those objects' states
      // to methods per-object with all of the hands for which the check produced a state-change.
      foreach (var hand in _interactionHands) {
        HashSet<IInteractionBehaviour> stateChangedObjects;
        if (multiObjectStateCheckFunc(hand, out stateChangedObjects)) {
          foreach (var stateChangedObject in stateChangedObjects) {
            if (!s_objHandsMap.ContainsKey(stateChangedObject)) {
              s_objHandsMap[stateChangedObject] = Pool<List<InteractionHand>>.Spawn();
            }
            s_objHandsMap[stateChangedObject].Add(hand);
          }
        }
      }
      // Finally, iterate through each (object, hands) pair and call the action for each pair
      foreach (var objHandsPair in s_objHandsMap) {
        actionPerInteractionObject(objHandsPair.Key, objHandsPair.Value);

        // Clear each hands list and return it to the list pool.
        objHandsPair.Value.Clear();
        Pool<List<InteractionHand>>.Recycle(objHandsPair.Value);
      }
    }

    #endregion

    #region Object Registration

    public void RegisterInteractionBehaviour(IInteractionBehaviour interactionObj) {
      _interactionBehaviours.Add(interactionObj);
      rigidbodyRegistry[interactionObj.rigidbody] = interactionObj;
    }

    /// <summary> Returns true if the Interaction Behaviour was registered with this manager; otherwise returns false. 
    /// The manager is guaranteed not to have the Interaction Behaviour registered after calling this method. </summary>
    public bool UnregisterInteractionBehaviour(IInteractionBehaviour interactionObj) {
      bool wasRemovalSuccessful = _interactionBehaviours.Remove(interactionObj);
      if (wasRemovalSuccessful) {
        foreach (var intHand in _interactionHands) {
          intHand.ReleaseObject(interactionObj);
          intHand.grabClassifier.UnregisterInteractionBehaviour(interactionObj);
        }
        rigidbodyRegistry.Remove(interactionObj.rigidbody);      }
      return wasRemovalSuccessful;
    }

    public bool IsBehaviourRegistered(IInteractionBehaviour interactionObj) {
      return _interactionBehaviours.Contains(interactionObj);
    }

    // TODO: Allow InteractionBehaviours to be unregistered; this should call out to hands and
    // handle their grasped object state appropriately if their grasped object was just unregistered.

    #endregion

    #region Internal

    private void refreshInteractionHands() {
      int handsIdx = 0;
      foreach (var child in this.transform.GetChildren()) {
        InteractionHand intHand = child.GetComponent<InteractionHand>();
        if (intHand != null) {
          _interactionHands[handsIdx++] = intHand;
        }
        if (handsIdx == 2) break;
      }

#if UNITY_EDITOR
      PrefabType prefabType = PrefabUtility.GetPrefabType(this.gameObject);
      if (prefabType == PrefabType.Prefab || prefabType == PrefabType.ModelPrefab) {
        return;
      }
#endif

      if (_interactionHands[0] == null) {
        GameObject obj = new GameObject();
        _interactionHands[0] = obj.AddComponent<InteractionHand>();
      }
      _interactionHands[0].gameObject.name = "Interaction Hand (Left)";
      _interactionHands[0].interactionManager = this;
      _interactionHands[0].handAccessor = _getFixedLeftHand;
      _interactionHands[0].transform.parent = this.transform;

      if (_interactionHands[1] == null) {
        GameObject obj = new GameObject();
        _interactionHands[1] = obj.AddComponent<InteractionHand>();
      }
      _interactionHands[1].gameObject.name = "Interaction Hand (Right)";
      _interactionHands[1].interactionManager = this;
      _interactionHands[1].handAccessor = _getFixedRightHand;
      _interactionHands[1].transform.parent = this.transform;
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
        Debug.LogError("InteractionManager Could not find enough free layers for auto-setup, manual setup required.");
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
      if (_drawHandRuntimeGizmos) {
        foreach (var hand in _interactionHands) {
          if (hand != null) {
            hand.OnDrawRuntimeGizmos(drawer);
          }
        }
      }
    }

    #endregion

  }

}