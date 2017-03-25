using InteractionEngineUtility;
using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public partial class InteractionManager : MonoBehaviour, IRuntimeGizmoComponent {

    [Header("Interactions")]
    public bool enableHovering = true;
    public bool enableContact  = true;
    public bool enableGrasping = true;

    // TODO: Hide these settings behind a dropdown, they probably don't really ever need to be changed
    [Header("Interaction Settings")]
    [DisableIf("enableHovering", isEqualTo: false)]
    public float hoverActivationRadius = 0.5F;
    [DisableIf("contactOrGraspingEnabled", isEqualTo: false)]
    public float touchActivationRadius = 0.15F;

    [Header("Layer Settings")]
    [Tooltip("Whether or not to create the layers used for interaction when the scene runs.")]
    [SerializeField]
    protected bool _autoGenerateLayers = true;

    [Tooltip("Layer to use for auto-generation. The generated interaction layers will have the same collision settings as this layer.")]
    [SerializeField]
    protected SingleLayer _templateLayer = 0;

    [Tooltip("The layer containing interaction objects.")]
    [SerializeField]
    protected SingleLayer _interactionLayer = 0;

    [Tooltip("The layer containing interaction objects when they become grasped.")]
    [SerializeField]
    protected SingleLayer _graspedObjectLayer = 0;

    [Tooltip("The layer containing the colliders for the bones of the hand.")]
    [SerializeField]
    protected SingleLayer _contactBoneLayer = 0;

    [SerializeField]
    #pragma warning disable 0414
    private bool _showDebugOptions = false;
    #pragma warning restore 0414
    [SerializeField]
    private bool _debugDrawHandGizmos = false;

    #pragma warning disable 0414
    [HideInInspector]
    [SerializeField]
    // Editor-only utility variable for touchActivationRadius property drawing.
    private bool contactOrGraspingEnabled = true;
    #pragma warning restore 0414

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
    private HashSet<InteractionBehaviourBase> _interactionBehaviours = new HashSet<InteractionBehaviourBase>();
    private Dictionary<Rigidbody, InteractionBehaviourBase> _rigidbodyRegistry = new Dictionary<Rigidbody, InteractionBehaviourBase>();
    public Dictionary<Rigidbody, InteractionBehaviourBase> RigidbodyRegistry { get { return _rigidbodyRegistry; } }

    /// <summary> Stores data for implementing Soft Contact for InteractionHands. </summary>
    [NonSerialized]
    public List<PhysicsUtility.SoftContact> _softContacts = new List<PhysicsUtility.SoftContact>(80);
    /// <summary> Stores data for implementing Soft Contact for InteractionHands. </summary>
    [NonSerialized]
    public Dictionary<Rigidbody, PhysicsUtility.Velocities> _softContactOriginalVelocities = new Dictionary<Rigidbody, PhysicsUtility.Velocities>(5);

    void OnValidate() {
      contactOrGraspingEnabled = enableContact || enableGrasping;

      if (!Application.isPlaying && _autoGenerateLayers) {
        AutoGenerateLayers();
      }
    }

    /// <summary> Often, only one InteractionManager is necessary per Unity scene.
    /// This property will contain that InteractionManager as soon as its Awake()
    /// method is called. Using more than one InteractionManager is valid, but be
    /// sure to assign the InteractionBehaviour's desired manager appropriately.
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
    public static InteractionManager singleton { get; set; }

    private Func<Hand> GetFixedLeftHand  = new Func<Hand>(() => Hands.FixedLeft);
    private Func<Hand> GetFixedRightHand = new Func<Hand>(() => Hands.FixedRight);

    void Awake() {
      if (InteractionManager.singleton == null) singleton = this;

      Provider = Hands.Provider;

      _interactionHands[0] = new InteractionHand(this, GetFixedLeftHand,  Chirality.Left,  WorldHoverActivationRadius, WorldTouchActivationRadius);
      _interactionHands[1] = new InteractionHand(this, GetFixedRightHand, Chirality.Right, WorldHoverActivationRadius, WorldTouchActivationRadius);

      if (_autoGenerateLayers) {
        AutoGenerateLayers();
        AutoSetupCollisionLayers();
      }
    }

    void Start() {
      if (Provider == null) {
        Debug.LogError("[InteractionManager] No LeapServiceProvider found.");
      }
    }

    void FixedUpdate() {
      OnPrePhysicalUpdate();

      foreach (var interactionHand in _interactionHands) {
        interactionHand.FixedUpdateHand(enableHovering, enableContact, enableGrasping);
      }

      // Apply soft contacts from both hands in unified solve.
      // (This will clear softContacts and originalVelocities as well.)
      if (_softContacts.Count > 0) {
        PhysicsUtility.applySoftContacts(_softContacts, _softContactOriginalVelocities);
      }

      foreach (var interactionObj in _interactionBehaviours) {
        interactionObj.FixedUpdateObject();
      }

      OnPostPhysicalUpdate();
    }

    void Update() {
      if (Provider != null) {
        _providerScale = Provider.transform.lossyScale.x;

        foreach (var interactionHand in _interactionHands) {
          interactionHand.HoverActivationRadius = WorldHoverActivationRadius;
          interactionHand.TouchActivationRadius = WorldTouchActivationRadius;
        }
      }
    }

    void LateUpdate() {
      OnGraphicalUpdate();
    }

    #region Object Registration


    public void RegisterInteractionBehaviour(InteractionBehaviourBase interactionObj) {
      _interactionBehaviours.Add(interactionObj);
      _rigidbodyRegistry[interactionObj.Rigidbody] = interactionObj;
    }

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
    public bool ReleaseObjectFromGrasp(InteractionBehaviourBase interactionObj) {
      if (!_interactionBehaviours.Contains(interactionObj)) {
        Debug.LogError("ReleaseObjectFromGrasp was called, but the interaction object " + interactionObj.transform.name + " is not registered"
          + " with this InteractionManager.");
        return false;
      }

      foreach (var hand in _interactionHands) {
        if (hand.IsGrasping(interactionObj)) {
          hand.ReleaseGrasp();
          return true;
        }
      }
      return false;
    }

    #region Internal

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
      if (_debugDrawHandGizmos) {
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