using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public partial class InteractionManager : MonoBehaviour {

    [Header("Interactions")]
    public bool enableHovering = true;
    public bool enableContact  = true;
    public bool enableGrasping = true;

    [Header("Interaction Settings")]
    [DisableIf("enableHovering", isEqualTo: false)]
    public float hoverActivationRadius = 0.5F;
    [DisableIf("contactOrGraspingEnabled", isEqualTo: false)]
    public float touchActivationRadius = 0.15F;

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

    private InteractionHand[] _interactionHands = new InteractionHand[2];
    private HashSet<InteractionBehaviourBase> _interactionBehaviours = new HashSet<InteractionBehaviourBase>();

    void Awake() {
      Provider = Hands.Provider;

      _interactionHands[0] = new InteractionHand(this, () => { return Hands.FixedLeft;  }, WorldHoverActivationRadius, WorldTouchActivationRadius);
      _interactionHands[1] = new InteractionHand(this, () => { return Hands.FixedRight; }, WorldHoverActivationRadius, WorldTouchActivationRadius);
    }

    void OnValidate() {
      contactOrGraspingEnabled = enableContact || enableGrasping;
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
    }

    #endregion

    public InteractionHand GetInteractionHand(Chirality whichHand) {
      if (whichHand == Chirality.Left) {
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

  }

}