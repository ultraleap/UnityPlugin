using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  public class AnchorGroup : MonoBehaviour {

    public List<Anchor> anchors;

    /// <summary> Warning: Search time is O(N). </summary>
    public bool ContainsAnchor(Anchor anchor) {
      return anchors.Contains(anchor);
    }

    /// <summary>
    /// Returns the closest anchor to the argument position. By default, this method will
    /// only return anchors that are enabled, whose distance from the argument falls within
    /// that anchor's range, and that isn't occupied by an AnchorableBehaviour already.
    /// </summary>
    public Anchor FindClosestAnchor(Vector3 fromPosition, bool requireWithinAnchorRange = true,
                                                          bool requireAnchorIsEnabled = true,
                                                          bool requireAnchorHasSpace = true) {
      Anchor closestAnchor = null;
      float closestAnchorDistanceSqrd = float.PositiveInfinity;
      foreach (Anchor anchor in anchors) {
        if (anchor == null
            || (requireAnchorIsEnabled && !anchor.enabled)
            || (requireAnchorHasSpace && anchor.anchoredObjects.Count > 0)) continue;
        float anchorDistSqrd = anchor.GetDistanceSqrd(fromPosition);
        if (anchorDistSqrd < closestAnchorDistanceSqrd
            && (!requireWithinAnchorRange || anchorDistSqrd < anchor.anchorRange * anchor.anchorRange)) {
          closestAnchor = anchor;
          closestAnchorDistanceSqrd = anchorDistSqrd;
        }
      }
      return closestAnchor;
    }

  }

}