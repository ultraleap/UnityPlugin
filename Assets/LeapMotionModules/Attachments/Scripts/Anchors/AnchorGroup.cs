using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Attachments {

  public class AnchorGroup : MonoBehaviour {

    public List<Anchor> anchors;

    /// <summary> Warning: Search time is O(N). </summary>
    public bool ContainsAnchor(Anchor anchor) {
      return anchors.Contains(anchor);
    }

    /// <summary> Returns the closest anchor to the argument position. By default, this method will
    /// only return anchors that are enabled. </summary>
    public Anchor FindClosestAnchor(Vector3 fromPosition, bool requireAnchorIsEnabled = true) {
      Anchor closestAnchor = null;
      float closestDistSqrd = float.PositiveInfinity;
      foreach (Anchor anchor in anchors) {
        if (anchor == null || requireAnchorIsEnabled && !anchor.enabled) continue;
        float anchorDistSqrd = anchor.GetDistanceSqrd(fromPosition);
        if (closestAnchor == null || anchorDistSqrd < closestDistSqrd) {
          closestAnchor = anchor;
          closestDistSqrd = anchorDistSqrd;
        }
      }
      return closestAnchor;
    }

  }

}