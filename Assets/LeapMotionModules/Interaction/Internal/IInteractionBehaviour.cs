using Leap;
using Leap.Unity.Attributes;
using Leap.Unity.Space;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction.Internal {

  /// <summary>
  /// IInteractionBehaviour is the interface that defines all Interaction objects,
  /// specifying the minimum set of functionality required to make objects interactable.
  /// </summary>
  public interface IInteractionBehaviour {

    // Properties from MonoBehaviour
    string              name       { get; } // (subclass MonoBehaviour to satisfy)
    GameObject          gameObject { get; } // ^
    Transform           transform  { get; } // ^

    // Properties for interaction
    InteractionManager  manager    { get; }
    Rigidbody           rigidbody  { get; }
    ISpaceComponent     space      { get; } // OK to return null if this object is not in curved space.

    // Interaction overrides
    bool ignoreHover     { get; }
    bool ignoreContact   { get; }
    bool ignoreGrasping  { get; }

    // Interaction settings
    bool allowMultiGrasp { get; }
    
    // Called by the Interaction Manager manually
    // every fixed (physics) frame.
    void FixedUpdateObject();

    // Hand interactions

    // Hover
    float GetComparativeHoverDistance(Vector3 worldPosition);
    void BeginHover(List<InteractionHand> hands);
    void StayHovered(List<InteractionHand> hands);
    void EndHover(List<InteractionHand> hands);
    void BeginPrimaryHover(List<InteractionHand> hands);
    void StayPrimaryHovered(List<InteractionHand> hands);
    void EndPrimaryHover(List<InteractionHand> hands);

    // Contact
    void BeginContact(List<InteractionHand> hands);
    void StayContacted(List<InteractionHand> hands);
    void EndContact(List<InteractionHand> hands);

    // Grasping
    bool isGrasped { get; }
    void BeginGrasp(List<InteractionHand> hands);
    void StayGrasped(List<InteractionHand> hands);
    void EndGrasp(List<InteractionHand> hands);

    bool isSuspended { get; }
    void BeginSuspension(InteractionHand hand);
    void EndSuspension(InteractionHand hand);

  }

}