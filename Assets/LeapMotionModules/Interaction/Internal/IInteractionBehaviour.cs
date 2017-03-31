using Leap;
using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction.Internal {

  public interface IInteractionBehaviour {

    // Properties from MonoBehaviour
    string              name       { get; } // (subclass MonoBehaviour to satisfy)
    GameObject          gameObject { get; } // ^
    Transform           transform  { get; } // ^

    // Properties for interaction
    InteractionManager  manager    { get; }
    Rigidbody           rigidbody  { get; }

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
    float GetDistance(Vector3 worldPosition);
    void BeginHover(InteractionHand hand);
    void EndHover(InteractionHand hand);
    void BeginPrimaryHover(InteractionHand hand);
    void EndPrimaryHover(InteractionHand hand);

    // Contact
    void BeginContact(InteractionHand hand);
    void EndContact(InteractionHand hand);

    // Grasping
    bool isGrasped { get; }
    void BeginGrasp(InteractionHand hand);
    void EndGrasp(InteractionHand hand);

    bool isSuspended { get; }
    void SuspendGraspedObject(InteractionHand hand);
    void ResumeGraspedObject(InteractionHand hand);

  }

}