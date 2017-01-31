using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class InteractionManager : MonoBehaviour {

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

    /// <summary>
    /// Interaction objects further than this distance from a given hand will not be
    /// considered for any hover interactions with that hand.
    /// </summary>
    private float WorldHoverActivationRadius { get { return hoverActivationRadius * _providerScale; } }

    /// <summary>
    /// Interaction objects further than this distance from a given hand will not be
    /// considered for any contact or grasping interactions with that hand.
    /// </summary>
    private float WorldTouchActivationRadius { get { return touchActivationRadius * _providerScale; } }

    private InteractionHand[] interactionHands = new InteractionHand[2];

    void Awake() {
      Provider = Hands.Provider;

      interactionHands[0] = new InteractionHand(this, () => { return Hands.FixedLeft;  }, WorldHoverActivationRadius, WorldTouchActivationRadius);
      interactionHands[1] = new InteractionHand(this, () => { return Hands.FixedRight; }, WorldHoverActivationRadius, WorldTouchActivationRadius);
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
      foreach (var interactionHand in interactionHands) {
        interactionHand.FixedUpdateHand(enableHovering, enableContact, enableGrasping);
      }
    }

    void Update() {
      if (Provider != null) {
        _providerScale = Provider.transform.lossyScale.x;

        foreach (var interactionHand in interactionHands) {
          interactionHand.HoverActivationRadius = WorldHoverActivationRadius;
          interactionHand.TouchActivationRadius = WorldTouchActivationRadius;
        }
      }
    }

    #region Accessors

    public InteractionHand GetInteractionHand(Chirality whichHand) {
      if (whichHand == Chirality.Left) {
        return interactionHands[0];
      }
      else {
        return interactionHands[1];
      }
    }

    #endregion

    #region Hovering

    #endregion

    #region "Touch" (Contact / Grasping)

    #endregion

    #region Contact

    #endregion

    #region Grasping

    #endregion

  }

}