using InteractionEngineUtility;
using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public partial class InteractionManager : MonoBehaviour, IRuntimeGizmoComponent {

    [Header("Interaction Types")]
    [Tooltip("Hovering provides callbacks to Interaction Behaviours when hands are nearby.")]
    public bool enableHovering = true;
    [Tooltip("Contact allows hands to collide with Interaction Behaviours in an intuitive way, and enables contact callbacks to Interaction Behaviours.")]
    public bool enableContact  = true;
    [Tooltip("With grasping enabled, hands can pick up, place, pass, or throw Interaction Behaviours. Grasping also provides grasp-related callbacks to Interaction Behaviours for specifying custom behavior.")]
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
    public float hoverActivationRadius = 0.5F;
    [DisableIfAll("enableContact", "enableGrasping", areEqualTo: false)]
    [Tooltip("Beyond this radius, an interaction object will not be considered for contact or grasping logic. The radius should be small as an optimization but certainly not smaller than a hand and not too tight around the hand to allow good behavior when the hand is moving quickly through space. This value is automatically scaled under the hood by the Leap Service Provider's localScale.x.")]
    public float touchActivationRadius = 0.15F;

    [Header("Layer Settings")]
    [Tooltip("Whether or not to create the layers used for interaction when the scene runs. Hand interactions require an interaction layer (for objects), a grasped object layer, and a contact bone layer (for hand bones). Keep this checked to have these layers created for you, but be aware that the layers will have blank names due to Unity limitations.")]
    [SerializeField]
    protected bool _autoGenerateLayers = true;

    [Tooltip("When automatically generating layers, the Interaction layer (for interactable objects) will use the same physics collision flags as the layer specified here.")]
    [SerializeField]
    protected SingleLayer _templateLayer = 0;

    [Tooltip("The layer for interactable objects (i.e. InteractionBehaviours). Usually this would have the same collision flags as the Default layer, but it should be its own layer so hands don't have to check collision against all physics objects in the scene.")]
    [SerializeField]
    protected SingleLayer _interactionLayer = 0;

    [SerializeField]
    [Tooltip("The layer objects are moved to when they become grasped. This layer should not collide with the hand bone layer, but usually should collide with everything else that the interaction layer collides with.")]
    protected SingleLayer _graspedObjectLayer = 0;

    [Tooltip("The layer containing the collider bones of the hand. This layer should collide with anything you'd like to be able to touch, but it should not collide with the grasped object layer.")]
    [SerializeField]
    protected SingleLayer _contactBoneLayer = 0;

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

    void OnValidate() {
      if (!Application.isPlaying && _autoGenerateLayers) {
        AutoGenerateLayers();
      }
    }

    private InteractionHand[] _interactionHands = new InteractionHand[2];
    private HashSet<InteractionBehaviourBase> _interactionBehaviours = new HashSet<InteractionBehaviourBase>();

    private Dictionary<Rigidbody, InteractionBehaviourBase> _rigidbodyRegistry;
    public Dictionary<Rigidbody, InteractionBehaviourBase> rigidbodyRegistry {
      get {
        if (_rigidbodyRegistry == null) {
          _rigidbodyRegistry = new Dictionary<Rigidbody, InteractionBehaviourBase>();
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

    private static InteractionManager s_singleton;
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
    public static InteractionManager singleton {
      get {
        if (s_singleton == null) { s_singleton = FindObjectOfType<InteractionManager>(); }
        return s_singleton;
      }
      set { s_singleton = value; }
    }

    private Func<Hand> _getFixedLeftHand  = new Func<Hand>(() => Hands.FixedLeft);
    private Func<Hand> _getFixedRightHand = new Func<Hand>(() => Hands.FixedRight);

    void Awake() {
      if (s_singleton == null) s_singleton = this;

      Provider = Hands.Provider;

      _interactionHands[0] = new InteractionHand(this, _getFixedLeftHand);
      _interactionHands[1] = new InteractionHand(this, _getFixedRightHand);

      if (_autoGenerateLayers) {
        AutoGenerateLayers();
        AutoSetupCollisionLayers();
      }
    }

    // TODO: Do correct thing in OnEnable / OnDisable for whole Managers

    void Start() {
      if (Provider == null) {
        Debug.LogError("[InteractionManager] No LeapServiceProvider found.");
        this.enabled = false;
      }
    }

    void FixedUpdate() {
      OnPrePhysicalUpdate();

      // Ensure provider scale information is up-to-date.
      if (Provider != null) {
        _providerScale = Provider.transform.lossyScale.x;
      }

      // Perform each hand's FixedUpdateHand.
      foreach (var interactionHand in _interactionHands) {
        interactionHand.FixedUpdateHand(enableHovering, enableContact, enableGrasping);
      }

      // Apply soft contacts from both hands in unified solve.
      // (This will clear softContacts and originalVelocities as well.)
      if (_softContacts.Count > 0) {
        PhysicsUtility.applySoftContacts(_softContacts, _softContactOriginalVelocities);
      }

      // Perform each interaction object's FixedUpdateObject.
      foreach (var interactionObj in _interactionBehaviours) {
        interactionObj.FixedUpdateObject();
      }

      OnPostPhysicalUpdate();
    }

    void LateUpdate() {
      OnGraphicalUpdate();
    }

    #region Object Registration


    public void RegisterInteractionBehaviour(InteractionBehaviourBase interactionObj) {
      _interactionBehaviours.Add(interactionObj);
      rigidbodyRegistry[interactionObj.rigidbody] = interactionObj;
    }

    public bool IsBehaviourRegistered(InteractionBehaviourBase interactionObj) {
      return _interactionBehaviours.Contains(interactionObj);
    }

    // TODO: Allow InteractionBehaviours to be unregistered; this should call out to hands and
    // handle their grasped object state appropriately if their grasped object was just unregistered.

    #endregion

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

    /// <summary> Returns true if the object was released from a grasped hand, or false if the object was not held in the first place. </summary>
    public bool TryReleaseObjectFromGrasp(InteractionBehaviourBase interactionObj) {
      if (!_interactionBehaviours.Contains(interactionObj)) {
        Debug.LogError("ReleaseObjectFromGrasp was called, but the interaction object " + interactionObj.transform.name + " is not registered"
          + " with this InteractionManager.");
        return false;
      }

      var didRelease = false;
      foreach (var hand in _interactionHands) {
        if (hand.IsGrasping(interactionObj)) {
          hand.ReleaseGrasp();
          didRelease = true;
        }
      }
      return didRelease;
    }

    #region Internal

    public SingleLayer TemplateLayer { get { return _templateLayer; } }
    public SingleLayer InteractionLayer { get { return _interactionLayer; } }
    public SingleLayer GraspedObjectLayer { get { return _graspedObjectLayer; } }
    public SingleLayer ContactBoneLayer { get { return _contactBoneLayer; } }

    protected void AutoGenerateLayers() {
      _interactionLayer = -1;
      _graspedObjectLayer = -1;
      _contactBoneLayer = -1;
      for (int i = 8; i < 32; i++) {
        string layerName = LayerMask.LayerToName(i);
        if (string.IsNullOrEmpty(layerName)) {
          if (_interactionLayer == -1) {
            _interactionLayer = i;
          }
          else if (_graspedObjectLayer == -1) {
            _graspedObjectLayer = i;
          }
          else if (_contactBoneLayer == -1) {
            _contactBoneLayer = i;
            break;
          }
        }
      }

      if (_interactionLayer == -1 || _graspedObjectLayer == -1 || _contactBoneLayer == -1) {
        if (Application.isPlaying) {
          enabled = false;
        }
        Debug.LogError("InteractionManager Could not find enough free layers for auto-setup, manual setup required.");
        _autoGenerateLayers = false;
        return;
      }
    }

    private void AutoSetupCollisionLayers() {
      for (int i = 0; i < 32; i++) {
        // Copy ignore settings from template layer
        bool shouldIgnore = Physics.GetIgnoreLayerCollision(_templateLayer, i);
        Physics.IgnoreLayerCollision(_interactionLayer, i, shouldIgnore);
        Physics.IgnoreLayerCollision(_graspedObjectLayer, i, shouldIgnore);

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