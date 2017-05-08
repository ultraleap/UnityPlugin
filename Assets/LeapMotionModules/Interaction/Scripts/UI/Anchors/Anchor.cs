using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Interaction {

  public class Anchor : MonoBehaviour, IRuntimeGizmoComponent {

    public float anchorRange = 0.1F;

    [Tooltip("Should this anchor allow multiple objects to be attached to it at the same time? "
           + "This property is enforced by AnchorGroups and AnchorableBehaviours.")]
    public bool allowMultipleObjects = false;

    private HashSet<AnchorableBehaviour> _anchoredObjects = new HashSet<AnchorableBehaviour>();
    /// <summary>
    /// Gets the set of AnchorableBehaviours currently attached to this anchor.
    /// </summary>
    public HashSet<AnchorableBehaviour> anchoredObjects { get { return _anchoredObjects; } }

    #region Events

    // TODO: These events are not yet complete. Work on implementing them is going to
    // come with changes to Anchor/AnchorableBehaviour/AnchorGroup coming in another PR.

    /// <summary>
    /// Called as soon as any anchorable objects come within range of this anchor. If the anchor
    /// is part of an anchor group, this will only be called when an anchorable object would be
    /// attached to this anchor if it made an anchor attempt. (In other words, this event may
    /// not be called if all anchorable objects within range prefer other nearby anchors.)
    /// </summary>
    //public UnityEvent WhenAnchorableWithinRange;

    /// <summary>
    /// Called when all nearby anchorable objects have left the range of this anchor, or if
    /// all nearby anchorable objects would prefer other anchors if this Anchor is within an
    /// AnchorGroup.
    /// </summary>
    //public UnityEvent WhenNoAnchorableWithinRange;

    /// <summary>
    /// Called every Update() that an AnchorableBehaviour within range prefers this anchor.
    /// </summary>
    //public AnchorableBehaviourEvent WhileAnchorableWithinRange;

    #endregion

    /// <summary>
    /// Returns whether the target position is within the range of this anchor.
    /// </summary>
    public bool IsWithinRange(Vector3 position) {
      Vector3 delta = this.transform.position - position;
      return delta.sqrMagnitude < anchorRange * anchorRange;
    }

    /// <summary>
    /// Returns the squared distance to the target position. AnchorGroups use this to avoid
    /// square roots while determining the closest anchor to a given position.
    /// </summary>
    public virtual float GetDistanceSqrd(Vector3 position) {
      return (this.transform.position - position).sqrMagnitude;
    }

    public void NotifyAnchored(AnchorableBehaviour anchObj) {
      _anchoredObjects.Add(anchObj);
    }

    public void NotifyUnanchored(AnchorableBehaviour anchObj) {
      _anchoredObjects.Remove(anchObj);
    }

    #region Runtime Gizmos

    public bool drawRangeGizmo = false;
    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!drawRangeGizmo) return;
      drawer.color = new Color(0.4F, 0.5F, 0.15F);
      drawer.DrawWireSphere(this.transform.position, anchorRange);
    }

    #endregion

  }

}