using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public bool IsWithinRange(Vector3 position) {
      Vector3 delta = this.transform.position - position;
      return delta.sqrMagnitude < anchorRange * anchorRange;
    }

    /// <summary> Returns the squared distance to the target. AnchorGroups use this to avoid
    /// square roots while determining the closest anchor to a given position. </summary>
    internal float GetDistanceSqrd(Vector3 position) {
      return (this.transform.position - position).sqrMagnitude;
    }

    public void NotifyAnchored(AnchorableBehaviour anchObj) {
      _anchoredObjects.Add(anchObj);
    }

    public void NotifyUnanchored(AnchorableBehaviour anchObj) {
      _anchoredObjects.Remove(anchObj);
    }

    public bool drawRangeGizmo = false;
    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!drawRangeGizmo) return;
      drawer.color = new Color(0.4F, 0.5F, 0.15F);
      drawer.DrawWireSphere(this.transform.position, anchorRange);
    }

  }

}